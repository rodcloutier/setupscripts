$env:GOROOT=(go env GOROOT)
$env:GOPATH=$PSScriptRoot

Clear-Host

echo "GO workspace was setup properly"
echo "GOROOT=$env:GOROOT"
echo "GOPATH=$env:GOPATH"