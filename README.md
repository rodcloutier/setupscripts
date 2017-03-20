
# Build setup

The Deployer projects depends on the latest stable version of
[CommandLineParser](http://commandline.codeplex.com/) (1.9.71.2).
Since premake does not properly support NuGet, install the dependency using
VisualStudio. The steps decribed are for Vs2015

1. Right-click the Deployer project
2. Select `Manage NuGet Packages...` menu entry.
3. Change the `Package source` to `nuget.org`
4. Select `Browse`
5. Search for `CommandLineParser`
6. Install it
