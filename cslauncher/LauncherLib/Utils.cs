using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLauncher.LauncherLib
{
    public class Utils
    {
        public static void AddOrSetEnvVariable(StringDictionary envVariables, string key, string value)
        {
            if (envVariables.ContainsKey(key))
                envVariables[key] = value;
            else
                envVariables.Add(key, value);
        }
    }
}
