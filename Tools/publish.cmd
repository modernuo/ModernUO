:<<"::SHELLSCRIPT"
@ECHO OFF
GOTO :CMDSCRIPT

::SHELLSCRIPT
if [[ -z $2 ]]
then
  c="-c Release"
else
  c="-c $2"
fi

dotnet restore --force-evaluate

if [[ $1 ]]
then
  r="-r $1-x64"
else
  if [[ $(uname) = "Darwin" ]]
  then
    r="-r osx-x64"
  else
    r="-r linux-x64"
  fi
fi

dotnet publish ${c} ${r} --no-restore --self-contained=false -o Distribution Projects/Server/Server.csproj
dotnet publish ${c} ${r} --no-restore --self-contained=false -o Distribution/Assemblies Projects/UOContent/UOContent.csproj
exit $?

:CMDSCRIPT
IF "%~2" == "" (
  SET c=-c Release
) ELSE (
  SET c=-c %~2
)

dotnet restore --force-evaluate

IF "%~1" == "" (
  SET r=-r win-x64
) ELSE (
  SET r=-r %~1-x64
)

dotnet publish %c% %r% --no-restore --self-contained=false -o Distribution Projects\Server\Server.csproj
dotnet publish %c% %r% --no-restore --self-contained=false -o Distribution\Assemblies Projects\UOContent\UOContent.csproj
