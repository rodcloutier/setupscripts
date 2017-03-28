using CSLauncher.LauncherLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CSLauncher.Deployer
{
    internal class Command
    {
        internal Command(string filePath, string arguments, EnvVariable[] envVariables)
        {
            FilePath = filePath;
            Arguments = arguments;
            EnvVariables = envVariables;
            string hashString = filePath + arguments;
            foreach (var envVar in EnvVariables)
            {
                if (string.IsNullOrEmpty(envVar.Key))
                    hashString += envVar.Key;
                if (string.IsNullOrEmpty(envVar.Value))
                    hashString += envVar.Value;
            }
            byte[] hashBytes = Encoding.UTF8.GetBytes(hashString);
            SHA1 sha1 = SHA1.Create();
            byte[] hashResusts = sha1.ComputeHash(hashBytes);
            StringBuilder hashStringBuilder = new StringBuilder(hashResusts.Length * 2);
            foreach (var b in hashResusts)
            {
                hashStringBuilder.Append(b.ToString("x2"));
            }
            Hash = hashStringBuilder.ToString();
        }

        internal string FilePath { get; }
        internal string Arguments { get; }
        internal EnvVariable[] EnvVariables { get; }
        internal string Hash { get; }

        public void Run(Deployment deployment)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Path.Combine(deployment.BinPath, FilePath);
            startInfo.Arguments = Arguments;

            foreach (EnvVariable envVariable in EnvVariables)
            {
                LauncherLib.Utils.AddOrSetEnvVariable(startInfo.EnvironmentVariables, envVariable.Key, envVariable.Value);
            }

            Process p = Process.Start(startInfo);
            p.WaitForExit();
        }
    }
}