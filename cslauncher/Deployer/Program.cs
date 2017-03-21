using System;
using System.Diagnostics;
using System.IO;

namespace CSLauncher.Deployer
{
    class Program
    {
        internal static bool HasOption(string[] args, string shortFormat, string longFormat)
        {
            return Array.Exists(args, element => element.ToLower().Equals(shortFormat)) ||
                    Array.Exists(args, element => element.ToLower().Equals(longFormat));
        }

        internal static string GetOptionValue(string[] args, string format, string defaultValue=null)
        {
            string arg = Array.Find(args, element => element.ToLower().StartsWith(format));
            if (arg == null)
                return defaultValue;
            return arg.Remove(0, format.Length);
        }

        internal static void PrintUsage()
        {
            Console.WriteLine("Deployer expect to read in deployment file that defines");
            Console.WriteLine("the files to deploy/install");
            Console.WriteLine("Usage: Deployer.exe [options]");
            Console.WriteLine("where:");
            Console.WriteLine("\t-h,--help\tPrint this help");
            Console.WriteLine("\t--version\tPrint the version.");
            Console.WriteLine("\t-v,--verbose\tIncreases log verbosity");
            Console.WriteLine("\t-d,--dryrun\tParses and prepare the deployment but does not run the Package and Aliases steps");
            Console.WriteLine("\t-c,--clean\tDelete the deployment directory before doing the install");
            Console.WriteLine("\t--toolset\t");
            Console.WriteLine("\t--config\tThe deployment configuration file. Default: deployment.json");
        }

        internal static void PrintVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Console.WriteLine(fvi.FileVersion);
        }

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                bool help = HasOption(args, "-h", "--help");
                bool version = HasOption(args, "--version", "--version");
                bool verbose = HasOption(args, "-v", "--verbose");
                bool dryRun = HasOption(args, "-d", "--dryrun");
                bool clean = HasOption(args, "-c", "--clean");
                string toolset = GetOptionValue(args, "--toolset=");
                string deploymentFile = GetOptionValue(args, "--config=", "deployment.json");

                if (help)
                {
                    PrintUsage();
                    return 0;
                }

                if (version)
                {
                    PrintVersion();
                    return 0;
                }

                if (!File.Exists(deploymentFile) )
                {
                    throw new Exception("Expected file deployment.json not found in current directory");
                }

                Deployment deployment = DeploymentSerializer.Read(deploymentFile);

                var deployer = new Deployer(deployment, verbose);
                deployer.Prepare(toolset);

                if (!dryRun)
                {
                    deployer.ProcessPackages(clean);
                    deployer.ProcessAliases(clean);
                    deployer.ProcessCommands();
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
