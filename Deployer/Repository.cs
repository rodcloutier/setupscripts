using NuGet;
using System.IO;
using System;
using System.Net;
using System.IO.Compression;

namespace CSLauncher.Deployer
{
    internal abstract class Repository
    {
        internal Repository(string installPath, bool requiresSemanticVersion = false)
        {
            InstallPath = installPath;
            RequiresSemanticVersion = requiresSemanticVersion;
        }

        internal string InstallPath { get; }
        internal bool RequiresSemanticVersion { get; }

        internal virtual string GetInstallPath(Package p)
        {
            return Path.Combine(InstallPath, p.ToFullString());
        }

        internal virtual string GetDownloadPath(Package p)
        {
            return GetInstallPath(p);
        }

        internal virtual void InstallPackage(Package p, string packageFullString)
        {
            throw new NotImplementedException();
        }

        internal virtual bool Exists(string packageFullString)
        {
            return true;
        }
    }

    internal class HttpRepository : Repository
    {
        public HttpRepository(string source, string installPath)
            : base(installPath)
        {
            Source = new Uri(source);
        }

        Uri Source { get; }

        internal override bool Exists(string packageFullString)
        {
            throw new NotImplementedException();
        }

        internal override void InstallPackage(Package p, string packageFullString)
        {
            Utils.Log("Installing Package {0} from {1}", packageFullString, Source);

            string packageInstallPath = p.InstallPath;
            string zipFileName = packageFullString + ".zip";

            Uri remoteArchiveUri = new Uri(Source, zipFileName);
            string localZipFilePath = Path.Combine(InstallPath, zipFileName);

            Utils.Log("--Downloading {0}", remoteArchiveUri);
            using (var client = new WebClient())
            {
                client.DownloadFile(remoteArchiveUri, localZipFilePath);
            }

            Utils.Log("--Unzipping {0} to {1}", localZipFilePath, localZipFilePath);
            ZipFile.ExtractToDirectory(localZipFilePath, packageInstallPath);

            Utils.Log("--Deleting {0}", localZipFilePath);
            File.Delete(localZipFilePath);
        }
    }

    internal class DirectoryRepository : Repository
    {
        public DirectoryRepository(string source, string installPath)
            : base(installPath)
        {
            if (!Directory.Exists(source))
                throw new InvalidDataException(string.Format("Directory source {0} doesn't exist", source));
            Source = source;
        }

        string Source { get; }

        internal override bool Exists(string packageFullString)
        {
            string zipFileName = packageFullString + ".zip";
            string remoteZipFilePath = Path.Combine(Source, zipFileName);

            return File.Exists(remoteZipFilePath);
        }

        internal override void InstallPackage(Package p, string packageFullString)
        {
            Utils.Log("Installing Package {0} from {1}", packageFullString, Source);

            string packageInstallPath = p.InstallPath;
            string zipFileName = packageFullString + ".zip";
            string remoteZipFilePath = Path.Combine(Source, zipFileName);
            string localZipFilePath = Path.Combine(InstallPath, zipFileName);

            Utils.Log("--Copying {0}", remoteZipFilePath);
            File.Copy(remoteZipFilePath, localZipFilePath);

            Utils.Log("--Unzipping {0} to {1}", localZipFilePath, packageInstallPath);
            ZipFile.ExtractToDirectory(localZipFilePath, packageInstallPath);

            Utils.Log("--Deleting temp file {0}", localZipFilePath);
            File.Delete(localZipFilePath);
        }
    }

    internal class NugetRepository : Repository
    {
        public NugetRepository(string source, string installPath)
            : base(installPath, true)
        {
            NugetRepo = PackageRepositoryFactory.Default.CreateRepository(source);
            PackageManager = new PackageManager(NugetRepo, InstallPath);
        }

        IPackageRepository NugetRepo { get; }
        PackageManager PackageManager { get; }

        internal override string GetInstallPath(Package p)
        {
            return Path.Combine(GetDownloadPath(p), "tools");
        }

        internal override string GetDownloadPath(Package p)
        {
            return Path.Combine(InstallPath, PackageManager.PathResolver.GetPackageDirectory(p.PackageId, p.SemVersion));
        }

        internal override void InstallPackage(Package p, string packageFullString)
        {
            PackageManager.InstallPackage(p.PackageId, p.SemVersion);
        }
    }
}
