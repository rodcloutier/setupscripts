using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CSLauncher.LauncherLib;

namespace CSLauncher.Deployer
{
    public class Deployer
    {
        struct CopyRequest
        {
            internal CopyRequest(string from, string to, bool isFile)
            {
                From = from;
                To = to;
                IsFile = isFile;
            }

            internal string From;
            internal string To;
            internal bool IsFile;
        }

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

        List<CopyRequest> CopyRequests;
        List<AliasRequest> AliasRequests;

        public Deployer(Deployment deployment, bool verbose)
        {
            Deployment = deployment;
            Verbose = verbose;

            CopyRequests = new List<CopyRequest>();
            AliasRequests = new List<AliasRequest>();
        }

        private static void CopyDirectory(string source, string target, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(source);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + source);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(target, file.Name);
                if (!File.Exists(tempPath))
                    file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(target, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static Task CopyDirectoryAsync(string source, string target, bool copySubDirs)
        {
            return Task.Run(() => CopyDirectory(source, target, copySubDirs));
        }

        private static void CopyFile(string source, string target)
        {
            if (!File.Exists(target))
            {
                var fileInfo = new FileInfo(source);
                fileInfo.CopyTo(target);
            }
        }

        private static Task CopyFileAsync(string source, string target)
        {
            return Task.Run(() => CopyFile(source, target));
        }

        public void Prepare()
        {
            string binPath = Path.GetFullPath(Deployment.BinPath);
            if (Verbose) Console.WriteLine("Using bin path:{0}", binPath);

            string installPath = Path.GetFullPath(Deployment.InstallPath);
            if (Verbose) Console.WriteLine("Using install path:{0}", installPath);

            if (installPath.ToLower() == binPath.ToLower())
            {
                throw new Exception("The install path and the bin path cannot point to the same directory");
            }

            if (!File.Exists(Deployment.LauncherPath))
            {
                throw new Exception("Could not find the launcher app");
            }

            if (!File.Exists(Deployment.LauncherLibPath))
            {
                throw new Exception("Could not find the launcher lib");
            }

            if (Verbose) Console.WriteLine("Using launcher path:{0}", Deployment.LauncherPath);

            var installPathsSet = new HashSet<string>();
            var aliasPathsSet = new HashSet<string>();
            foreach (ToolSet toolset in Deployment.ToolSets)
            {
                if (Verbose) Console.WriteLine("Processing toolset {0} started", toolset.Name);
                string toolsetPath = Path.GetFullPath(toolset.Path);
                bool isFile = File.Exists(toolsetPath);
                bool isDirectory = Directory.Exists(toolsetPath);

                if (!isFile && !isDirectory)
                {
                    throw new Exception(string.Format("Tool set path ({0}) does not exist", toolsetPath));
                }

                string destinationName = Path.GetFileName(toolset.Path);
                string destinationPath = Path.Combine(Deployment.InstallPath, destinationName);

                if (installPathsSet.Contains(destinationPath.ToLower()))
                {
                    throw new Exception(string.Format("Duplicate destination path: {0}", destinationName));
                }
                installPathsSet.Add(destinationPath.ToLower());

                CopyRequests.Add(new CopyRequest(toolset.Path, destinationPath, isFile));
                if (Verbose) Console.WriteLine("--Copying {0} from {1} to {2}", isFile ? "file" : "directory", toolset.Path, destinationPath);

                foreach (Tool tool in toolset.Tools)
                {
                    if (Verbose) Console.WriteLine("--Processing tool {0}", toolset.Name);
                    LauncherConfig launcherConfig = new LauncherConfig();

                    launcherConfig.ExePath = isFile ? destinationPath : Path.Combine(destinationPath, tool.LauncherConfig.ExePath);
                    launcherConfig.NoWait = tool.LauncherConfig.NoWait;
                    launcherConfig.EnvVariables = tool.LauncherConfig.EnvVariables;

                    foreach (string alias in tool.Aliases)
                    {
                        if (Verbose) Console.WriteLine("----Adding alias {0}", alias);
                        string aliasPath = Path.Combine(binPath, alias);
                        if (aliasPathsSet.Contains(aliasPath.ToLower()))
                        {
                            throw new Exception(string.Format("Duplicate alias path: {0}", aliasPath));
                        }
                        aliasPathsSet.Add(aliasPath);
                        AliasRequests.Add(new AliasRequest(launcherConfig, aliasPath));
                    }
                }
            }
        }

        public void ProcessCopies(bool clean)
        {
            var tasks = new List<Task>();
            tasks.Capacity = CopyRequests.Count;
            if (clean && Directory.Exists(Deployment.InstallPath))
            {
                Directory.Delete(Deployment.InstallPath, true);
            }
            Directory.CreateDirectory(Deployment.InstallPath);
            foreach (CopyRequest copyRequest in CopyRequests)
            {
                if (copyRequest.IsFile)
                {
                    tasks.Add(CopyFileAsync(copyRequest.From, copyRequest.To));
                }
                else
                {
                    tasks.Add(CopyDirectoryAsync(copyRequest.From, copyRequest.To, true));
                }
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
            foreach(AliasRequest aliasRequest in AliasRequests)
            {
                string launcherPath = aliasRequest.AliasPath + ".exe";
                var fileInfo = new FileInfo(Deployment.LauncherPath);
                fileInfo.CopyTo(launcherPath, true);
                string configPath = aliasRequest.AliasPath + ".cfg";
                AppInfoSerializer.Write(configPath, aliasRequest.LauncherConfig);
            }
        }
    }

}