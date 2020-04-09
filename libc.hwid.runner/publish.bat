dotnet publish -c Release -r win-x86 --self-contained false /p:publishsinglefile=true -o "./bin/Release/netcoreapp3.1/publish/win-x86"
dotnet publish -c Release -r win-x64 --self-contained false /p:publishsinglefile=true -o "./bin/Release/netcoreapp3.1/publish/win-x64"
dotnet publish -c Release -r linux-x64 --self-contained false -o "./bin/Release/netcoreapp3.1/publish/linux-x64"