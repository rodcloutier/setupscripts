using System;

namespace CSLauncher.Deployer
{
    class Program
    {
        internal static bool HasOption(string[] args, string shortFormat, string longFormat)
        {
            return Array.Exists(args, element => element.ToLower().Equals(shortFormat)) ||
                    Array.Exists(args, element => element.ToLower().Equals(longFormat));
        }

        internal static string GetOptionValue(string[] args, string format)
        {
            string arg = Array.Find(args, element => element.ToLower().StartsWith(format));
            if (arg == null)
                return arg;
            return arg.Remove(0, format.Length);
        }

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                bool verbose = HasOption(args, "-v", "--version");
                bool dryRun = HasOption(args, "-d", "--dryrun");
                bool clean = HasOption(args, "-c", "--clean");
                string toolset = GetOptionValue(args, "--toolset=");

                Deployment deployment = DeploymentSerializer.Read("deployment.json");

                var deployer = new Deployer(deployment, verbose);
                deployer.Prepare(toolset);

                if (!dryRun)
                {
                    deployer.ProcessPackages(clean);
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
