using CSLauncher.LauncherLib;
using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSLauncher.Deployer
{
    internal class Deployment
    {
        internal Deployment(List<DeploymentFile> deploymentFiles, string optionBinPath, string optionInstallPath, DateTime deploymentStamp)
        {
            ConfigMapping = new Dictionary<string, string>();
            BinPath = InitBinPath(deploymentFiles, optionBinPath);
            InstallPath = InitInstallPath(deploymentFiles, optionInstallPath);
            HttpProxy = InitHttpProxy(deploymentFiles);
            LauncherPath = Utils.RootPathToExePath("Launcher.exe");
            LauncherLibPath = Utils.RootPathToExePath("LauncherLib.dll");
            TimeStamp = deploymentStamp;
            Repositories = InitRepositories(deploymentFiles);
            Packages = InitPackages(deploymentFiles);
            Tools = InitTools(deploymentFiles);
            FixupConfigValues();
        }

        internal string BinPath { get; }
        internal string InstallPath { get; }
        internal string LauncherPath { get; }
        internal string LauncherLibPath { get; }
        internal string HttpProxy { get; }
        internal DateTime TimeStamp { get; }
        internal Dictionary<string, Repository> Repositories { get; }
        internal Dictionary<string, List<Package>> Packages { get; }
        internal List<Tool> Tools { get; }
        internal Dictionary<string, string> ConfigMapping { get; }

        private string InitBinPath(List<DeploymentFile> deploymentFiles, string optionBinPath)
        {
            string binPath = optionBinPath;
            foreach (var deploymentFile in deploymentFiles)
            {
                binPath = Utils.OverrideString(binPath, deploymentFile.BinPath);
            }

            if (binPath == null)
            {
                binPath = Path.Combine("%USERPROFILE%", "bin");
            }

            binPath = Utils.NormalizePath(binPath);
            Utils.Log("Using bin path: {0}", binPath);
            AddConfigMapping("binPath", binPath);
            return binPath;
        }

        private string InitInstallPath(List<DeploymentFile> deploymentFiles, string optionInstallPath)
        {
            string installPath = optionInstallPath;
            foreach (var deploymentFile in deploymentFiles)
            {
                installPath = Utils.OverrideString(installPath, deploymentFile.InstallPath);
            }

            if (installPath == null)
            {
                installPath = Path.Combine(BinPath, "packages");
            }

            installPath = Utils.NormalizePath(installPath);
            Utils.Log("Using install path: {0}", installPath);
            AddConfigMapping("installPath", installPath);
            return installPath;
        }

        private string InitHttpProxy(List<DeploymentFile> deploymentFiles)
        {
            string httpProxy = null;
            foreach (var deploymentFile in deploymentFiles)
            {
                httpProxy = Utils.OverrideString(httpProxy, deploymentFile.HttpProxy);
            }
            if (httpProxy != null)
            {
                Utils.Log("Using http proxy: {0}", httpProxy);
            }
            return httpProxy;
        }

        private Dictionary<string, Repository> InitRepositories(List<DeploymentFile> deploymentFiles)
        {
            var repositoriesClasses =  new Dictionary<string, System.Type>()
            {
                {"directory", typeof(DirectoryRepository)},
                {"nuget", typeof(NugetRepository)}
            };

            var repositories = new Dictionary<string, Repository>();
            foreach (var deploymentFile in deploymentFiles)
            {
                if (deploymentFile.Repositories == null)
                    continue;
                foreach (var fileRepo in deploymentFile.Repositories)
                {
                    if (string.IsNullOrEmpty(fileRepo.Id))
                        throw new InvalidDataException(string.Format("Missing repository id in {0}", deploymentFile.FileName));
                    if (string.IsNullOrEmpty(fileRepo.Type))
                        throw new InvalidDataException(string.Format("Missing repository type in {0} for {1}", deploymentFile.FileName, fileRepo.Id));
                    if (string.IsNullOrEmpty(fileRepo.Source))
                        throw new InvalidDataException(string.Format("Missing repository source in {0} for {1}", deploymentFile.FileName, fileRepo.Id));

                    if (repositories.ContainsKey(fileRepo.Id))
                        Utils.Log("Overriding repository {0} with the one deployment file {1}", fileRepo.Id, deploymentFile.FileName);
                    else
                        Utils.Log("Using repository {0}", fileRepo.Id);

                    var type = fileRepo.Type.ToLower();

                    var args = new object[]{fileRepo.Source, InstallPath};
                    repositories[fileRepo.Id] = Activator.CreateInstance(repositoriesClasses[type], args) as Repository;
                }
            }

            return repositories;
        }

        private Dictionary<string, List<Package>> InitPackages(List<DeploymentFile> deploymentFiles)
        {
            var packagesDict = new Dictionary<string, Dictionary<string, Package>>();
            foreach (var deploymentFile in deploymentFiles)
            {
                if (deploymentFile.Packages == null)
                    continue;
                foreach (var filePackage in deploymentFile.Packages)
                {
                    Package package = Package.CreatePackage(filePackage, Repositories, deploymentFile.FileName);

                    if (!packagesDict.ContainsKey(filePackage.Id))
                    {
                        packagesDict[package.PackageId] = new Dictionary<string, Package>();
                    }
                    if (packagesDict[package.PackageId].ContainsKey(package.PackageId))
                        Utils.Log("Overriding package {0} with the one on deployment file {1}", package.PackageId, deploymentFile.FileName);
                    else
                        Utils.Log("Using package {0}", package.PackageId);

                    packagesDict[package.PackageId][package.PackageId + "." + package.VersionString()] = package;
                }
            }
            var packages = new Dictionary<string, List<Package>>();
            foreach (var entry in packagesDict)
            {
                var list = new List<Package>(entry.Value.Count);
                foreach(var packageEntry in entry.Value)
                {
                    list.Add(packageEntry.Value);
                    AddConfigMapping("package-" + packageEntry.Value.ToFullString(), packageEntry.Value.InstallPath);
                }
                list.Sort((a, b) => { return b.CompareTo(a); });
                packages[entry.Key] = list;
            }
            return packages;
        }

        private List<Tool> InitTools(List<DeploymentFile> deploymentFiles)
        {
            var toolsDict = new Dictionary<string, List<Tool>>();
            var aliasesSet = new HashSet<string>();
            foreach (var deploymentFile in deploymentFiles)
            {
                if (deploymentFile.Toolsets == null)
                    continue;

                foreach (var toolset in deploymentFile.Toolsets)
                {
                    if (string.IsNullOrEmpty(toolset.Id))
                        throw new InvalidDataException(string.Format("Missing toolset id in {0}", deploymentFile.FileName));

                    Package package = GetCompatiblePackage(toolset.PackageSpec);
                    if (!string.IsNullOrEmpty(toolset.PackageSpec) && package == null)
                        throw new InvalidDataException(string.Format("Could not resolve the packageId for toolset {0} in {1} for file {2}",
                            toolset.Id, toolset.PackageSpec, deploymentFile.FileName));
                    string packageInstallPath = null;
                    if (package != null)
                    {
                        Utils.Log("Using package {0} for toolset {1}", toolset.PackageSpec, toolset.Id);
                        package.IsUsed = true;
                        packageInstallPath = package.InstallPath;
                    }

                    List<Tool> newTools = InitTools(toolset.Id, toolset.Tools, packageInstallPath, deploymentFile);
                    if (toolsDict.ContainsKey(toolset.Id))
                        Utils.Log("Overriding toolset {0} with the one on deployment file {1}", toolset.Id, deploymentFile.FileName);
                    else
                        Utils.Log("Using toolset {0}", toolset.Id);

                    toolsDict[toolset.Id] = newTools;
                }
            }
            var tools = new List<Tool>(toolsDict.Count);
            foreach (var entry in toolsDict)
            {
                foreach (var tool in entry.Value)
                {
                    tools.Add(tool);
                    foreach (var alias in tool.Aliases)
                    {
                        if (aliasesSet.Contains(alias))
                            throw new InvalidDataException(string.Format("Conflicting aliases {0}", alias));
                        aliasesSet.Add(alias);
                    }
                }
            }
            return tools;
        }

        private Package GetCompatiblePackage(string packageSpec)
        {
            if (string.IsNullOrEmpty(packageSpec))
                return null;

            string packageId;
            string version;
            Utils.VersionOp vOp;
            Utils.ParsePackageSpecComponents(packageSpec, out packageId, out vOp, out version);

            if (!Packages.ContainsKey(packageId))
                return null;

            try
            {

                Utils.VersionComponent versionComponent;
                SemanticVersion semVer = null;
                Utils.ParseVersionComponent(version, out semVer, out versionComponent);

                Package foundPackage = null;
                foreach (var potentialPackage in Packages[packageId])
                {
                    // If we have a semantic version in the potentialPackage
                    if( potentialPackage.SemVersion != null )
                    {
                        if (Utils.ValidPackage(semVer, potentialPackage.SemVersion, versionComponent, vOp))
                        {
                            foundPackage = potentialPackage;
                            break;
                        }
                    }
                }
                if( foundPackage == null && version == "0")
                {
                    // We did not find any semantic version in any package and we are
                    // not asked for a version
                    // We take the first one with no sem ver since we have no cue which to
                    // choose
                    // TODO. Warn the user if there are more that could be
                    // resolved?
                    foreach (var potentialPackage in Packages[packageId])
                    {
                        if (potentialPackage.SemVersion == null )
                        {
                            foundPackage = potentialPackage;
                            break;
                        }
                    }
                }

                return foundPackage;
            }
            catch( InvalidDataException )
            {
                // We are in a case where the asked version cannot be resolved to a
                // semantic version. Try to do an exact compare

                // If any of the packages repository requires semver, bail out...
                foreach (var package in Packages[packageId])
                {
                    if (package.Repository.RequiresSemanticVersion)
                    {
                        Utils.Log("Repository {} requires a semantic version. '{1}' is not a semantic version", package.Repository, version);
                        throw;
                    }
                }

                // Find the valid package
                foreach (var potentialPackage in Packages[packageId])
                {
                    if ( version.Equals(potentialPackage.Version) )
                        return potentialPackage;
                }
            }

            return null;
        }

        private List<Tool> InitTools(string toolset, DeploymentFile.Tool[] fileTools, string packageInstallPath, DeploymentFile deploymentFile)
        {
            var tools = new List<Tool>();
            foreach (var fileTool in fileTools)
            {
                if (string.IsNullOrEmpty(fileTool.Path))
                    throw new InvalidDataException(string.Format("Missing tool path in {0}", deploymentFile.FileName));
                if (fileTool.Aliases.Length == 0)
                    throw new InvalidDataException(string.Format("Missing tool aliases in {0} for {1}", fileTool.Path, deploymentFile.FileName));

                string toolInstallPath = Environment.ExpandEnvironmentVariables(fileTool.Path);
                if (!string.IsNullOrEmpty(packageInstallPath))
                {
                    if (Path.IsPathRooted(toolInstallPath))
                        throw new InvalidDataException(string.Format("Tools with linked packages can't have rooted paths: {0}", fileTool.Path));

                    toolInstallPath = Path.GetFullPath(Path.Combine(packageInstallPath, toolInstallPath));
                }
                else
                {
                    if (!Path.IsPathRooted(toolInstallPath))
                        throw new InvalidDataException(string.Format("Tools without linked packages need rooted paths: {0}", toolInstallPath));
                }

                switch (fileTool.Type)
                {
                    case "exe":
                        tools.Add(new ExeTool(toolset, fileTool.Aliases, toolInstallPath, fileTool.Blocking, fileTool.InitialArgs, Utils.InitEnvVariables(fileTool.EnvVariables)));
                        break;
                    case "bash":
                        tools.Add(new BashTool(toolset, fileTool.Aliases, toolInstallPath, Utils.InitEnvVariables(fileTool.EnvVariables)));
                        break;
                    default:
                        throw new InvalidDataException(string.Format("Tool type {0} not supported", fileTool.Type));
                }

                AddConfigMapping("tool-" + fileTool.Aliases[0], toolInstallPath);
            }

            return tools;
        }

        private void FixupConfigValues()
        {
            foreach (var entry in Packages)
            {
                foreach (var package in entry.Value)
                {
                    foreach (var command in package.Commands)
                    {
                        if (command.EnvVariables != null)
                        {
                            for (int i = 0; i < command.EnvVariables.Length; ++i)
                            {
                                if (!string.IsNullOrEmpty(command.EnvVariables[i].Value))
                                {
                                    SetConfigValue(ref command.EnvVariables[i].Value);
                                    command.EnvVariables[i].Value = Environment.ExpandEnvironmentVariables(command.EnvVariables[i].Value);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var tool in Tools)
            {
                if (tool.EnvVariables != null)
                {
                    for (int i = 0; i < tool.EnvVariables.Length; ++i)
                    {
                        if (!string.IsNullOrEmpty(tool.EnvVariables[i].Value))
                        {
                            SetConfigValue(ref tool.EnvVariables[i].Value);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(tool.InitalArgs))
                {
                    SetConfigValue(ref tool.InitalArgs);
                }
            }
        }

        private void AddConfigMapping(string key, string value)
        {
            ConfigMapping["{" + key + "}"] = value;
        }

        private void SetConfigValue(ref string str)
        {
            if (string.IsNullOrEmpty(str))
                return;

            MatchCollection mc = Regex.Matches(str, @"\{(.*?)\}");
            foreach (Match m in mc)
            {
                try
                {
                    str = str.Replace(m.ToString(), ConfigMapping[m.ToString()]);
                }
                catch (KeyNotFoundException)
                {
                    throw new KeyNotFoundException("Failed to find key " + m.ToString());
                }
            }
        }
    }
}
