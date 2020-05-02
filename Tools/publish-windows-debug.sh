dotnet build -c Debug -r win-x64 Projects/Argon2/Argon2.csproj
dotnet publish -c Debug -r win-x64 --self-contained=false
