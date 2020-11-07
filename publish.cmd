dotnet restore --force-evaluate

:<<"::SHELLSCRIPT"
@ECHO OFF
GOTO :CMDSCRIPT

::SHELLSCRIPT
config=$1
os=$2

if [[ $os ]]; then
  os="-r $os-x64"
elif [[ $(uname) = "Darwin" ]]; then
  os="-r osx-x64"
elif [[ -f /etc/os-release ]]; then
  . /etc/os-release
  NAME="$(tr '[:upper:]' '[:lower:]' <<< $NAME)"
  os="-r $NAME.$VERSION_ID-x64"
fi

if [[ $config ]]; then
  config="$(tr '[:lower:]' '[:upper:]' <<< ${3:0:1})${3:1}"
  config="-c $config"
else
  config="-c Release"
fi

echo dotnet publish ${config} ${os} -f net5.0 --no-restore --self-contained=false -o Distribution Projects/Server/Server.csproj
dotnet publish ${config} ${os} -f net5.0 --no-restore --self-contained=false -o Distribution Projects/Server/Server.csproj
echo dotnet publish ${config} ${os} -f net5.0 --no-restore --self-contained=false -o Distribution/Assemblies Projects/UOContent/UOContent.csproj
dotnet publish ${config} ${os} -f net5.0 --no-restore --self-contained=false -o Distribution/Assemblies Projects/UOContent/UOContent.csproj
exit $?

:CMDSCRIPT

IF "%~1" == "" or "%~1" == "release" (
  SET config=-c Release
) ELSE (
  SET config=-c Debug
)

IF "%~2" == "" (
  SET os=-r win-x64
) ELSE (
  SET os=-r %~1-x64
)

echo dotnet publish %config% %os% -f net5.0 --no-restore --self-contained=false -o Distribution Projects\Server\Server.csproj
dotnet publish %config% %os% -f net5.0 --no-restore --self-contained=false -o Distribution Projects\Server\Server.csproj
echo dotnet publish %config% %os% -f net5.0 --no-restore --self-contained=false -o Distribution\Assemblies Projects\UOContent\UOContent.csproj
dotnet publish %config% %os% -f net5.0 --no-restore --self-contained=false -o Distribution\Assemblies Projects\UOContent\UOContent.csproj
