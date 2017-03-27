using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace CSLauncher.Deployer
{
    class Options
    {
        [Option('v', "version", HelpText = "Shows the version")]
        public bool Version { get; set; }

        [Option('d', "dry-run", MutuallyExclusiveSet = "option-mutex-1", HelpText = "Parses and prepare the deployment but does not run the Package and Aliases steps")]
        public bool DryRun { get; set; }

        [Option('c', "clean", MutuallyExclusiveSet = "option-mutex-1", HelpText = "Delete the deployment directories")]
        public bool Clean { get; set; }

        [Option("toolset", MutuallyExclusiveSet = "option-mutex-1", HelpText = "Only install the specified toolset")]
        public string Toolset { get; set; }

        [Option("bin-path", HelpText = "Path to use to override 'binPath' in the config file")]
        public string BinPath { get; set; }

        [Option("install-path", HelpText = "Path to use to override 'installPath' in the config file")]
        public string InstallPath { get; set; }

        [Option("get-bin-Path", HelpText= "returns the config 'binPath' value without doing any install")]
        public bool GetBinPath { get; set;  }

        [Option("output-processed-json", HelpText = "returns the processed json file")]
        public bool OutputProcessJson { get; set; }

        [Option("quiet", HelpText = "Runs without output any info")]
        public bool Quiet { get; set; }

        [ValueList(typeof(List<string>))]
        public IList<string> DeploymentFilePaths { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = HeadingInfo.Default,
                Copyright = CopyrightInfo.Default,
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("Deployer expect to read in at minimum one json or yml deployment file that defines the files to deploy/install");
            help.AddPreOptionsLine("Usage: Deployer.exe [options] <FILES>");
            help.AddPreOptionsLine("  where:");

            help.AddOptions(this);
            return help;
        }
    }
    
    class Program
    {
        internal static void PrintVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Console.WriteLine(fvi.FileVersion);
        }

        [STAThread]
        static int Main(string[] args)
        {
#if !DEBUG
            try
#endif
            {
                var options = new Options();
                var parser = new CommandLine.Parser(
                    s =>
                    {
                        s.IgnoreUnknownArguments = false;
                        s.MutuallyExclusive = true;
                        s.CaseSensitive = true;
                        s.HelpWriter = Console.Error;
                    }
                );
                var isValid = parser.ParseArgumentsStrict(args, options);
                if (!isValid)
                {
                    return 1;
                }

                if (options.Version)
                {
                    PrintVersion();
                    return 0;
                }

                if (options.DeploymentFilePaths == null || options.DeploymentFilePaths.Count == 0)
                {
                    Console.WriteLine(options.GetUsage());
                }

                Utils.Quiet = options.Quiet || options.GetBinPath;
                var deployer = new Deployer(options.DeploymentFilePaths, options.BinPath, options.InstallPath, options.OutputProcessJson);
                if (options.GetBinPath)
                {
                    Console.WriteLine(deployer.Deployment.BinPath);
                    return 0;
                }

                if (!options.DryRun)
                {
                    deployer.Run(options.Clean, options.Toolset);
                }

                return 0;
            }
#if !DEBUG
            catch (Exception e)
            {
                Console.WriteLine("Fatal error -> " + e.Message);
                return -1;
            }
#endif
        }
    }
}
