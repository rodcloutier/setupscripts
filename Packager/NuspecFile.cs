using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace CSLauncher.Packager
{

    /*
            <?xml version="1.0"?>
            <package >
                <metadata>
                    <id>AzCopy</id>
                    <version>1.0.0-tag</version>
                    <authors>jelmansouri</authors>
                    <owners>jelmansouri</owners>
                    <licenseUrl>http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE</licenseUrl>
                    <projectUrl>http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE</projectUrl>
                    <iconUrl>http://ICON_URL_HERE_OR_DELETE_THIS_LINE</iconUrl>
                    <requireLicenseAcceptance>false</requireLicenseAcceptance>
                    <description>Package description</description>
                    <releaseNotes>Summary of changes made in this release of the package.</releaseNotes>
                    <copyright>Copyright 2017</copyright>
                    <tags>Tag1 Tag2</tags>
                    <dependencies>
                    </dependencies>
                    <packageTypes>
                            <packageType name="DeployerPackage" />
                    </packageTypes>
                </metadata>
                <files>
                    <!-- Add files from an arbitrary folder that's not necessarily in the project -->
                    <file src=".\tools\**" target="tools" />
                </files>
            </package>
        */

    [XmlRoot("package")]
    public class Package
    {
        public Package() { FileEntries = new List<FileEntry>(); }
        public Package(string id, string version)
        {
            Metadata = new PkgMetadata(id, version);
            FileEntries = new List<FileEntry>();
            FileEntries.Add(new FileEntry(@".\tools\**", "tools"));
        }

        [XmlElement("metadata")]
        public PkgMetadata Metadata { get; set; }

        [XmlArray("files")]
        [XmlArrayItem("file")]
        public List<FileEntry> FileEntries { get; }

        public class PkgMetadata
        {
            public PkgMetadata()
            {
                Dependencies = new List<string>();
                PackageTypes = new List<PackageType>();
            }
            public PkgMetadata(string id, string version)
            {
                Id = id;
                Version = version;
                Authors = Environment.UserName;
                Owners = Environment.UserName;
                LicenseUrl = "https://github.com/jelmansouri/setupscripts/blob/master/cslauncher/README.md";
                ProjectUrl = "https://github.com/jelmansouri/setupscripts/blob/master/cslauncher/README.md";
                //IconUrl = "http://blog.tech-fellow.net/content/images/2015/08/nuget.png";
                RequireLicenseAcceptance = false;
                Description = "Deployer package created using Deployer.Packager";
                ReleaseNotes = "This is a package of an already existing tool";
                Copyright = "";
                Tags = "Deployer " + id;
                Dependencies = new List<string>();
                PackageTypes = new List<PackageType>();
                PackageTypes.Add(new PackageType("DployerPackage"));
            }
            [XmlElement("id")]
            public string Id { get; set; }

            [XmlElement("version")]
            public string Version { get; set; }

            [XmlElement("authors")]
            public string Authors { get; set; }

            [XmlElement("owners")]
            public string Owners { get; set; }

            [XmlElement("licenseUrl")]
            public string LicenseUrl { get; set; }

            [XmlElement("projectUrl")]
            public string ProjectUrl { get; set; }

            [XmlElement("iconUrl")]
            public string IconUrl { get; set; }

            [XmlElement("requireLicenseAcceptance")]
            public bool RequireLicenseAcceptance { get; set; }

            [XmlElement("description")]
            public string Description { get; set; }

            [XmlElement("releaseNotes")]
            public string ReleaseNotes { get; set; }

            [XmlElement("copyright")]
            public string Copyright { get; set; }

            [XmlElement("tags")]
            public string Tags { get; set; }

            [XmlArray("dependencies")]
            [XmlArrayItem("dependency")]
            public List<string> Dependencies { get; }

            [XmlArray("packageTypes")]
            [XmlArrayItem("packageType")]
            public List<PackageType> PackageTypes { get; }

            public class PackageType
            {
                public PackageType()
                {

                }
                public PackageType(string name)
                {
                    Name = name;
                }

                [XmlAttribute("name")]
                public string Name { get; set; }
            }
        }

        public class FileEntry
        {
            public FileEntry()
            {

            }

            public FileEntry(string source, string target)
            {
                Source = source;
                Target = target;
            }

            [XmlAttribute("src")]
            public string Source { get; set; }

            [XmlAttribute("target")]
            public string Target { get; set; }
        }

        public void Serialize(string location)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Package));
            using (TextWriter writer = new StreamWriter(location))
            {
                serializer.Serialize(writer, this);
            }
        }
    }
}