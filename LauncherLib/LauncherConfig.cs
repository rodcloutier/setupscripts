using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CSLauncher.LauncherLib
{
    [DataContract]
    public struct EnvVariable
    {
        [DataMember(Name = "key")]
        public string Key;
        [DataMember(Name = "value")]
        public string Value;
    }

    [DataContract]
    public class LauncherConfig
    {
        [DataMember(Name = "exePath")]
        public string ExePath;

        [DataMember(Name = "blocking")]
        public bool Blocking;

        [DataMember(Name = "envVariables")]
        public EnvVariable[] EnvVariables;
    }

    public class AppInfoSerializer
    {
        public static LauncherConfig Read(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open);
            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(LauncherConfig));
            object objAppInfo = jsonSerializer.ReadObject(fileStream);
            fileStream.Close();
            return objAppInfo as LauncherConfig;
        }

        public static void Write(string path, LauncherConfig launcherConfig)
        {
            FileStream fileStream = new FileStream(path, FileMode.Create);
            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(LauncherConfig));
            jsonSerializer.WriteObject(fileStream, launcherConfig);
            fileStream.Close();
        }
    }
}

