using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;


namespace CSLauncher.Packager
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            string url = ConfigurationManager.AppSettings.Get("url");
            Version version = new Version(ConfigurationManager.AppSettings.Get("version"));
            string deployerLocation = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings.Get("location"));
            string deployerSentinel = Path.Combine(deployerLocation, "__deployer__");

            bool download = false;
            if (!Directory.Exists(deployerLocation))
            {
                Directory.CreateDirectory(deployerLocation);
                download = true;
            }
            else
            {
                if (File.Exists(deployerSentinel))
                {
                    string sentinelContent = File.ReadAllText(deployerSentinel);
                    Version sentinelVersion = new Version(sentinelContent);
                    if (version > sentinelVersion)
                    {
                        DirectoryInfo dir = new DirectoryInfo(deployerLocation);
                        FileInfo[] files = dir.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            file.Delete();
                        }
                        download = true;
                    }
                }
                else
                {
                    download = true;
                }
            }
            if (download)
            {
                string zipFile = Path.Combine(deployerLocation, "Deployer.zip");
                using (var client = new WebClient())
                {
                    client.DownloadFile(url, zipFile);
                }

                ZipFile.ExtractToDirectory(zipFile, deployerLocation);
                File.Delete(zipFile);
                File.WriteAllText(deployerSentinel, version.ToString());
            }

            string deployerExe = Path.Combine(deployerLocation, "Deployer.exe");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = deployerExe;
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

            Process p = Process.Start(startInfo);
            p.WaitForExit();
            return p.ExitCode;
        }
    }
}