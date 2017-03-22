using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSLauncher.LauncherLib;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace CSLauncher.Deployer
{
    public class Deployer
    {
        struct AliasRequest
        {
            internal AliasRequest(LauncherConfig launcherConfig, string aliasPath)
            {
                LauncherConfig = launcherConfig;
                AliasPath = aliasPath;
            }

            internal LauncherConfig LauncherConfig;
            internal string AliasPath;
        }

        private Deployment Deployment;
        private bool Verbose;

        List<Action> PackageSetupActions = new List<Action>();
        List<Action> AliasSetupActions = new List<Action>();
        List<Action> CommandActions = new List<Action>();
        StringDictionary ConfigValuesMapping = new StringDictionary();


        public Deployer(Deployment deployment, bool verbose)
        {
            Deployment = deployment;
            Verbose = verbose;
        }


        public void Prepare(string toolsetFilter)
        {
            // todo automatically fill this from the json file
            AddConfigValueMapping("binPath", Deployment.BinPath);
            AddConfigValueMapping("installPath", Deployment.InstallPath);
            if (!string.IsNullOrEmpty(Deployment.HttpProxy))
            {
                AddConfigValueMapping("httpProxy", Deployment.HttpProxy);
            }

            LogVerbose("Using launcher path:{0}", Deployment.LauncherPath);

            foreach (ToolSet toolset in Deployment.ToolSets)
            {
                if (toolsetFilter != null && toolsetFilter != toolset.Name)
                    continue;

                Log("Toolset {0} preparation started", toolset.Name);

                string toolsetInstallPath;
                if (toolset.UrlSource != null)
                    toolsetInstallPath = HandleUrlSource(toolset.UrlSource);
                else if (toolset.NugetSource != null)
                    toolsetInstallPath = HandleNugetSource(toolset.NugetSource);
                else
                    throw new Exception(string.Format("Toolset {0} missing source url and nuget", toolset));

                foreach (Tool tool in toolset.Tools)
                {
                    HandleTool(toolset, tool, toolsetInstallPath);
                }
            }
        }
        public void Clean()
        {
            if (Directory.Exists(Deployment.InstallPath))
            {
                Directory.Delete(Deployment.InstallPath, true);
            }
            if (Directory.Exists(Deployment.BinPath))
            {
                Directory.Delete(Deployment.BinPath, true);
            }
        }

        public void ProcessPackages()
        {   
            Directory.CreateDirectory(Deployment.InstallPath);

            var tasks = new List<Task>();
            foreach (var packageAction in PackageSetupActions)
            {
                tasks.Add(Task.Run(packageAction));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void ProcessAliases()
        {
            Directory.CreateDirectory(Deployment.BinPath);

            string launcherLibPath = Path.Combine(Deployment.BinPath, Path.GetFileName(Deployment.LauncherLibPath));
            CopyFileIfNewer(Deployment.LauncherLibPath, launcherLibPath);

            var tasks = new List<Task>();
            foreach (var aliasAction in AliasSetupActions)
            {
                tasks.Add(Task.Run(aliasAction));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void ProcessCommands()
        {
            foreach(var command in CommandActions)
            {
                command.Invoke();
            }
        }

        public void CleanUnused()
        {
            foreach(string directory in Directory.GetDirectories(Deployment.InstallPath))
            {
                string sentinelFile = Path.Combine(directory, "__deployer__");
                if (!File.Exists(sentinelFile) || File.GetLastWriteTimeUtc(sentinelFile) != Deployment.FileDateTime)
                {
                    Log("Deleting unused package {0}", directory);
                    Directory.Delete(directory, true);
                }
            }

            foreach (string file in Directory.GetFiles(Deployment.BinPath, "*.cfg"))
            {
                if (File.GetLastWriteTimeUtc(file) != Deployment.FileDateTime)
                {
                    Log("Deleting unused alias {0}", Path.GetFileNameWithoutExtension(file));

                    File.Delete(file);
                    string exeFile = Path.ChangeExtension(file, ".exe");
                    if (File.Exists(exeFile))
                        File.Delete(exeFile);
                    string shFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                    if (File.Exists(shFile))
                        File.Delete(shFile);
                }
            }
        }

        private void AddConfigValueMapping(string key, string value)
        {
            ConfigValuesMapping.Add("{"+key+"}", value);
        }

        private string GetFinalValue(string str)
        {
            string finalVal = str;
            MatchCollection mc = Regex.Matches(str, @"\{(.*?)\}");
            foreach (Match m in mc)
            {
                finalVal = finalVal.Replace(m.ToString(), ConfigValuesMapping[m.ToString()]);
            }

            return finalVal;
        }

        private string HandleUrlSource(string urlSource)
        {
            LogVerbose("--Preparing source {0}", urlSource);

            int separatorIdx = urlSource.LastIndexOf('/') + 1;
            string installPath = Deployment.InstallPath;
            string zipFileName = urlSource.Substring(separatorIdx, urlSource.Length - separatorIdx);
            string zipFileNameWithoutExt = zipFileName.Substring(0, zipFileName.Length - 4);
            string toolsetInstallPath = Path.Combine(installPath, zipFileNameWithoutExt);
            string downloadPath = Path.Combine(installPath, zipFileName);
            
            PackageSetupActions.Add(() =>
                {
                    if (!Directory.Exists(toolsetInstallPath))
                    {
                        Log("Processing zip file {0}", zipFileName);

                        LogVerbose("--Downloading {0}", urlSource);
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(urlSource, downloadPath);
                        }

                        string expectedDir = toolsetInstallPath.Split(Path.DirectorySeparatorChar).Last();
                        string comparableDir = expectedDir + "/";

                        using (ZipArchive zip = ZipFile.Open(downloadPath, ZipArchiveMode.Read))
                        {
                            if (!zip.Entries.Any(entry => comparableDir == entry.FullName))
                            {
                                LogVerbose("--Did not find " + expectedDir + " dir in archive. Will create it");
                                installPath = Path.Combine(installPath, expectedDir);
                            }
                        }

                        LogVerbose("--Unzipping {0} to {1}", downloadPath, installPath);
                        ZipFile.ExtractToDirectory(downloadPath, installPath);

                        LogVerbose("--Deleting {0}", downloadPath);
                        File.Delete(downloadPath);
                    }
                    string sentinelFile = Path.Combine(toolsetInstallPath, "__deployer__");
                    using (var file = File.Open(sentinelFile, FileMode.OpenOrCreate)) { }
                    File.SetLastWriteTimeUtc(sentinelFile, Deployment.FileDateTime);
                }
            );

            return toolsetInstallPath;
        }

        private string HandleNugetSource(NugetSource nugetSource)
        {
            string installPath = Deployment.InstallPath;

            //ID of the package to be looked up
            /*string packageID = "EntityFramework";

            //Connect to the official package repository
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

            //Initialize the package manager
            string path = "";
            PackageManager packageManager = new PackageManager(repo, path);

            //Download and unzip the package
            packageManager.InstallPackage(packageID, SemanticVersion.Parse("5.0.0"));
            */
            throw new NotImplementedException();
        }

        private Action CreateExeAliasSetupAction(LauncherConfig launcherConfig, string alias, string aliasPath)
        {
            return () =>
            {
                Log("Processing exe alias {0}", alias);

                string aliasExePath = aliasPath + ".exe";
                LogVerbose("--Copying launcher to {0}", aliasExePath);
                CopyFile(Deployment.LauncherPath, aliasExePath, true);
                string configPath = aliasPath + ".cfg";
                LogVerbose("--Serializing config file to {0}", configPath);
                AppInfoSerializer.Write(configPath, launcherConfig);
                File.SetLastWriteTimeUtc(configPath, Deployment.FileDateTime);
            };
        }

        private Action CreateBashScriptAliasSetupAction(LauncherConfig launcherConfig, string alias, string aliasPath)
        {
            return () =>
            {
                Log("Processing bash alias {0}", alias);

                LogVerbose("--Creating laucher script {0}", aliasPath);

                using (StreamWriter file = new StreamWriter(aliasPath))
                {
                    // TODO Use a templating technique instead
                    file.WriteLine("#! /bin/bash");
                    file.WriteLine();

                    foreach( EnvVariable env in launcherConfig.EnvVariables)
                    {
                        file.WriteLine("export {0}={1}", env.Key, env.Value);
                    }
                    file.WriteLine();

                    file.WriteLine("# Get full directory name of the script no matter where it is being called from.");
                    file.WriteLine("CURRENT_DIR=$(cd $(dirname ${BASH_SOURCE[0]-$0} ) ; pwd )");
                    file.WriteLine();

                    string nixPath = '/' + launcherConfig.ExePath.Replace(":", string.Empty).Replace('\\', '/');
                    file.WriteLine("# Call the actual tool");
                    file.WriteLine("source {0}", nixPath);
                }

                string dummyConfigPath = aliasPath + ".cfg";
                using (StreamWriter file = new StreamWriter(dummyConfigPath))
                {
                    file.WriteLine("dummy config file");
                }
                File.SetLastWriteTimeUtc(dummyConfigPath, Deployment.FileDateTime);
            };
        }
        
        private Action CreateCommandAction(string file, string arguments, EnvVariable[] envVariables)
        {
            return () =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Path.Combine(Deployment.BinPath, file);
                startInfo.Arguments = arguments;

                foreach (EnvVariable envVariable in envVariables)
                {
                    string val = GetFinalValue(envVariable.Value);
                    Utils.AddOrSetEnvVariable(startInfo.EnvironmentVariables, envVariable.Key, val);
                }

                Process p = Process.Start(startInfo);
                p.WaitForExit();
            };
        }

        private void HandleTool(ToolSet toolset, Tool tool, string toolsetInstallPath)
        {
            LogVerbose("--Preparing tool {0}", toolset.Name);
            LauncherConfig launcherConfig = new LauncherConfig();

            launcherConfig.ExePath = Path.Combine(toolsetInstallPath, tool.LauncherConfig.ExePath);
            launcherConfig.NoWait = tool.LauncherConfig.NoWait;
            launcherConfig.EnvVariables = tool.LauncherConfig.EnvVariables;
            launcherConfig.Type = tool.LauncherConfig.Type;

            for (int i = 0; i < launcherConfig.EnvVariables.Length; ++i)
            {
                launcherConfig.EnvVariables[i].Value = GetFinalValue(launcherConfig.EnvVariables[i].Value);
            }

            Func<LauncherConfig, string, string, Action> createAliasSetupAction;
            switch (launcherConfig.Type)
            {
                case "exe":
                    createAliasSetupAction = CreateExeAliasSetupAction;
                    break;
                case "bash":
                    createAliasSetupAction = CreateBashScriptAliasSetupAction;
                    break;
                default:
                    throw new Exception("Invalid launcherConfig.type: " + launcherConfig.Type);
            }

            if (tool.Command != null)
            {
                CommandActions.Add(CreateCommandAction(tool.Command.FileName, tool.Command.Arguments, tool.Command.EnvVariables));
            }

            foreach (string alias in tool.Aliases)
            {
                LogVerbose("----Preparing alias {0}", alias);
                string aliasPath = Path.Combine(Deployment.BinPath, alias);
                AliasSetupActions.Add(createAliasSetupAction(launcherConfig, alias, aliasPath));
            }
        }

        private static void CopyFile(string source, string target, bool overwrite = false)
        {
            if (overwrite || !File.Exists(target))
            {
                var fileInfo = new FileInfo(source);
                fileInfo.CopyTo(target, overwrite);
            }
        }

        private static void CopyFileIfNewer(string source, string target)
        {
            CopyFile(source, target, !File.Exists(target) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(target));
        }

        private void LogVerbose(string format, params object[] arg)
        {
            if (Verbose)
                Console.WriteLine(format, arg);
        }

        private void Log(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }
    }
}
