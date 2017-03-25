using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CSLauncher.Deployer
{
    internal class Deployer
    {
        internal Deployer(IList<string> deploymentFilePaths, string optionBinPath, string optionInstallPath, bool outputProcessedJson)
        {
            DateTime mostRecentWriteTimeStamp = DateTime.MinValue;
            List<DeploymentFile> deploymentFiles = new List<DeploymentFile>(deploymentFilePaths.Count);
            foreach (var deploymentFilePath in deploymentFilePaths)
            {
                if (!File.Exists(deploymentFilePath))
                {
                    throw new Exception(String.Format("File '{0}' not found in current directory", deploymentFilePath));
                }
                deploymentFiles.Add(DeploymentFile.Read(deploymentFilePath, outputProcessedJson));
                DateTime currentFileTime = File.GetLastWriteTimeUtc(deploymentFilePath);
                mostRecentWriteTimeStamp = mostRecentWriteTimeStamp > currentFileTime ? mostRecentWriteTimeStamp : currentFileTime;
                Utils.Log("Using deployment file: {0}", deploymentFilePath);
            }

            if (deploymentFiles.Count == 0)
            {
                throw new Exception("You at least have to provide one deployment file");
            }

            Deployment = new Deployment(deploymentFiles, optionBinPath, optionInstallPath, mostRecentWriteTimeStamp);
        }

        internal Deployment Deployment { get; }

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
            foreach (var entry in Deployment.Packages)
            {
                foreach (var package in entry.Value)
                {
                    if (package.IsUsed)
                        tasks.Add(Task.Run(() => { package.Install(Deployment); }));
                    else
                        Utils.Log("Warining: Unused package {0}-{1}", entry.Key, package.Version.ToFullString());
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void ProcessTools()
        {
            Directory.CreateDirectory(Deployment.BinPath);

            string launcherLibPath = Path.Combine(Deployment.BinPath, Path.GetFileName(Deployment.LauncherLibPath));
            Utils.CopyFileIfNewer(Deployment.LauncherLibPath, launcherLibPath);

            var tasks = new List<Task>();
            foreach (var tool in Deployment.Tools)
            {
                tasks.Add(Task.Run(() => { tool.Install(Deployment); }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void ProcessCommands()
        {
            foreach (var tool in Deployment.Tools)
            {
                tool.PostInstall(Deployment);
            }
        }

        public void CleanUnused()
        {
            foreach (string directory in Directory.GetDirectories(Deployment.InstallPath))
            {
                string sentinelFile = Path.Combine(directory, "__deployer__");
                if (!File.Exists(sentinelFile) || File.GetLastWriteTimeUtc(sentinelFile) != Deployment.TimeStamp)
                {
                    Utils.Log("Deleting unused package {0}", directory);
                    Directory.Delete(directory, true);
                }
            }

            foreach (string file in Directory.GetFiles(Deployment.BinPath, "*.cfg"))
            {
                if (File.GetLastWriteTimeUtc(file) != Deployment.TimeStamp)
                {
                    Utils.Log("Deleting unused alias {0}", Path.GetFileNameWithoutExtension(file));

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
    }
}
