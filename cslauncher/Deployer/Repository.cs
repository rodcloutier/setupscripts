using NuGet;
using System.IO;
using System;
using System.Net;
using System.IO.Compression;

namespace CSLauncher.Deployer
{
    internal abstract class Repository
    {
        internal Repository(string installPath)
        {
            InstallPath = installPath;
        }

        internal string InstallPath { get; }

        internal virtual string GetInstallPath(Package p)
        {
            throw new NotImplementedException();
        }

        internal virtual bool PreInstallPackage(Deployment deployment, Package p)
        {
            return true;
        }

        internal virtual void InstallPackage(Deployment deployment, Package p)
        {
            throw new NotImplementedException();
        }
    }

    internal class HttpRepository : Repository
    {
        internal HttpRepository(string source, string installPath)
            : base(installPath)
        {
            if (!source.EndsWith("/"))
                Source = source + '/';
            else
                Source = source;
        }

        string Source { get; }

        internal override string GetInstallPath(Package p)
        {
            return Path.Combine(InstallPath, p.ToFullString());
        }

        internal override bool PreInstallPackage(Deployment deployment, Package p)
        {
            string sentinelFile = Path.Combine(p.InstallPath, "__deployer__");
            return !File.Exists(sentinelFile) || File.GetLastWriteTimeUtc(sentinelFile) != deployment.TimeStamp;
        }

        internal override void InstallPackage(Deployment deployment, Package p)
        {
            Utils.Log("Installing Package {0} from {1}", p.ToFullString(), Source);

            string packageInstallPath = p.InstallPath;
            string zipFileName = p.ToFullString() + ".zip";
            string remoteZipFilePath = Source + zipFileName;
            string localZipFilePath = InstallPath + '\\' + zipFileName;

            if (!Directory.Exists(p.InstallPath))
            {
                Utils.Log("--Downloading {0}", remoteZipFilePath);
                using (var client = new WebClient())
                {
                    client.DownloadFile(remoteZipFilePath, localZipFilePath);
                }

                Utils.Log("--Unzipping {0} to {1}", localZipFilePath, localZipFilePath);
                ZipFile.ExtractToDirectory(localZipFilePath, packageInstallPath);

                Utils.Log("--Deleting {0}", localZipFilePath);
                File.Delete(localZipFilePath);
            }
            string sentinelFile = Path.Combine(packageInstallPath, "__deployer__");
            using (var file = File.Open(sentinelFile, FileMode.OpenOrCreate)) { }
            File.SetLastWriteTimeUtc(sentinelFile, deployment.TimeStamp);
        }
    }

    internal class DirectoryRepository : Repository
    {
        internal DirectoryRepository(string source, string installPath)
            : base(installPath)
        {
            if (!Directory.Exists(source))
                throw new InvalidDataException(string.Format("Directory source {0} does't exist", source));

            if (!source.EndsWith("\\"))
                Source = source + '\\';
            else
                Source = source;
        }

        string Source { get; }
        

        internal override string GetInstallPath(Package p)
        {
            return Path.Combine(InstallPath, p.ToFullString());
        }

        internal override bool PreInstallPackage(Deployment deployment, Package p)
        {
            string sentinelFile = Path.Combine(p.InstallPath, "__deployer__");
            return !File.Exists(sentinelFile) || File.GetLastWriteTimeUtc(sentinelFile) != deployment.TimeStamp;
        }

        internal override void InstallPackage(Deployment deployment, Package p)
        {
            Utils.Log("Installing Package {0} from {1}", p.ToFullString(), Source);

            string packageInstallPath = p.InstallPath;
            string zipFileName = p.ToFullString() + ".zip";
            string remoteZipFilePath = Source + zipFileName;
            string localZipFilePath = InstallPath + '\\' + zipFileName;

            if (!Directory.Exists(packageInstallPath))
            {
                Utils.Log("--Copying {0}", remoteZipFilePath);
                File.Copy(remoteZipFilePath, localZipFilePath);

                Utils.Log("--Unzipping {0} to {1}", localZipFilePath, localZipFilePath);
                ZipFile.ExtractToDirectory(localZipFilePath, packageInstallPath);

                Utils.Log("--Deleting {0}", localZipFilePath);
                File.Delete(localZipFilePath);
            }
            string sentinelFile = Path.Combine(packageInstallPath, "__deployer__");
            using (var file = File.Open(sentinelFile, FileMode.OpenOrCreate)) { }
            File.SetLastWriteTimeUtc(sentinelFile, deployment.TimeStamp);
        }
    }

    internal class NugetRepository : Repository
    {
        internal NugetRepository(string source, string installPath)
            : base(installPath)
        {
            NugetRepo = PackageRepositoryFactory.Default.CreateRepository(source);
            PackageManager = new PackageManager(NugetRepo, InstallPath);
        }

        IPackageRepository NugetRepo { get; }
        PackageManager PackageManager { get; }

        internal override bool PreInstallPackage(Deployment deployment, Package p)
        {
            string sentinelFile = Path.Combine(InstallPath, PackageManager.PathResolver.GetPackageDirectory(p.PackageId, p.Version), "__deployer__");
            return !File.Exists(sentinelFile) || File.GetLastWriteTimeUtc(sentinelFile) != deployment.TimeStamp;
        }

        internal override string GetInstallPath(Package p)
        {
            return Path.Combine(InstallPath, PackageManager.PathResolver.GetPackageDirectory(p.PackageId, p.Version) + "\\tool");
        }

        internal override void InstallPackage(Deployment deployment, Package p)
        {
            PackageManager.InstallPackage(p.PackageId, p.Version);

            string sentinelFile = Path.Combine(InstallPath, PackageManager.PathResolver.GetPackageDirectory(p.PackageId, p.Version), "__deployer__");
            using (var file = File.Open(sentinelFile, FileMode.OpenOrCreate)) { }
            File.SetLastWriteTimeUtc(sentinelFile, deployment.TimeStamp);
        }
    }
}