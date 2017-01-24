-- premake5.lua
workspace "CSLauncher"
   configurations { "Debug", "Release" }
   architecture "x86_64"
   location "Build"

project "LauncherLib"
   kind "SharedLib"
   language "C#"
   targetdir "Bin"
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
   targetdir "Bin"
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
   targetdir "Bin"
   links     { "System" }
   links     { "System.Runtime.Serialization" }
   links     { "System.Xml" }
   links     { "LauncherLib" }
   
   files { "Deployer/**.cs" }

   filter "configurations:Debug"
      defines { "DEBUG" }
      symbols "On" 

   filter "configurations:Release"
      defines { "NDEBUG" }
      optimize "On"
      symbols "Off"