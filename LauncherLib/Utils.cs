using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

namespace CSLauncher.LauncherLib
{
    public class Utils
    {
        public static void AddOrSetEnvVariable(StringDictionary envVariables, string key, string value)
        {
            var expandedValue = Environment.ExpandEnvironmentVariables(value);
            if (envVariables.ContainsKey(key))
                envVariables[key] = expandedValue;
            else
                envVariables.Add(key, expandedValue);
        }
    }
}
