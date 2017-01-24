using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CSLauncher.Deployer
{
    class Program
    {

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                bool verbose = Array.Exists(args, element => element.Equals("-v")) || 
                    Array.Exists(args, element => element.Equals("--verbose"));
                bool dryRun = Array.Exists(args, element => element.Equals("-d")) ||
                    Array.Exists(args, element => element.Equals("--dryrun"));
                bool clean = Array.Exists(args, element => element.Equals("-c")) ||
                    Array.Exists(args, element => element.Equals("--clean")); ;


                Deployment deployment = DeploymentSerializer.Read("deployment.json");

                var deployer = new Deployer(deployment, verbose);
                deployer.Prepare();

                if (!dryRun)
                {
                    deployer.ProcessCopies(clean);
                    deployer.ProcessAliases(clean);
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Fatal error -> " + e.Message);
                return -1;
            }
        }
    }
}
