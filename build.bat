rmdir "./bin" /S /Q

cd src
dotnet publish -c Release -o "../bin"
