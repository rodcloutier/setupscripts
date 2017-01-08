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
        toolsets = jsoninput["toolsets"]
    except KeyError as err:
        print("Missing mandatory keys in the input file : {}".format(str(err)))
        sys.exit()

    if not os.path.exists(targetpath) or not os.path.isdir(targetpath):
        print("Invalid target path : {}".format(targetpath))
        sys.exit()

    if verbose: print("Target path: {}".format(targetpath))

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
                nixsourcepath = "/" + sourcepath.replace(":", "").replace("\\", "/")
                if verbose: print("  Processing {} {} with aliases".format("non blocking" if not blocking else "", sourcepath))

                batfilecontent = '@echo off\n{}"{}" %*'.format(
                    'start "" ' if not  blocking else "", sourcepath)
                bashfilecontent = '{}"{}" $@{}'.format(
                    "nohup " if not blocking else "",
                    nixsourcepath, " &>/dev/null &" if not blocking else "")

                for target in targetnames:
                    if verbose: print("   Creating alias: {}".format(target))
                    batfilename = os.path.join(targetpath, target + ".bat")
                    bashfilename = os.path.join(targetpath, target)
                    with open(batfilename, "w") as f:
                        f.write(batfilecontent)
                    with open(bashfilename, "w") as f:
                        f.write(bashfilecontent)

            except KeyError as err:
                print("Missing necessary key for tool spec from: {}".format(str(err)))
            except Exception as err:
                print("Error: {}".format(str(err)))

if __name__ == "__main__":
    main(sys.argv[1:])
