using System;
using System.Diagnostics;


namespace CSLauncher.Deployer
{
    internal class Git
    {
        static internal void run(params string[] args)
        {
            var cmd = string.Join(" ", args);

            var info = new ProcessStartInfo("git", cmd);
            info.UseShellExecute = false;
            var proc = Process.Start(info);
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception(String.Format("Failed to execute git {0} command\n", cmd));
            }
        }
    }
}
