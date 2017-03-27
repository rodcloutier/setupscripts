using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CSLauncher.Packager
{
    class Options
    {
        [Option("version", HelpText = "Shows the version")]
        public bool Version { get; set; }

        [ValueList(typeof(List<string>))]
        public IList<string> InputFiles { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("<<app title>>", "<<app version>>"),
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
                var parser = new Parser(
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