dotnet build -c Release -r osx-x64 Projects/Argon2/Argon2.csproj
dotnet publish -c Release -r osx-x64 --self-contained=false
