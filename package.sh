#/bin/bash

ZIP7_EXE=$(cygpath -u "$PROGRAMFILES\\7-Zip\\7z.exe")

cd Build/bin/Release
"$ZIP7_EXE" a -tzip Deployer.zip *.exe *.dll
"$ZIP7_EXE" d Deployer.zip *.vshost.exe
