using NuGet;
using System;
using System.Collections.Generic;
using System.IO;

namespace CSLauncher.Deployer
{
    internal class Package
    {
        internal Package(string packageId, SemanticVersion version, Repository repository)
        {
            PackageId = "Deployer." + packageId;
            Version = version;
            Repository = repository;
            IsUsed = false;
            Commands = new List<Command>();
        }

        internal string PackageId { get; }
        internal SemanticVersion Version { get; }
        internal Repository Repository { get; }
        internal List<Command> Commands { get; }
        internal string InstallPath
        {
            get { return Repository.GetInstallPath(this); }
        }
        internal string DownloadPath
        {
            get { return Repository.GetDownloadPath(this); }
        }
        internal bool IsUsed { get; set; }

        internal bool PreInstall(Deployment deployment)
        {
            string sentinelFile = Path.Combine(DownloadPath, "__deployer__");
            return !File.Exists(sentinelFile) || File.GetLastWriteTimeUtc(sentinelFile) != deployment.TimeStamp;
        }

        internal void Install(Deployment deployment)
        {
            string sentinelFile = Path.Combine(DownloadPath, "__deployer__");
            
            if (!File.Exists(sentinelFile))
            {
                if (Directory.Exists(DownloadPath))
                {
                    Directory.Delete(DownloadPath, true);
                }

                Repository.InstallPackage(deployment, this);
                foreach (var command in Commands)
                {
                    command.Run(deployment);
                }
            }
            else
            {
                string[] commandHashes = File.ReadAllLines(sentinelFile);
                foreach (var command in Commands)
                {
                    if (!Array.Exists(commandHashes, (s) => { return s == command.Hash; }))
                        command.Run(deployment);
                }
            }
        }

        internal void PostInstall(Deployment deployment)
        {
            string sentinelFile = Path.Combine(DownloadPath, "__deployer__");

            using (var file = File.Open(sentinelFile, FileMode.OpenOrCreate))
            {
                using (StreamWriter stream = new StreamWriter(file))
                {
                    foreach (var command in Commands)
                    {
                        stream.WriteLine(command.Hash);
                    }
                }
            }
            File.SetLastWriteTimeUtc(sentinelFile, deployment.TimeStamp);
        }

        internal string ToFullString() { return PackageId + "." + Version.ToFullString(); }
    }
}