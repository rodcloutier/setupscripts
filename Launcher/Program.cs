using System;
using System.Diagnostics;
using System.IO;
using CSLauncher.LauncherLib;

namespace CSLauncher.Launcher
{
    class Program
    {
        // for dev facility if you ever want to debug uncomment line {1} and {2}
        // {1}
        //private static string _JsonString = @"{""exePath"":""C:\\DevTools\\cloc-1.72.exe"", ""noWait"":""false"", ""envVariables"":[]}";

        [STAThread]
        static int Main(string[] args)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)) + ".cfg";
            // {2}
            //File.WriteAllText(path, _JsonString);
            LauncherConfig launcherConfig = AppInfoSerializer.Read(path);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = launcherConfig.ExePath;

            string cmd = Environment.CommandLine;
            // Remove exe part from raw command line
            int firstArgIndex = 0;
            bool discardNextSpaces = false;
            foreach (char c in cmd)
            {
                if (c == '\"')
                {
                    discardNextSpaces = !discardNextSpaces;
                }

                if (!discardNextSpaces && (c == ' ' || c == '\t'))
                    break;

                firstArgIndex++;
            }
            string arguments = cmd.Substring(firstArgIndex, cmd.Length - firstArgIndex).Trim();
            startInfo.Arguments = arguments;

            foreach(EnvVariable envVariable in launcherConfig.EnvVariables)
            {
                if (startInfo.EnvironmentVariables.ContainsKey(envVariable.Key))
                    startInfo.EnvironmentVariables[envVariable.Key] = envVariable.Value;
                else
                    startInfo.EnvironmentVariables.Add(envVariable.Key, envVariable.Value);
            }

            Process p = Process.Start(startInfo);
            if (!launcherConfig.NoWait)
                p.WaitForExit();

            return 0;
        }
    }
}