using NuGet;
using System.Collections.Generic;

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
            return Repository.PreInstallPackage(deployment, this);
        }

        internal void Install(Deployment deployment)
        {
            Repository.InstallPackage(deployment, this);
        }

        internal string ToFullString() { return PackageId + "-" + Version.ToFullString(); }
    }
}