
# Build setup

To build the project you need to add those two tools to you path
- [Premake](https://premake.github.io/download.html#v5)
- [Paket](https://github.com/fsprojects/Paket/releases/tag/4.0.7)

Then run:
premake vs2015 && premake paket

Note that the build has only been tested so far on vs2015.

# Deployment format

Can either be written in json or yaml format.

```json
{
    "binPath": "mandatory, Bin Path, that should be appended to your path",
    "installPath": "mandatory, Where the packages are going to be installed",
    "launcherPath": "mandatory, Launcher path, usually Launcher.exe if the launcher is in the deployer directory",
    "launcherLibPath": "mandatory, Launcher lib path, usually LauncherLib.dll if the dkk is in the deployer directory",
    "httpProxy": "optional, proxy to be used when getting packages or in environment variables",
    "toolsets": [
        {
            "name": "hello",
            "url": "file://C:/DevTools/sources/hello-1.0.0.zip",
            "tools": [
                {
                    "launcherConfig": {
                        "exePath": "hello.exe",
                        "envVariables": [
                            {
                                "key": "FOO",
                                "value": "BAR"
                            }
                        ]
                    },
                    "aliases": ["hello"]
                },
                 {
                    "launcherConfig": {
                        "exePath": "hello_bash.sh",
                        "type": "exe",
                        "envVariables": [
                            {
                                "key": "FOO",
                                "value": "BAR"
                            }
                        ]
                    },
                    "aliases": ["hello_bash"]
                },
                {
                    "launcherConfig": {
                        "exePath": "hello_generated.exe",
                        "noWait": false,
                        "envVariables": [ ]
                    },
                    "command": {
                        "file": "hello.exe",
                        "arguments": "--genrate hello_generated.exe",
                        "envVariables": [
                            {
                                "key": "HTTP_PROXY",
                                "value": "{httpProxy}"
                            },
                            {
                                "key": "HTTPS_PROXY",
                                "value": "{httpProxy}"
                            }
                        ]
                    },
                    "aliases": ["pip", "pip3", "pip35"]
                },
            ]
        }
    ]
}
```
