using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using CSLauncher.LauncherLib;
using YamlDotNet.Serialization;
using System.Collections.Generic;

namespace CSLauncher.Deployer
{
    [DataContract]
    public class Command
    {
        [DataMember(Name = "file")]
        public string FileName;

        [DataMember(Name = "arguments")]
        public string Arguments;

        [DataMember(Name = "envVariables")]
        public EnvVariable[] EnvVariables;
    }

    [DataContract]
    public class Tool
    {
        [DataMember(Name = "command")]
        public Command Command;

        [DataMember(Name = "launcherConfig")]
        public LauncherConfig LauncherConfig;

        [DataMember(Name = "aliases")]
        public string[] Aliases;

        public void Validate(HashSet<string> aliases)
        {
            foreach(var alias in Aliases)
            {
                if (aliases.Contains(alias))
                    throw new Exception(String.Format("Duplicate alias {0}", alias));

                aliases.Add(alias);
            }
        }
    }

    [DataContract]
    public class NugetSource
    {
        [DataMember(Name = "repositoryUrl")]
        public string RepositoryUrl;

        [DataMember(Name = "packageName")]
        public string PackageName;

        [DataMember(Name = "packageVersion")]
        public string PackageVersion;
    }

    [DataContract]
    public class ToolSet
    {
        [DataMember(Name = "name")]
        public string Name;

        [DataMember(Name = "url")]
        public string UrlSource;

        [DataMember(Name = "nuget")]
        public NugetSource NugetSource;

        [DataMember(Name = "tools")]
        public Tool[] Tools;

        public void Merge(ToolSet secondaryToolSet)
        {
            throw new NotImplementedException();
        }

        public void Validate(HashSet<string> toolsets, HashSet<string> aliases)
        {
            if (toolsets.Contains(Name))
                throw new Exception(String.Format("Duplicate toolset {0}", Name));

            toolsets.Add(Name);

            if (String.IsNullOrEmpty(UrlSource) && NugetSource != null)
                throw new Exception(String.Format("ToolSet {0} cannot have an Url and a Nuget source at the same time", Name));

            foreach(var tool in Tools)
            {
                tool.Validate(aliases);
            }
        }
    }

    [DataContract]
    public class Deployment
    {
        public DateTime FileDateTime;

        [DataMember(Name = "binPath")]
        public string BinPath;

        [DataMember(Name = "installPath")]
        public string InstallPath;

        [DataMember(Name = "launcherPath")]
        public string LauncherPath;

        [DataMember(Name = "launcherLibPath")]
        public string LauncherLibPath;

        [DataMember(Name = "httpProxy")]
        public string HttpProxy;

        [DataMember(Name = "toolsets")]
        public List<ToolSet> ToolSets;

        public void Merge(Deployment secondaryDeployment)
        {
            // O(n²) but we don't really care, n is quite low
            foreach (var secondaryToolSet in secondaryDeployment.ToolSets)
            {
                bool found = false;
                foreach (var mainToolSet in ToolSets)
                {
                    if (mainToolSet.Name == secondaryToolSet.Name)
                    {
                        mainToolSet.Merge(secondaryToolSet);
                        found = true;
                    }
                }
                if (!found)
                {
                    ToolSets.Add(secondaryToolSet);
                }
            }
        }

        public void Validate()
        {
            string binPath = Path.GetFullPath(BinPath);
            Console.WriteLine("Using bin path: {0}", binPath);

            string installPath = Path.GetFullPath(InstallPath);
            Console.WriteLine("Using install path: {0}", installPath);

            if (installPath.ToLower() == binPath.ToLower())
                throw new Exception("The install path and the bin path cannot point to the same directory");

            if (!File.Exists(LauncherPath))
                throw new Exception("Could not find the launcher app");

            if (!File.Exists(LauncherLibPath))
                throw new Exception("Could not find the launcher lib");

            HashSet<string> toolsetsSet = new HashSet<string>();
            HashSet<string> aliasesSet = new HashSet<string>();
            foreach (var toolSet in ToolSets)
            {
                toolSet.Validate(toolsetsSet, aliasesSet);
            }
        }

        private static string RootPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                string currentExecutablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return Path.Combine(currentExecutablePath, path);
            }
            return path;
        }

        public static Deployment Deserialize(string path, string optionBinPath, string optionInstallPath)
        {
            object objDeployment = null;

            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    // Convert to JSON
                    var serializerBuilder = new SerializerBuilder();
                    serializerBuilder.JsonCompatible();

                    var serializer = serializerBuilder.Build();
                    var deserializer = new Deserializer();
                    var writer = new StreamWriter(stream);

                    serializer.Serialize(writer, deserializer.Deserialize(new StreamReader(fileStream)));
                    writer.Flush();

                    stream.Seek(0, SeekOrigin.Begin);

                    // Deserialize with the contract
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Deployment));
                    objDeployment = jsonSerializer.ReadObject(stream);
                }
            }

            Deployment dep = objDeployment as Deployment;
            dep.FileDateTime = File.GetLastWriteTimeUtc(path);
            dep.LauncherPath = RootPath(dep.LauncherPath);
            dep.LauncherLibPath = RootPath(dep.LauncherLibPath);

            if (optionBinPath != null)
            {
                dep.BinPath = optionBinPath;
            }

            if (dep.BinPath == null)
            {
                dep.BinPath = Path.Combine("%USERPROFILE%", "bin");
            }

            if (optionInstallPath != null)
            {
                dep.InstallPath = optionInstallPath;
            }

            if (dep.InstallPath == null)
            {
                dep.InstallPath = Path.Combine(dep.BinPath, "packages");
            }

            dep.LauncherPath = Environment.ExpandEnvironmentVariables(dep.LauncherPath);
            dep.LauncherLibPath = Environment.ExpandEnvironmentVariables(dep.LauncherLibPath);
            dep.BinPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dep.BinPath));
            dep.InstallPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dep.InstallPath));

            return dep;
        }
    }
}
