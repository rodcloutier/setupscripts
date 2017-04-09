using CommandLine;
using CommandLine.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace CSLauncher.Packager
{
    class Options
    {
        [Option("version", HelpText = "Shows the version")]
        public bool Version { get; set; }

        [Option('f', "package-format", HelpText = "Output package format, can be zip or nuget")]
        public string PackageFormat { get; set; }

        [Option('n', "package-name", HelpText = "Input package name")]
        public string PackageName { get; set; }

        [Option('v', "package-version", HelpText = "Input package version")]
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

                if (string.IsNullOrEmpty(options.Input) || (!File.Exists(options.Input) && !Directory.Exists(options.Input)))
                {
                    Console.WriteLine(options.GetUsage());
                    return 1;
                }

                if (string.IsNullOrEmpty(options.PackageFormat))
                    options.PackageFormat = "nuget";
                if (string.IsNullOrEmpty(options.PackageName))
                    options.PackageName = Path.GetFileName(options.Input);
                if (string.IsNullOrEmpty(options.PackageVersion))
                    options.PackageVersion = "1.0.0";

                if (options.PackageFormat != "nuget" && options.PackageFormat != "zip")
                {
                    Console.WriteLine(options.GetUsage());
                    return 1;
                }

                string outputDir = options.PackageName + "." + options.PackageVersion;
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
                Directory.CreateDirectory(outputDir);

                string outputToolDir = outputDir;
                if (options.PackageFormat == "nuget")
                {
                    outputToolDir = Path.Combine(outputDir, "tools");
                    Directory.CreateDirectory(outputToolDir);
                }

                if (Directory.Exists(options.Input))
                {
                    string inputDir = SimplifyDirectory(options.Input);
                    DirectoryCopy(inputDir, outputToolDir, true);
                }
                else if (Path.GetExtension(options.Input) == ".zip")
                {
                    string zipFileName = Path.GetFileName(options.Input);
                    string uncompressedLocation = Path.GetFileNameWithoutExtension(zipFileName);

                    ZipFile.ExtractToDirectory(zipFileName, uncompressedLocation);
                    string inputDir = SimplifyDirectory(uncompressedLocation);
                    DirectoryCopy(inputDir, outputToolDir, true);
                    Directory.Delete(uncompressedLocation, true);
                }
                else if (Path.GetExtension(options.Input) == ".exe")
                {
                    string exeFileName = Path.GetFileName(options.Input);
                    File.Copy(options.Input, Path.Combine(outputToolDir, exeFileName), true);
                }

                switch (options.PackageFormat)
                {
                    case "nuget":
                        Package pkg = new Package(options.PackageName, options.PackageVersion);
                        string nuspecFilePath = Path.Combine(outputDir, options.PackageName + ".nuspec");
                        pkg.Serialize(nuspecFilePath);
                        Console.WriteLine("You need to run now:");
                        Console.WriteLine("> nuget pack " + nuspecFilePath);
                        Console.WriteLine("> nuget push " + outputDir + ".nupkg - source \"nugetServerUrl\\path\"");
                        break;
                    case "zip":
                        ZipFile.CreateFromDirectory(outputDir, outputDir + ".zip");
                        Directory.Delete(outputDir, true);
                        Console.WriteLine("You need to run now:");
                        Console.WriteLine("> curl -u<USERNAME>:<PASSWORD> -T filePath \"serverUrl\\path\"");
                        break;
                    default:
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
        private static string SimplifyDirectory(string inputDir)
        {
            bool simplify = false;
            do
            {
                string[] sublEments = Directory.GetFileSystemEntries(inputDir);
                if (sublEments.Length == 1 && Directory.Exists(sublEments[0]))
                {
                    inputDir = sublEments[0];
                    simplify = true;
                    continue;
                }
                simplify = false;
            } while (simplify);

            return inputDir;
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}