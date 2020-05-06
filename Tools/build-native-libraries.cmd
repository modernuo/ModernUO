dotnet build -c Release Projects/Argon2/Argon2.csproj
dotnet pack -o ./packages Projects/Argon2/Argon2.csproj
dotnet build -c Release Projects/ZLib/ZLib.csproj
dotnet pack -o ./packages Projects/ZLib/ZLib.csproj
