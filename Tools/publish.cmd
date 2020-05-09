dotnet clean
:<<"::SHELLSCRIPT"
@ECHO OFF
GOTO :CMDSCRIPT
::SHELLSCRIPT
Tools/build-native-libraries.cmd

if [[ -z $2 ]]
then
  c="-c Release"
else
  c=-c $2
fi

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

dotnet publish Projects/Server/Server.csproj ${c} ${r} --self-contained=false -o Distribution
dotnet publish Projects/Scripts/Scripts.csproj ${c} ${r} --self-contained=false -o Distribution/Assemblies
exit $?

:CMDSCRIPT
CALL Tools\build-native-libraries.cmd

IF "%~2" == "" (
  SET c =-c Release
) ELSE (
  SET c =-c %~2
)

IF NOT "%~1" == "" (
  SET r =-r %~1-x64
) ELSE (
  SET r =-r win-x64
)

dotnet publish Projects\Server\Server.csproj %c% %r% --self-contained=false -o Distribution
dotnet publish Projects\Scripts\Scripts.csproj %c% %r% --self-contained=false -o Distribution\Assemblies
