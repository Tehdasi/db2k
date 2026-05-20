rmdir /s /q bin
dotnet publish db2k.csproj --no-self-contained -p:PublishSingleFile=true
copy bin\release\net10.0\win-x64\publish\db2k.exe c:\apps\db2k