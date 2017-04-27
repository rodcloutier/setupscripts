using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using NuGet;


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

    internal class GitRepository : Repository
    {
        public GitRepository(string source, string installPath)
            : base(installPath)
        {
            Source = source;
        }

        string Source { get; }


        // TODO move to utils
        internal static bool IsPath(string pathCandidate)
        {
            System.IO.FileInfo fi = null;
            try
            {
                fi = new System.IO.FileInfo(pathCandidate);
            }
            catch (ArgumentException) { }
            catch (System.IO.PathTooLongException) { }
            catch (NotSupportedException) { }
            return !ReferenceEquals(fi, null);
        }

        // Validate that the requested package git repository exists
        // and the requested version also exists
        // Currently does not support git sha1 as version
        internal override bool Exists(string packageFullString)
        {
            // TODO refactor to support . in names

            var components = packageFullString.Split('.');
            var package = components[0];
            var version = String.Join(".", components.Skip(1).Take(components.Length - 1).ToArray());

            string repo = "";
            if (IsPath(Source))
            {
                repo = Path.Combine(Source, package);
            }
            else
            {
                repo = string.Format("{0}/{1}.git", Source, package);
            }

            var tempRepoPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempRepoPath);
            var tempRepoContext = "-C " + tempRepoPath;
            Git.run(tempRepoContext, "init");

            Git.run(tempRepoContext, "remote", "add", "origin", repo);

            try
            {
                Git.run(tempRepoContext, "ls-remote", "origin", "--exit-code", "--tags", version);
            }
            catch( Exception )
            {
                return false;
            }

            return true;
        }

        internal override void InstallPackage(Package p, string packageFullString)
        {
            // TODO (rod): If the version is master, we might want to act
            // differently: refuse, change to sha, always replace, etc.

            Utils.Log("Installing Package {0} from {1}", packageFullString, Source);

            // TODO detect that we might have a full git path?
            // TODO extract the version from packageFullString
            var repo = string.Format("{0}/{1}.git", Source, p.PackageId);
            var installPath = GetInstallPath(p);

            Utils.Log("--Cloning repository {0}", repo.ToString());
            // Git.run("clone", repo.ToString(), installPath);

            Utils.Log("--Checking out version {0}", p.Version);
            // Git.run("-C", installPath, "checkout", p.Version);
        }
    }
}
