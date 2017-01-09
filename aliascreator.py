import os
import json
import sys
import getopt

def usage(): 
    # script usage 
    print(
        "aliascreator.py [-h|--help] [-i|--input filename]"
        "\n This script creates a set of aliases for commands to be called on windows from bash & cmd & ps"
        "\n the scripts takes a json file and all paths specified within it are relative to its location"
        "\n\t -h|--help    display usage"
        "\n\t -i|--input   explicitly set an input file"
        "\n\t -v|--verbose verbose mode"
        )

def write_on_position(filecontent, pos, value):
    for c in bytes(value, "utf-8"):
        filecontent[pos] = c
        pos += 1
    filecontent[pos] = 0

def main(argv):
    # my code here
    try:
        opts, args = getopt.getopt(argv, "hvi:", ["verbose", "help", "input="])
    except getopt.GetoptError as err:
        # print help information and exit:
        print(str(err))  # will print something like "option -a not recognized"
        usage()
        sys.exit()

    inputfile = "aliascreator.json"
    verbose = False
    for o, a in opts:
        if o in ("-h", "--help"):
            usage()
            sys.exit()
        elif o in ("-i", "--input"):
            inputfile = a
        elif o in ("-v", "--verbose"):
            verbose = True
        else:
            assert False, "unhandled option"

    if not os.path.exists(inputfile) or not os.path.isfile(inputfile):
        print(inputfile + " could not be found")
        sys.exit()

    inputfile = os.path.abspath(inputfile)
    if verbose: print("File found: {}".format(inputfile))

    workingdir = os.path.dirname(inputfile)
    os.chdir(workingdir)
    if verbose: print("Changing Working directory to: {}".format(workingdir))

    try:
        with open(inputfile, "r") as f:
            jsoninput = json.load(f)
    except json.JSONDecodeError as err:
        print(str(err))
        sys.exit()

    try:
        targetpath = jsoninput["targetPath"]
        launcher = jsoninput["launcher"]
        toolsets = jsoninput["toolsets"]
    except KeyError as err:
        print("Missing mandatory keys in the input file : {}".format(str(err)))
        sys.exit()

    if not os.path.exists(targetpath) or not os.path.isdir(targetpath):
        print("Invalid target path : {}".format(targetpath))
        sys.exit()

    if verbose: print("Target path: {}".format(targetpath))

    if not os.path.exists(launcher) or not os.path.isfile(launcher):
        print("Launcher not found : {}".format(targetpath))
        sys.exit()

    if verbose: print("Using launcher: {}".format(launcher)) 

    try:
        with open("launcher.exe", "rb") as f:
            launchercontent = bytearray(f.read())
            #bytes("C:\\DevTools\\cpython-3.5.2.amd64\\python.exe", "utf-8")
            #bytes("", "utf-8")
            appnamepos = launchercontent.find(b"FILLAPPNAME")
            optionspos = launchercontent.find(b"FILLOPTIONS")
            envvarspos = launchercontent.find(b"FILLENVVARS")
            if (appnamepos is -1) or (optionspos is -1) or (envvarspos is -1):
                raise Exception("Wrong launcher format")
    except Exception as err:
        print(str(err))
        sys.exit()

    for toolset in toolsets:
        toolsetname = toolset["name"] if "name" in toolset else "unknown"
        if verbose: print("Processing toolset: {}".format(toolsetname))
        tools = toolset["tools"] if "tools" in toolset else []
        for tool in tools:
            try:
                sourcepath = tool["sourcePath"]
                targetnames = tool["targetName"]
                blocking = tool["blocking"] if "blocking" in tool else True

                if not os.path.exists(sourcepath) or not os.path.isfile(sourcepath):
                    raise Exception("Invalid source path {}".format(sourcepath))

                sourcepath = os.path.abspath(sourcepath)
                if verbose: print("  Processing {} with aliases".format(sourcepath))

                options = "nonblocking,nostdredirect,"  if not blocking else ""
                if verbose and len(options) is not 0: print("  Setting options: {}".format(options))

                envvars = ""
                if "additionalEnvVariables" in tool:
                    if verbose: print("  Setting environment variables: {}".format(tool["additionalEnvVariables"]))
                    for key, val in tool["additionalEnvVariables"].items():
                        envvars += "{}={},".format(key, val)

                launchercontentcopy = launchercontent.copy()
                write_on_position(launchercontentcopy, appnamepos, sourcepath)
                write_on_position(launchercontentcopy, optionspos, options)
                write_on_position(launchercontentcopy, envvarspos, envvars)

                for target in targetnames:
                    if verbose: print("   Creating alias: {}".format(target))
                    aliasfilename = os.path.join(targetpath, target + ".exe")
                    with open(aliasfilename, "wb") as f:
                        f.write(launchercontentcopy)

            except KeyError as err:
                print("Missing necessary key for tool spec from: {}".format(str(err)))
            except Exception as err:
                print("Error: {}".format(str(err)))

if __name__ == "__main__":
    main(sys.argv[1:])
