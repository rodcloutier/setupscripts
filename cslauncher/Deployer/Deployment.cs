using System;
using System.IO;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using CSLauncher.LauncherLib;

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
    public class GitSource
    {
        [DataMember(Name = "url")]
        public string Url;

        [DataMember(Name = "commit")]
        public string Commit;

        public GitSource()
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            Commit = "master";
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }
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

        [DataMember(Name = "git")]
        public GitSource GitSource;            

        [DataMember(Name = "tools")]
        public Tool[] Tools;
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
        public ToolSet[] ToolSets;
    }

    public class DeploymentSerializer
    {
        private static string RootPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                string currentExecutablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return Path.Combine(currentExecutablePath, path);
            }
            return path;
        }

        public static Deployment Read(string path, string optionBinPath, string optionInstallPath)
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

            if (optionBinPath != null )
            {
                dep.BinPath = optionBinPath;
            }

            if ( dep.BinPath == null )
            {
                dep.BinPath = Path.Combine("%USERPROFILE%", "bin");
            }

            if (optionInstallPath != null )
            {
                dep.InstallPath = optionInstallPath;
            }
    
            if (dep.InstallPath == null )
            {
                dep.InstallPath = Path.Combine(dep.BinPath, "packages");
            }

            dep.LauncherPath = Environment.ExpandEnvironmentVariables(dep.LauncherPath);
            dep.LauncherLibPath = Environment.ExpandEnvironmentVariables(dep.LauncherLibPath);
            dep.BinPath = Environment.ExpandEnvironmentVariables(dep.BinPath);
            dep.InstallPath = Environment.ExpandEnvironmentVariables(dep.InstallPath);

            return dep;
        }
    }
}
