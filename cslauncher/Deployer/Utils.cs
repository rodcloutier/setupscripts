using NuGet;
using System;
using System.Collections.Generic;
using System.IO;

namespace CSLauncher.Deployer
{
    internal static class Utils
    {
        internal enum VersionOp
        {
            Equal,
            NotEqual,
            Lower,
            LowerEqual,
            Greater,
            GreaterEqual
        }

        internal enum VersionComponent
        {
            None,
            Major,
            Minor,
            Build,
            Revision
        }

        internal static bool Quiet { get; set; }

        static Utils()
        {
            Quiet = false;
        }

        internal static string OverrideString(string currentPath, string newPath)
        {
            return string.IsNullOrEmpty(newPath) ? currentPath : newPath;
        }

        internal static string RootPathToExePath(string path)
        {
            if (Path.IsPathRooted(path))
                throw new ArgumentException(string.Format("Path: {0} is already rooted", path));

            string currentExecutablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(currentExecutablePath, path);
        }

        internal static string NormalizePath(string path)
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        }

        internal static void Log(string format, params object[] arg)
        {
            if (!Quiet)
                Console.WriteLine(format, arg);
        }

        internal static void CopyFile(string source, string target, bool overwrite = false)
        {
            if (overwrite || !File.Exists(target))
            {
                var fileInfo = new FileInfo(source);
                fileInfo.CopyTo(target, overwrite);
            }
        }

        internal static void CopyFileIfNewer(string source, string target)
        {
            CopyFile(source, target, !File.Exists(target) || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(target));
        }

        internal static SemanticVersion ParseVersion(string version)
        {
            try
            {
                return SemanticVersion.Parse(version);
            }
            catch (ArgumentException e)
            {
                throw new InvalidDataException(string.Format("Version info count not be processed for {0}: {1}", version, e.Message));
            }
        }
        

        private static Dictionary<string, VersionOp> OpsMapping = new Dictionary<string, VersionOp>() {
            { "==", VersionOp.Equal},
            { "!=", VersionOp.NotEqual},
            { "<", VersionOp.Lower},
            { "<=", VersionOp.LowerEqual},
            { ">", VersionOp.Greater},
            { ">=", VersionOp.GreaterEqual},
        };

        private static Func<Version, Version, VersionComponent, bool>[] comparisonFunctions = new Func<Version, Version, VersionComponent, bool>[] {
            (v1, v2, component) => { return GetNextComponentValue(v1, component) == GetNextComponentValue(v2, component); },
            (v1, v2, component) => { return GetNextComponentValue(v1, component) != GetNextComponentValue(v2, component); },
            (v1, v2, component) => { return GetNextComponentValue(v1, component) < GetNextComponentValue(v2, component); },
            (v1, v2, component) => { return GetNextComponentValue(v1, component) <= GetNextComponentValue(v2, component); },
            (v1, v2, component) => { return GetNextComponentValue(v1, component) > GetNextComponentValue(v2, component); },
            (v1, v2, component) => { return GetNextComponentValue(v1, component) >= GetNextComponentValue(v2, component); },
        };

        private static int GetNextComponentValue(Version v, VersionComponent component)
        {
            switch(component)
            {
                case VersionComponent.None:
                    return v.Major;
                case VersionComponent.Major:
                    return v.Minor;
                case VersionComponent.Minor:
                    return v.Build;
                case VersionComponent.Build:
                    return v.Revision;
                default:
                    return 0;
            }
        }

        internal static void GetPackageSpecComponents(string packageSpec, out string packageId, out VersionOp versionOp, out SemanticVersion semVer, out VersionComponent filterComponent)
        {
            string packageVersion;
            var specComponents = packageSpec.Split(' ');
            if (specComponents.Length == 1)
            {
                packageId = specComponents[0];
                versionOp = VersionOp.GreaterEqual;
                packageVersion = "0";
            }
            else if (specComponents.Length == 3)
            {
                packageId = specComponents[0];
                versionOp = OpsMapping[specComponents[1]];
                packageVersion = specComponents[2];
            }
            else
            {
                throw new InvalidDataException(string.Format("Invlaid package spec {0}, needs to be \"packageName compOperator x[.y.z.w-tag]", packageSpec));
            }
            int idx = 0;
            int filterThreshold = 0;
            while (idx < packageVersion.Length && packageVersion[idx] != '-') { if (packageVersion[idx++] == '.') filterThreshold++; }
            if (filterThreshold == 0)
            {
                packageVersion = packageVersion.Insert(idx, ".0");
            }
            semVer = ParseVersion(packageVersion);
            filterComponent = (VersionComponent)Math.Min(filterThreshold, (int)VersionComponent.Build);
        }

        internal static bool FilterPackage(SemanticVersion specVer1, SemanticVersion packageVer2, VersionComponent filterComponent)
        {
            bool filterPackage = false;
            if (!string.IsNullOrEmpty(specVer1.SpecialVersion) && specVer1.SpecialVersion != packageVer2.SpecialVersion)
            {
                filterPackage |= true;
            }
            Version specV = specVer1.Version;
            Version packageV = packageVer2.Version;
            switch (filterComponent)
            {
                case Utils.VersionComponent.Major:
                    if (specV.Major != packageV.Major)
                        filterPackage |= true;
                    break;
                case Utils.VersionComponent.Minor:
                    if (specV.Major != packageV.Major || specV.Minor != packageV.Minor)
                        filterPackage |= true;
                    break;
                case Utils.VersionComponent.Build:
                    if (specV.Major != packageV.Major || specV.Minor != packageV.Minor || specV.Build != specV.Build)
                        filterPackage |= true;
                    break;
            }
            return filterPackage;
        }

        internal static bool ValidPackage(SemanticVersion specVer1, SemanticVersion packageVer2, VersionComponent filterComponent, VersionOp versionOp)
        {
            Version specV = specVer1.Version;
            Version packageV = packageVer2.Version;
            
            return comparisonFunctions[(int)versionOp](packageV, specV, filterComponent);
        }
    }
}