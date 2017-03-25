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
        [Option("version", HelpText = "Shows the version")]
        public bool Version { get; set; }

        [Option('d', "dryrun", MutuallyExclusiveSet = "zero", HelpText = "Parses and prepare the deployment but does not run the Package and Aliases steps")]
        public bool DryRun { get; set; }

        [Option('c', "clean", MutuallyExclusiveSet = "zero", HelpText = "Delete the deployment directories")]
        public bool Clean { get; set; }

        [Option("toolset")]
        public string ToolSet { get; set; }

        [Option("binpath", HelpText = "Path to use to override 'binPath' in the config file")]
        public string BinPath { get; set; }

        [Option("installpath", HelpText = "Path to use to override 'installPath' in the config file")]
        public string InstallPath { get; set; }

        [Option("get-binPath", HelpText= "returns the config 'binPath' value without doing any install")]
        public bool GetBinPath { get; set;  }

        [Option("outputprocessedjson", HelpText = "returns the processed json file")]
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
                Heading  = new HeadingInfo("<<app title>>", "<<app version>>"),
                Copyright = new CopyrightInfo("<<app author>>", 2017),
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
                Utils.Quiet = options.Quiet || options.GetBinPath;
                var deployer = new Deployer(options.DeploymentFilePaths, options.BinPath, options.InstallPath, options.OutputProcessJson);
                if (options.GetBinPath)
                {
                    Console.WriteLine(deployer.Deployment.BinPath);
                    return 0;
                }

                if (options.Clean)
                {
                    deployer.Clean();
                }
                else
                {
                    if (!options.DryRun)
                    {
                        deployer.ProcessPackages();
                        deployer.ProcessTools();
                        deployer.ProcessCommands();
                        deployer.CleanUnused();
                    }
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
