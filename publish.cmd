dotnet restore --force-evaluate

:<<"::SHELLSCRIPT"
@ECHO OFF
GOTO :CMDSCRIPT

::SHELLSCRIPT
os=$1

if [[ $os ]]; then
  r="-r $os-x64"
elif [[ $(uname) = "Darwin" ]]; then
  r="-r osx-x64"
elif [[ -f /etc/os-release ]]; then
  . /etc/os-release
  NAME="$(tr '[:upper:]' '[:lower:]' <<< $NAME)"
  r="-r $NAME.$VERSION_ID-x64"
fi

framework="$(tr '[:upper:]' '[:lower:]' <<< $2)"

if [[ $framework = "net" ]]; then
  f="-f net5.0"
else
  f="-f netcoreapp3.1"
fi

if [[ -z $3 ]]; then
  c="-c Release"
else
  config="$(tr '[:lower:]' '[:upper:]' <<< ${3:0:1})${3:1}"
  c="-c $config"
fi

echo dotnet publish ${c} ${r} ${f} --no-restore --self-contained=false -o Distribution Projects/Server/Server.csproj
dotnet publish ${c} ${r} ${f} --no-restore --self-contained=false -o Distribution Projects/Server/Server.csproj
echo dotnet publish ${c} ${r} ${f} --no-restore --self-contained=false -o Distribution/Assemblies Projects/UOContent/UOContent.csproj
dotnet publish ${c} ${r} ${f} --no-restore --self-contained=false -o Distribution/Assemblies Projects/UOContent/UOContent.csproj
exit $?

:CMDSCRIPT
IF "%~2" == "" (
  SET f=-f netcoreapp3.1
) ELSE (
  IF "%~2" == "core" (
    SET f=-f netcoreapp3.1
  ) ELSE (
    SET f=-f net5.0
  )
)

IF "%~3" == "" or "%~3" == "release" (
  SET c=-c Release
) ELSE (
  SET c=-c Debug
)

IF "%~1" == "" (
  SET r=-r win-x64
) ELSE (
  SET r=-r %~1-x64
)

echo dotnet publish %c% %r% %f% --no-restore --self-contained=false -o Distribution Projects\Server\Server.csproj
dotnet publish %c% %r% %f% --no-restore --self-contained=false -o Distribution Projects\Server\Server.csproj
echo dotnet publish %c% %r% %f% --no-restore --self-contained=false -o Distribution\Assemblies Projects\UOContent\UOContent.csproj
dotnet publish %c% %r% %f% --no-restore --self-contained=false -o Distribution\Assemblies Projects\UOContent\UOContent.csproj
