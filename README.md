
# Build setup

To build the project you need to add those two tools to you path
- [Premake](https://premake.github.io/download.html#v5)
- [Paket](https://github.com/fsprojects/Paket/releases/tag/4.0.7)

Then run:
premake vs2015 && premake paket

Note that the build has only been tested so far on vs2015.

# Deployment format

Can either be written in json or yaml format.

```yaml
# path where aliases are going to be created, default value is %USERPROFILE$\bin
binPath: '%USERPROFILE%\testbin'
# path where packages are going to be downloaded, default value is binPath\packages
installPath: '%USERPROFILE%\testbin'
# proxy to be set in case case you are behind a corporate proxy, default value is null
httpProxy: &HTTP_PROXY null

# Repositories definition, id is a mandatory, unique identifier, type must be set to either [http, nuget, directory],
# source must be set to the location of the repository
repositories:
  - id: local
    type: directory
    source: C:\DevTools\sources

# Packages definition, note that packages are downloaded only if used
# id is the name of the pacakge, version is the complete version of the packages
# sourceId is the reposisotry used to get it
packages:
  - id: PortableGit
    version: "2.11.0.3-64-bit"
    sourceId: local
  - id: CPython
    version: "3.5.2-amd64"
    sourceId: local
    commands:
      - filePath: python.exe
        args: -m pip install -U pip
        envVariables: &HTTP_PROXY_ENV
          - key: HTTP_PROXY
            value: *HTTP_PROXY
          - key: HTTPS_PROXY
            value: *HTTP_PROXY
      - filePath: python.exe
        args: -m pip install pylint
        envVariables: *HTTP_PROXY_ENV
      - filePath: python.exe
        args: -m pip install azure-cli
        envVariables: *HTTP_PROXY_ENV
  - id: CPython
    version: "2.7.12-amd64"
    sourceId: local
  - id: AzCopy
    version: "1.0.0-tag"
    sourceId: local

# toolsets definition
toolsets:
  - id: azcopy
    packageSpec: AzCopy
    tools:
      - path: AzCopy.exe
        aliases: [azcopy]
  - id: git
    packageSpec: PortableGit
    tools:
      - path: bin\git.exe
        aliases: [git]
      - path: bin\bash.exe
        aliases: [git-bash]
  - id: git
    packageSpec: PortableGit
    tools:
      - path: bin\git.exe
        aliases: [git]
      - path: bin\bash.exe
        aliases: [git-bash]
  - id: python3
    packageSpec: &MAIN_PYTHON CPython >= 3.5
    tools:
      - path: python.exe
        aliases: [python, python3]
      - path: Scripts\pip.exe
        aliases: [pip, pip3]
      - path: Scripts\pylint.exe
        aliases: [pylint, pylint3]
  - id: azure-cli-2
    packageSpec: *MAIN_PYTHON
    tools:
      - path: Scripts\az.bat
        aliases: [az, az2]

```
