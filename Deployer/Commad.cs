using CSLauncher.LauncherLib;

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

        public void Run()
        {
            /*
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Path.Combine(Deployment.BinPath, file);
            startInfo.Arguments = arguments;

            foreach (EnvVariable envVariable in envVariables)
            {
                string val = GetFinalValue(envVariable.Value);
                Utils.AddOrSetEnvVariable(startInfo.EnvironmentVariables, envVariable.Key, val);
            }

            Process p = Process.Start(startInfo);
            p.WaitForExit();
            */
        }
    }
}