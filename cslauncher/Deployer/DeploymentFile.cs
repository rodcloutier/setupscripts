using CSLauncher.LauncherLib;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using YamlDotNet.Serialization;

namespace CSLauncher.Deployer
{
    [DataContract]
    public class DeploymentFile
    {
        public string FileName { get; set; } // for error handling

        [DataMember(Name = "binPath")]
        public string BinPath { get; set; }
        [DataMember(Name = "installPath")]
        public string InstallPath { get; set; }
        [DataMember(Name = "httpProxy")]
        public string HttpProxy { get; set; }
        [DataMember(Name = "repositories")]
        public Repository[] Repositories { get; set; }
        [DataMember(Name = "packages")]
        public Package[] Packages { get; set; }
        [DataMember(Name = "toolsets")]
        public Toolset[] Toolsets { get; set; }

        [DataContract]
        public class Toolset
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }
            [DataMember(Name = "packageSpec")]
            public string PackageSpec { get; set; }
            [DataMember(Name = "tools")]
            public Tool[] Tools { get; set; }
        }

        [DataContract]
        public class Repository
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }
            [DataMember(Name = "type")]
            public string Type { get; set; }
            [DataMember(Name = "source")]
            public string Source { get; set; }
        }

        [DataContract]
        public class Package
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }
            [DataMember(Name = "version")]
            public string Version { get; set; }
            [DataMember(Name = "sourceId")]
            public string SourceID { get; set; }
        }

        [DataContract]
        public class Tool
        {
            [DataMember(Name = "type")]
            public string Type { get; set; }
            [DataMember(Name = "path")]
            public string Path { get; set; }
            [DataMember(Name = "blocking")]
            public bool Blocking { get; set; }
            [DataMember(Name = "envVariables")]
            public EnvVariable[] EnvVariables { get; set; }
            [DataMember(Name = "commands")]
            public Command[] Commands { get; set; }
            [DataMember(Name = "aliases")]
            public string[] Aliases { get; set; }

            [OnDeserializing]
            private void OnDeserializing(StreamingContext context)
            {
                Type = "exe";
                Blocking = true;
            }
        }

        [DataContract]
        public class Command
        {
            [DataMember(Name = "filePath")]
            public string FilePath { get; set; }
            [DataMember(Name = "args")]
            public string Arguments { get; set; }
            [DataMember(Name = "envVariables")]
            public EnvVariable[] EnvVariables { get; set; }
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

        public static DeploymentFile Read(string path, bool outputProcessedJson)
        {
            DeploymentFile deploymentFile = null;
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    // Convert to JSON
                    var serializerBuilder = new SerializerBuilder();
                    serializerBuilder.JsonCompatible();
                    serializerBuilder.DisableAliases();
                    var serializer = serializerBuilder.Build();
                    var deserializer = new Deserializer();
                    var writer = new StreamWriter(stream);

                    serializer.Serialize(writer, deserializer.Deserialize(new StreamReader(fileStream)));
                    writer.Flush();

                    stream.Seek(0, SeekOrigin.Begin);
                    if (outputProcessedJson)
                    {
                        using (var file = new FileStream(Path.GetFileNameWithoutExtension(path) + "_processed.json", FileMode.OpenOrCreate))
                        {
                            stream.CopyTo(file);
                        }
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    // Deserialize with the contract
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(DeploymentFile));
                    deploymentFile = jsonSerializer.ReadObject(stream) as DeploymentFile;
                }
            }
            deploymentFile.FileName = path;
            return deploymentFile;
        }
    }
}
