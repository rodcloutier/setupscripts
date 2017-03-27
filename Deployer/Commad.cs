using CSLauncher.LauncherLib;
using System;
using System.Diagnostics;
using System.IO;

namespace CSLauncher.Deployer
{
    internal class Command
    {
        internal Command(string filePath, string arguments, EnvVariable[] envVariables)
        {
            FilePath = filePath;
            Arguments = arguments;
            EnvVariables = envVariables;
        }

        internal string FilePath { get; }
        internal string Arguments { get; }
        internal EnvVariable[] EnvVariables { get; }

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