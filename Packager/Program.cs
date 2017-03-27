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

        [Option("package-format", HelpText = "Path to use to override 'binPath' in the config file")]
        public string PackageFormat { get; set; }

        [Option("package-name", HelpText = "Path to use to override 'binPath' in the config file")]
        public string PackageName { get; set; }

        [Option("package-version", HelpText = "Path to use to override 'binPath' in the config file")]
        public string PackageVersion { get; set; }

        [ValueOption(0)]
        public string Input { get; set; }

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
            help.AddPreOptionsLine("Packager formats a directory containing a package to a Deployer compliant format");
            help.AddPreOptionsLine("Usage: Packager.exe options <INPUT>");
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

                if (!File.Exists(options.Input) && !Directory.Exists(options.Input))
                {
                    return 1;
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