using System.Collections.Generic;
using CSLauncher.LauncherLib;
using System.IO;
using System;

namespace CSLauncher.Deployer
{
    abstract class Tool
    {
        internal Tool(string toolset, string[] aliases)
        {
            Toolset = toolset;
            Aliases = aliases;
        }
        internal string Toolset { get; }
        internal string[] Aliases { get; }

        internal virtual EnvVariable[] EnvVariables { get; }

        internal virtual bool PreInstall(Deployment deployment)
        {
            foreach (var alias in Aliases)
            {
                string configPath = Path.Combine(deployment.BinPath, alias) + ".cfg";
                if (!File.Exists(configPath) || File.GetLastWriteTimeUtc(configPath) != deployment.TimeStamp)
                    return true;
            }
            return false;
        }

        internal virtual void Install(Deployment deployment) { }
    }

  
    internal class ExeTool : Tool
    {
        internal ExeTool(string toolset, string[] aliases, string toolPath, bool blocking, EnvVariable[] envVariables)
            : base(toolset, aliases)
        {
            ToolPath = toolPath;
            Blocking = blocking;
            EnvVariables = envVariables;
        }

        string ToolPath { get; }
        bool Blocking { get; }
        internal override EnvVariable[] EnvVariables { get; }

        internal override void Install(Deployment deployment)
        {
            foreach (var alias in Aliases)
            {
                Utils.Log("Processing exe alias {0}", alias);
                string aliasPath = Path.Combine(deployment.BinPath, alias);
                string aliasExePath = aliasPath + ".exe";

                Utils.Log("--Copying launcher to {0}", aliasExePath);
                Utils.CopyFile(deployment.LauncherPath, aliasExePath, true);
                string configPath = aliasPath + ".cfg";

                Utils.Log("--Serializing config file to {0}", configPath);
                LauncherConfig launcherConfig = new LauncherConfig();
                launcherConfig.ExePath = ToolPath;
                launcherConfig.Blocking = Blocking;
                launcherConfig.EnvVariables = EnvVariables;

                AppInfoSerializer.Write(configPath, launcherConfig);
                File.SetLastWriteTimeUtc(configPath, deployment.TimeStamp);
            }
        }
    }

    internal class BashTool : Tool
    {
        internal BashTool(string toolset, string[] aliases, string toolPath, EnvVariable[] envVariables)
            : base(toolset, aliases)
        {
            ToolPath = toolPath;
            if (envVariables == null)
            {
                envVariables = new EnvVariable[] { };
            }
            EnvVariables = envVariables;           
        }

        string ToolPath { get; }
        internal override EnvVariable[] EnvVariables { get; }

        internal override void Install(Deployment deployment)
        {
            foreach (var alias in Aliases)
            {
                Utils.Log("Processing bash alias {0}", alias);
                string aliasPath = Path.Combine(deployment.BinPath, alias);

                Utils.Log("--Creating launcher script {0}", aliasPath);

                using (StreamWriter file = new StreamWriter(aliasPath))
                {
                    // TODO Use a templating technique instead
                    file.WriteLine("#! /bin/bash");
                    file.WriteLine();

                    foreach (EnvVariable env in EnvVariables)
                    {
                       file.WriteLine("export {0}={1}", env.Key, env.Value);
                    }
                    file.WriteLine();

                    file.WriteLine("# Get full directory name of the script no matter where it is being called from.");
                    file.WriteLine("CURRENT_DIR=$(cd $(dirname ${BASH_SOURCE[0]-$0} ) ; pwd )");
                    file.WriteLine();

                    string nixPath = '/' + ToolPath.Replace(":", string.Empty).Replace('\\', '/');
                    file.WriteLine("# Call the actual tool");
                    file.WriteLine("source {0}", nixPath);
                }

                string dummyConfigPath = aliasPath + ".cfg";
                using (StreamWriter file = new StreamWriter(dummyConfigPath))
                {
                    file.WriteLine("dummy config file");
                }
                File.SetLastWriteTimeUtc(dummyConfigPath, deployment.TimeStamp);
            }
        }
    }
}