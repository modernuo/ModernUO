dotnet build -c Debug -r linux-x64 Projects/Argon2/Argon2.csproj
dotnet publish -c Debug -r linux-x64 --self-contained=false
