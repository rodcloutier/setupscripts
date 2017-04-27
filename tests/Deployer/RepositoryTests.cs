using System;
using System.IO;
using NUnit.Framework;

using CSLauncher.Deployer;


namespace CSLauncher.Deployer.Tests
{
    [TestFixture]
    public class GitRepositoryExistTests
    {
        // TODO write test to document that git sha1 are not currently valid versions

        [Test]
        public void MasterTagAlwaysExists()
        {
            // ---
            var sourcePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var repoPath = Path.Combine(sourcePath, "mypackage");

            Directory.CreateDirectory(repoPath);

            var repoContext = "-C " + repoPath;
            Git.run(repoContext, "init");
            Git.run(repoContext, "commit", "--allow-empty", "-m", "\"Empty\"");

            GitRepository repo = new GitRepository(sourcePath, "installPath");

            // call -----------------------------------------------------------
            var exists = repo.Exists("mypackage.MyBranch");

            // test -----------------------------------------------------------
            Assert.IsTrue(exists);
        }

        [Test]
        public void CanCheckThatABranchExists()
        {
            // context -------------------------------------------------------
            var sourcePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var repoPath = Path.Combine(sourcePath, "mypackage");

            Directory.CreateDirectory(repoPath);

            var repoContext = "-C " + repoPath;

            Git.run(repoContext, "init");
            Git.run(repoContext, "commit", "--allow-empty", "-m", "\"Empty\"");
            Git.run(repoContext, "checkout", "-b", "MyBranch");

            GitRepository repo = new GitRepository(sourcePath, "installPath");

            // call -----------------------------------------------------------
            var exists = repo.Exists("mypackage.MyBranch");

            // test -----------------------------------------------------------
            Assert.IsTrue(exists);
        }

        [Test]
        public void ReturnsFalseIfRepositoryDoesNotExist()
        {
            // context -------------------------------------------------------
            GitRepository repo = new GitRepository("invalid_source", "installPath");

            // Call -----------------------------------------------------------
            var exists = repo.Exists("mypackage.master");

            // test -----------------------------------------------------------
            Assert.IsFalse(exists);
        }
    }

    [TestFixture]
    public class GitRepositoryInstallTests
    {
        [Test]
        public void SuccessfullInstall()
        {
            // context -------------------------------------------------------
            var packageId = "mypackage";
            var sourcePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var repoPath = Path.Combine(sourcePath, packageId);

            Directory.CreateDirectory(repoPath);

            var repoContext = "-C " + repoPath;

            Git.run(repoContext, "init");
            Git.run(repoContext, "commit", "--allow-empty", "-m", "\"Empty\"");

            var installPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            GitRepository repository = new GitRepository(sourcePath, installPath);

            Package package = new Package(packageId, null, "master", new DeploymentFile.Command[]{}, repository);

            // Call -----------------------------------------------------------
            repository.Install(package, package.ToFullString());

            // test -----------------------------------------------------------
            Assert.IsTrue(Directory.Exists(Path.Combine(installPath, package.ToFullString())));
        }
    }
}
