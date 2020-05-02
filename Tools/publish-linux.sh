dotnet build -c Release -r linux-x64 Projects/Argon2/Argon2.csproj
dotnet publish -c Release -r linux-x64 --self-contained=false
