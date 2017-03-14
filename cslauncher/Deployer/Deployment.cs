using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using CSLauncher.LauncherLib;

namespace CSLauncher.Deployer
{
    [DataContract]
    public class Tool
    {
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
    }

    [DataContract]
    public class Deployment
    {
        [DataMember(Name = "binPath")]
        public string BinPath;

        [DataMember(Name = "installPath")]
        public string InstallPath;

        [DataMember(Name = "launcherPath")]
        public string LauncherPath;

        [DataMember(Name = "launcherLibPath")]
        public string LauncherLibPath;

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

        public static Deployment Read(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open);
            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Deployment));
            object objDeployment = jsonSerializer.ReadObject(fileStream);
            fileStream.Close();
            Deployment dep = objDeployment as Deployment;

            dep.LauncherPath = RootPath(dep.LauncherPath);
            dep.LauncherLibPath = RootPath(dep.LauncherLibPath);

            return dep;
        }
    }
}
