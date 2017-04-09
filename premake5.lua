-- premake5.lua
newaction {
    trigger     = "paket",
    description = "running paket to add the necessary dependencies",

    onWorkspace = function(wks)
        --runpaket(wks.location)
	end,

    execute = function()
        runpaket("Build")
    end
}

function runpaket(wslocation)
    print("Running Paket")
    local cwd = os.getcwd()
    os.chdir(cwd .. "/" .. wslocation)
    os.execute("paket install")
    os.chdir(cwd)
end

function nugetdependencies(wslocation, list)
    local paketdependencies = "source https://www.nuget.org/api/v2" .. "\r\n"

    for _, val in pairs(list) do
        paketdependencies = paketdependencies .. "nuget" .. val .. "\r\n"
    end

    ok, err = os.writefile_ifnotequal(paketdependencies, wslocation ..  "/paket.dependencies")

    -- local action = premake.action.current()
    -- packetonend = action.onEnd
    --action.onEnd = function()
    --    if packetonend ~= nil then
    --        packetonend()
    --    end
    --    runpaket(wslocation)
    --end
end

function nugetreferences(prjlocation, list)
    local paketreferences = ""

    for _, val in pairs(list) do
        paketreferences = paketreferences ..  val .. "\r\n"
    end

    ok, err = os.writefile_ifnotequal(paketreferences, prjlocation ..  "/paket.references")
end

workspace "CSLauncher"
    configurations { "Debug", "Release" }
    architecture "x86_64"
    location "Build"
    nugetdependencies ("Build", { "CommandLineParser >= 1.9.71", "YamlDotNet >= 4.1.0", "Nuget.Core >= 2.14" })

project "LauncherLib"
    kind "SharedLib"
    location "Build/LauncherLib"
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
    filter {}

project "Launcher"
    kind "ConsoleApp"
    location "Build/Launcher"
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
    filter {}

project "Deployer"
    kind "ConsoleApp"
    location "Build/Deployer"
    language "C#"
    targetdir "Build/bin/%{cfg.buildcfg}"
    nugetreferences ( "Build/Deployer", { "CommandLineParser", "YamlDotNet", "Nuget.Core" } )

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
    files { "*.yml" }

    filter "configurations:Debug"
        defines { "DEBUG" }
        symbols "On"

    filter "configurations:Release"
        defines { "NDEBUG" }
        optimize "On"
        symbols "Off"
    filter {}

    configuration "*.yml"
        buildaction "Copy"
    configuration "*.json"
        buildaction "Copy"
    filter {}

project "Packager"
    kind "ConsoleApp"
    location "Build/Packager"
    language "C#"
    targetdir "Build/bin/%{cfg.buildcfg}"

    nugetreferences ( "Build/Packager", { "CommandLineParser" } )

    links     { "System" }
    links     { "System.Runtime.Serialization" }
    links     { "System.Xml" }
    links     { "System.IO.Compression" }
    links     { "System.IO.Compression.FileSystem" }

    files { "Packager/**.cs" }

    filter "configurations:Debug"
        defines { "DEBUG" }
        symbols "On"

    filter "configurations:Release"
        defines { "NDEBUG" }
        optimize "On"
        symbols "Off"
    filter {}