using System;
using System.Configuration;
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
            string downloadLocation  = Path.Combine(ConfigurationManager.AppSettings.Get("location"), "Deployer");
            if (!Directory.Exists(downloadLocation))
            {
                Directory.CreateDirectory(downloadLocation);
            }
            string zipFile = Path.Combine(downloadLocation, "Deployer.zip");
            using (var client = new WebClient())
            {
                client.DownloadFile(url, zipFile);
            }

            ZipFile.ExtractToDirectory(zipFile, downloadLocation);
            File.Delete(zipFile);

            return 0;
        }
    }
}