using NuGet;

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
        }

        internal string PackageId { get; }
        internal SemanticVersion Version { get; }
        internal Repository Repository { get; }
        internal string InstallPath
        {
            get { return Repository.GetInstallPath(this); }
        }
        internal bool IsUsed { get; set; }

        internal bool PreInstall(Deployment deployment) { return Repository.PreInstallPackage(deployment, this); }

        internal void Install(Deployment deployment) { Repository.InstallPackage(deployment, this); }

        internal string ToFullString() { return PackageId + "-" + Version.ToFullString(); }
    }
}