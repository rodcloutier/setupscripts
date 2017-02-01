# Setup Scripts
**The scripts/small apps you see here are a group of small utilities I use to setup my windows personal and work machine**

The gaming industry relies a lot on the use of windows (even exlusively since a lot of first party tooling is a actually only available on windows), but as I'm a big command line user, I always feel that I need to spend more time setting up my machine properly. As I'm changing machines more frequently now (thanks to windows 10 and the tests I'm doing with bleeding edge features) I'm pushing a couple of tools that I use on my personnal machine to accelerate the process. 

## cslauncher (you need .Net Framework 4.5)
New version of the launcher I'm using, deployer allows me to get dependencies and build aliases to the different apps copied. Note that I don't support installing packages and it's this way by design, it forces me to find or build an install-less package out of dependencies
See aliascreator.py part since it's pretty much end up doing the same thing. I wanted to add NuGet support and it was more conveinient doing it in C#

## aliascreator
I prefer usually running portable utilities rather than installing them, thus I have more control and understanding about what the tool needs to work properly. This utility allows me to link a bunch of executables to my user bin path.
Why not just use symlinks? when launched through cmd or powershell, Symlinks will behave like the executable was copied to the target location, meaning that the ExcutablePath will not point the the source executable path which meddles with DLL resolve paths or any instance where the program getting called relies on the ExecutablePath.