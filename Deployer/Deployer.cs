using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CSLauncher.LauncherLib;
using System.Net;
using System.IO.Compression;

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

        public Deployer(Deployment deployment, bool verbose)
        {
            Deployment = deployment;
            Verbose = verbose;
        }


        public void Prepare(string toolsetFilter)
        {
            string binPath = Path.GetFullPath(Deployment.BinPath);
            LogVerbose("Using bin path:{0}", binPath);

            string installPath = Path.GetFullPath(Deployment.InstallPath);
            LogVerbose("Using install path:{0}", installPath);

            if (installPath.ToLower() == binPath.ToLower())
                throw new Exception("The install path and the bin path cannot point to the same directory");

            if (!File.Exists(Deployment.LauncherPath))
                throw new Exception("Could not find the launcher app");

            if (!File.Exists(Deployment.LauncherLibPath))
                throw new Exception("Could not find the launcher lib");

            LogVerbose("Using launcher path:{0}", Deployment.LauncherPath);

            var installPathsSet = new HashSet<string>();
            foreach (ToolSet toolset in Deployment.ToolSets)
            {
                if (toolsetFilter != null && toolsetFilter != toolset.Name)
                    continue;

                Log("Toolset {0} preparation started", toolset.Name);

                string toolsetInstallPath;
                if (toolset.UrlSource != null)
                    toolsetInstallPath = HandleUrlSource(toolset.UrlSource, installPath);
                else if (toolset.NugetSource != null)
                    toolsetInstallPath = HandleNugetSource(toolset.NugetSource, installPath);
                else
                    throw new Exception(string.Format("Toolset {0} missing source url and nuget", toolset));

                foreach (Tool tool in toolset.Tools)
                {
                    HandleTool(toolset, tool, binPath, toolsetInstallPath);
                }
            }
        }

        public void ProcessPackages(bool clean)
        {
            if (clean && Directory.Exists(Deployment.InstallPath))
            {
                Directory.Delete(Deployment.InstallPath, true);
            }
            Directory.CreateDirectory(Deployment.InstallPath);

            var tasks = new List<Task>();
            foreach (var packageAction in PackageSetupActions)
            {
                tasks.Add(Task.Run(packageAction));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void ProcessAliases(bool clean)
        {
            if (clean && Directory.Exists(Deployment.BinPath))
            {
                Directory.Delete(Deployment.BinPath, true);
            }
            Directory.CreateDirectory(Deployment.BinPath);

            string launcherLibPath = Path.Combine(Deployment.BinPath, Path.GetFileName(Deployment.LauncherLibPath));
            CopyFile(Deployment.LauncherLibPath, launcherLibPath);

            var tasks = new List<Task>();
            foreach (var aliasAction in AliasSetupActions)
            {
                tasks.Add(Task.Run(aliasAction));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private string HandleUrlSource(string urlSource, string installPath)
        {
            LogVerbose("--Preparing source {0}", urlSource);

            int separatorIdx = urlSource.LastIndexOf('/') + 1;
            string zipFileName = urlSource.Substring(separatorIdx, urlSource.Length - separatorIdx);
            string zipFileNameWithoutExt = zipFileName.Substring(0, zipFileName.Length - 4);
            string toolsetInstallPath = Path.Combine(installPath, zipFileNameWithoutExt);
            string dowloadPath = Path.Combine(installPath, zipFileName);

            if (!Directory.Exists(toolsetInstallPath))
            {
                PackageSetupActions.Add( () =>
                    {
                        Log("Processing zip file {0}", zipFileName);

                        LogVerbose("--Downlading {0}", urlSource);
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(urlSource, dowloadPath);
                        }

                        LogVerbose("--Unzipping {0} to {1}", dowloadPath, installPath);
                        ZipFile.ExtractToDirectory(dowloadPath, installPath);

                        LogVerbose("--Deleting {0}", dowloadPath);
                        File.Delete(dowloadPath);
                    }
                );
            }
            
            return toolsetInstallPath;
        }

        private string HandleNugetSource(NugetSource nugetSource, string installPath)
        {
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

        private void HandleTool(ToolSet toolset, Tool tool, string binPath, string toolsetInstallPath)
        {
            LogVerbose("--Preparing tool {0}", toolset.Name);
            LauncherConfig launcherConfig = new LauncherConfig();

            launcherConfig.ExePath = Path.Combine(toolsetInstallPath, tool.LauncherConfig.ExePath);
            launcherConfig.NoWait = tool.LauncherConfig.NoWait;
            launcherConfig.EnvVariables = tool.LauncherConfig.EnvVariables;
            for (int i = 0; i < launcherConfig.EnvVariables.Length; ++i)
            {
                if (launcherConfig.EnvVariables[i].Value.Contains("{installPath}"))
                    launcherConfig.EnvVariables[i].Value = launcherConfig.EnvVariables[i].Value.Replace("{installPath}", toolsetInstallPath);
            }
            foreach (string alias in tool.Aliases)
            {
                LogVerbose("----Preparing alias {0}", alias);
                string aliasPath = Path.Combine(binPath, alias);
                AliasSetupActions.Add(
                    () =>
                    {
                        Log("Processing alias {0}", alias);

                        string aliasExePath = aliasPath + ".exe";
                        LogVerbose("--Copying launcher to {0}", aliasExePath);
                        CopyFile(Deployment.LauncherPath, aliasExePath, true);
                        string configPath = aliasPath + ".cfg";
                        LogVerbose("--Serializing config file to {0}", configPath);
                        AppInfoSerializer.Write(configPath, launcherConfig);
                    }
                );
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