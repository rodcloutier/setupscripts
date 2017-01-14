# Setup Scripts
**The scripts you see here a are small utilities that I use to setup my windows personal and work machine**

## aliascreator.py
I prefer usually running portable utilities rather than installing them, thus I have more control and understanding about what the tool needs to work properly. This utility allows me to link a bunch of executables to my user bin path.
Why not just use symlinks? when launched through cmd or powershell, Symlinks will behave like the executable was copied to the target location, meaning that the ExcutablePath will not point the the source executable path which meddles with DLL resolve paths or any instance where the program getting called relies on the ExecutablePath.