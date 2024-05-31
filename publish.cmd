:<<"::SHELLSCRIPT"
@ECHO OFF
GOTO :CMDSCRIPT

::SHELLSCRIPT
path=$(dirname "$0")
cd $path
./publish.sh
exit $?

:CMDSCRIPT
IF "%~1" == "" (
  SET config=-c Release
) ELSE (
  IF "%~1" == "release" (
    SET config=-c Release
  ) ELSE (
    SET config=-c Debug
  )
)

IF "%~2" == "" (
  SET os=-r win
) ELSE (
  SET os=-r %~2
)

IF "%~3" == "" (
  SET arch=x64
) ELSE (
  SET arch=%~3
)

echo dotnet tool restore
dotnet tool restore

echo dotnet clean --verbosity quiet
dotnet clean --verbosity quiet
echo dotnet restore --force-evaluate --source https://api.nuget.org/v3/index.json
dotnet restore --force-evaluate --source https://api.nuget.org/v3/index.json

echo dotnet publish %config% %os%-%arch% --no-restore --self-contained=false
dotnet publish %config% %os%-%arch% --no-restore --self-contained=false

echo Generating serialization migration schema...
dotnet tool run ModernUOSchemaGenerator -- ModernUO.sln
