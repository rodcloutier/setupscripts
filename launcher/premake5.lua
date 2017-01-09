-- premake5.lua
workspace "launcher"
   configurations { "Debug", "Release" }
   architecture "x86_64"
   location "build"

project "launcher"
   kind "ConsoleApp"
   language "C"
   targetdir "build/bin/%{cfg.buildcfg}"

   files { "**.h", "**.c" }

   filter "configurations:Debug"
      defines { "DEBUG" }
      symbols "On" 

   filter "configurations:Release"
      defines { "NDEBUG" }
      optimize "On"
      symbols "Off" 