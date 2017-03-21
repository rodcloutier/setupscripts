-- premake5.lua
workspace "CSLauncher"
    configurations { "Debug", "Release" }
    architecture "x86_64"
    location "Build"

project "LauncherLib"
    kind "SharedLib"
    language "C#"
    targetdir "Build/bin/%{cfg.buildcfg}"
    links     { "System" }
    links     { "System.Runtime.Serialization" }
    links     { "System.Xml" }

    files { "LauncherLib/**.cs" }

    filter "configurations:Debug"
        defines { "DEBUG" }
        symbols "On"

    filter "configurations:Release"
        defines { "NDEBUG" }
        optimize "On"
        symbols "Off"


project "Launcher"
    kind "ConsoleApp"
    language "C#"
    targetdir "Build/bin/%{cfg.buildcfg}"
    links     { "System" }
    links     { "LauncherLib" }

    files { "Launcher/**.cs" }

    filter "configurations:Debug"
        defines { "DEBUG" }
        symbols "On"

   filter "configurations:Release"
        defines { "NDEBUG" }
        optimize "On"
        symbols "Off"

project "Deployer"
    kind "ConsoleApp"
    language "C#"
    targetdir "Build/bin/%{cfg.buildcfg}"
    -- nuget     { "NuGet.Core:2.14" }
    -- nuget     { "CommandLineParser:1.9.71" }
    links     { "System" }

    links     { "System.Runtime.Serialization" }
    links     { "System.Xml" }
    links     { "System.IO.Compression" }
    links     { "System.IO.Compression.FileSystem" }
    links     { "LauncherLib" }

    filter { "system:macosx" }
        links     { "System.Core" }
    filter {}

    files { "Deployer/**.cs" }
    files { "*.json" }

    filter "configurations:Debug"
        defines { "DEBUG" }
        symbols "On"

    filter "configurations:Release"
        defines { "NDEBUG" }
        optimize "On"
        symbols "Off"
    filter {}

    configuration "deployment.json"
        buildaction "Copy"
