:<<"::SHELLSCRIPT"
@ECHO OFF
GOTO :CMDSCRIPT

::SHELLSCRIPT
if [[ -z $2 ]]
then
  c="-c Release"
else
  c=-c $2
fi

dotnet build ${c} Projects/Argon2/Argon2.csproj
dotnet pack -o packages Projects/Argon2/Argon2.csproj
dotnet build ${c} Projects/ZLib/ZLib.csproj
dotnet pack -o packages Projects/ZLib/ZLib.csproj
exit $?

:CMDSCRIPT
IF "%~1" == "" (
  SET c=-c Release
) ELSE (
  SET c=-c %~1
)

dotnet build %c% Projects\Argon2\Argon2.csproj
dotnet pack -o packages Projects\Argon2\Argon2.csproj
dotnet build %c% Projects\ZLib\ZLib.csproj
dotnet pack -o packages Projects\ZLib\ZLib.csproj
