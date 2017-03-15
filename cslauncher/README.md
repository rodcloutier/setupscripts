
# Build setup

To build the project you need to add those two tools to you path
- [Premake](https://premake.github.io/download.html#v5)
- [Paket](https://github.com/fsprojects/Paket/releases/tag/4.0.7)

Then run:
premake vs2015 && premake paket

Note that the build has only been tested so far on vs2015.

# Deployment format

Can either be written in json or yaml format.
Here is a documented example using yaml.

```yaml
    # Directory where the binaries (alias) will be deployed
    # optional: will default to %USERPROFILE% if not present
    binPath: ""

    # Directory where the packages will be deployed
    # optional: will default to %USERPROFILE%\packages if not present
    installPath: ""

    # Path to the launcher executable
    # mandatory: usually Launcher.exe if the launcher is in the deployer directory
    launcherPath: "Launcher.exe"

    # Path to the launcher executable
    # mandatory: usually LauncherLib.dll if the dll is in the deployer directory
    launcherLibPath": "LauncherLib.dll"

    # Http proxy to be used when getting packages or used in environment variables
    # optionnal
    httpProxy:

    # List of all the toolsets to install
    toolsets:

    # All toolset follow this structure

      # Name of the toolset
      # Mandatory
    - name: "hello"

      # Url where to find the package
      url: file://C:/DevTools/sources/hello-1.0.0.zip

      # Git configuration. Used to fetch from git instead of packages
      # Optional
      git:

          # The git url to use
          # Mandatory
          url: <url>

          # The commit to checkout
          # Optional: default to master
          commit: <sha|tag|branch>

      # List of tools to deploy from packages
      tools:

      # Simple executable hello
      - launcherConfig:
            exePath: "hello.exe"
            envVariables:
                - key: FOO
                  value: BAR
        aliases:
        - hello

      # Bash script
      - launcherConfig:
           exePath: hello_bash.sh
           type: script
        aliases:
        - hello_bash

      # Tool with command
      # The command will be executed after deployment
      - launcherConfig:
          exePath: hello.exe
        command:
          file: hello.exe
          argument: --arg1
          envVariables:
          - key: HTTP_PROXY
            value: "{httpProxy}"
          - key: HTTPS_PROXY
            value: "{httpProxy}"
        aliases:
        - hello_command

```

## Toolsets

```yaml

    # The configuration for the launcher
    # Mandatory
    launcherConfig:

        # The path, relative to package root, to which the alias will point to
        # Mandatory
        exePath: <path>

        # The type of laucher to create
        # Optional: defaults to exe
        type: <script|exe>

        # List of environment variables that will be injected at each launch
        # Optional
        envVariables:
        -key: <key name>
         value <value>

    # The aliase(s) that will be deployed, which point to the exePath
    # Mandatory
    aliases:
    - <alias>

    # Command to run after
    # optional
    command:

      # File to run
      # Mandatory
      file: <path)

      # Arguments to pass to the file
      # Mandatory
      arguments: <string>

      # List of environment variables that will be injected at each launch
      # Mandatory: even if empty
      envVariables:
      -key: <key name>
       value <value>
```
