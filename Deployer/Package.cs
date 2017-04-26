using NuGet;
using System;
using System.Collections.Generic;
using System.IO;

namespace CSLauncher.Deployer
{
    internal class Package
    {
        static public Package CreatePackage(DeploymentFile.Package filePackage, Dictionary<string, Repository> repositories, string sourceFile)
        {
            if (string.IsNullOrEmpty(filePackage.Id))
                throw new InvalidDataException(string.Format("Missing package id in {0}", sourceFile));
            if (string.IsNullOrEmpty(filePackage.Version))
                throw new InvalidDataException(string.Format("Missing package version in {0} for {1}", sourceFile, filePackage.Id));
            if (string.IsNullOrEmpty(filePackage.SourceID))
                throw new InvalidDataException(string.Format("Missing package sourceId in {0} for {1}", sourceFile, filePackage.Id));
            if (!repositories.ContainsKey(filePackage.SourceID))
                throw new InvalidDataException(string.Format("sourceId {0} used by package {1} not found", filePackage.SourceID, filePackage.Id));

            var repository = repositories[filePackage.SourceID];
            SemanticVersion semVer = null;
            try
            {
                semVer = Utils.ParseVersion(filePackage.Version);
            }
            catch( InvalidDataException )
            {
                if( repository.RequiresSemanticVersion )
                {
                    // TODO raise custom error to explain
                    throw;
                }
            }

            return new Package(filePackage.Id, semVer, filePackage.Version, filePackage.Commands, repository);
        }

        internal Package(string packageId, SemanticVersion semVersion, string version, DeploymentFile.Command[] commands, Repository repository)
        {
            PackageId = packageId;
            SemVersion = semVersion;
            Version = version;
            Repository = repository;
            IsUsed = false;
            Commands = new List<Command>();
            if (commands != null && commands.Length > 0)
            {
                foreach (var fileCommand in commands)
                {
                    string commandPath = Environment.ExpandEnvironmentVariables(fileCommand.FilePath); ;
                    if (!string.IsNullOrEmpty(commandPath) && !Path.IsPathRooted(commandPath))
                    {
                        commandPath = Path.GetFullPath(Path.Combine(InstallPath, commandPath));
                    }
                    Commands.Add(new Command(commandPath, fileCommand.Arguments, Utils.InitEnvVariables(fileCommand.EnvVariables)));
                }
            }
        }

        internal string PackageId { get; }
        internal SemanticVersion SemVersion { get; }
        internal string Version { get; }
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

                // We will try to see if we can install the package using the semantic
                // version name first. If not default to using the version name
                bool useSemanticVersion = (SemVersion != null);
                string fullString = this.ToFullString(useSemanticVersion);
                if( !Repository.Exists(this.ToFullString(useSemanticVersion)) )
                {
                    fullString = this.ToFullString();
                }

                Repository.InstallPackage(this, fullString);
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

        internal string ToFullString(bool useSemanticVersion = false) {
            return PackageId + "." + VersionString(useSemanticVersion);
        }

        internal string VersionString(bool useSemanticVersion = false) {
            return (useSemanticVersion)?SemVersion.ToFullString():Version;
        }

        internal int CompareTo(Package package) {
            if (SemVersion != null )
                return SemVersion.CompareTo(package.SemVersion);
            return Version.CompareTo(package.Version);
        }

    }
}
