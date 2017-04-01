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
            Utils.Log("Using bin path: {0}", installPath);
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

                    Repository newRepo = null;
                    string type = fileRepo.Type.ToLower();
                    switch (type)
                    {
                        case "directory":
                            newRepo = new DirectoryRepository(fileRepo.Source, InstallPath);
                            break;
                        case "nuget":
                            newRepo = new NugetRepository(fileRepo.Source, InstallPath);
                            break;
                        default:
                            throw new InvalidDataException(string.Format("Invalid repository type in {0} for {1}", deploymentFile.FileName, fileRepo.Id));
                    }
                    if (repositories.ContainsKey(fileRepo.Id))
                        Utils.Log("Overriding repository {0} with the one deployment file {1}", fileRepo.Id, deploymentFile.FileName);
                    else
                        Utils.Log("Using repository {0}", fileRepo.Id);

                    repositories[fileRepo.Id] = newRepo;
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
                    if (string.IsNullOrEmpty(filePackage.Id))
                        throw new InvalidDataException(string.Format("Missing package id in {0}", deploymentFile.FileName));
                    if (string.IsNullOrEmpty(filePackage.Version))
                        throw new InvalidDataException(string.Format("Missing package version in {0} for {1}", deploymentFile.FileName, filePackage.Id));
                    if (string.IsNullOrEmpty(filePackage.SourceID))
                        throw new InvalidDataException(string.Format("Missing package sourceId in {0} for {1}", deploymentFile.FileName, filePackage.Id));
                    if (!Repositories.ContainsKey(filePackage.SourceID))
                        throw new InvalidDataException(string.Format("sourceId {0} used by package {1}}", filePackage.SourceID, filePackage.Id));

                    SemanticVersion semVer = Utils.ParseVersion(filePackage.Version);
                    Package package = new Package(filePackage.Id, semVer, Repositories[filePackage.SourceID]);
                    if (!packagesDict.ContainsKey(filePackage.Id))
                    {
                        packagesDict[filePackage.Id] = new Dictionary<string, Package>();
                    }
                    if (packagesDict[filePackage.Id].ContainsKey(filePackage.Id))
                        Utils.Log("Overriding package {0} with the one on deployment file {1}", filePackage.Id, deploymentFile.FileName);
                    else
                        Utils.Log("Using package {0}", filePackage.Id);

                    packagesDict[filePackage.Id][filePackage.Id + "." + filePackage.Version] = package;

                    if (filePackage.Commands != null && filePackage.Commands.Length > 0)
                    {
                        var commandList = package.Commands;
                        foreach (var fileCommand in filePackage.Commands)
                        {
                            string commandPath = Environment.ExpandEnvironmentVariables(fileCommand.FilePath); ;
                            if (!string.IsNullOrEmpty(commandPath) && !Path.IsPathRooted(commandPath))
                            {
                                commandPath = Path.GetFullPath(Path.Combine(package.InstallPath, commandPath));
                            }
                            commandList.Add(new Command(commandPath, fileCommand.Arguments, InitEnvVariables(fileCommand.EnvVariables)));
                        }
                    }
                }
            }
            var packages = new Dictionary<string, List<Package>>();
            foreach (var entry in packagesDict)
            {
                
                var list = new List<Package>(entry.Value.Count);
                foreach(var packageEnty in entry.Value)
                {
                    list.Add(packageEnty.Value);
                    AddConfigMapping("package-" + packageEnty.Value.ToFullString(), packageEnty.Value.InstallPath);
                }
                list.Sort((a, b) => { return b.Version.CompareTo(a.Version); });
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
            if (!string.IsNullOrEmpty(packageSpec))
            {
                string packageId;
                SemanticVersion semVer;
                Utils.VersionOp vOp;
                Utils.VersionComponent filterComponent;
                Utils.GetPackageSpecComponents(packageSpec, out packageId, out vOp, out semVer, out filterComponent);

                if (Packages.ContainsKey(packageId))
                {
                    List<Package> potentialPackages = Packages[packageId];
                    var filteredPotentialPackages = new List<Package>();
                    foreach (var potentialPackage in potentialPackages)
                    {
                        if (!Utils.FilterPackage(semVer, potentialPackage.Version, filterComponent))
                            filteredPotentialPackages.Add(potentialPackage);
                    }
                    foreach (var potentialPackage in filteredPotentialPackages)
                    {
                        if (Utils.ValidPackage(semVer, potentialPackage.Version, filterComponent, vOp))
                            return potentialPackage;
                    }
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
                    if (!Path.IsPathRooted(fileTool.Path))
                        throw new InvalidDataException(string.Format("Tools without linked packages need rooted paths: {0}", fileTool.Path));
                }

                switch (fileTool.Type)
                {
                    case "exe":
                        tools.Add(new ExeTool(toolset, fileTool.Aliases, toolInstallPath, fileTool.Blocking, InitEnvVariables(fileTool.EnvVariables)));
                        break;
                    case "bash":
                        tools.Add(new BashTool(toolset, fileTool.Aliases, toolInstallPath, InitEnvVariables(fileTool.EnvVariables)));
                        break;
                    default:
                        throw new InvalidDataException(string.Format("Tool type {0} not supported", fileTool.Type));
                }

                AddConfigMapping("tool-" + fileTool.Aliases[0], toolInstallPath);
            }

            return tools;
        }

        private EnvVariable[] InitEnvVariables(EnvVariable[] envVariables)
        {
            EnvVariable[] newEnvVariables = null;
            if (envVariables != null)
            {
                newEnvVariables = new EnvVariable[envVariables.Length];
                envVariables.CopyTo(newEnvVariables, 0);
            }
            return newEnvVariables;
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
                                SetConfigValue(ref command.EnvVariables[i].Value);
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
                        SetConfigValue(ref tool.EnvVariables[i].Value);
                    }
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
                str = str.Replace(m.ToString(), ConfigMapping[m.ToString()]);
            }
        }
    }
}
