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
  o="-r $1-x64"
else
  if [[ $(uname) = "Darwin" ]]
  then
    o="-r osx-x64"
  else
    o="-r linux-x64"
  fi
fi

echo dotnet publish ${c} ${o} --self-contained=false
dotnet publish ${c} ${o} --self-contained=false
exit $?

:CMDSCRIPT
Tools\build-native-libraries.cmd

IF "%~2" == "" (
  SET c = "-c Release"
) ELSE (
  SET c = "-c %~2"
)

IF "%~1" != "" (
  SET o = "-r %~1-x64"
) ELSE (
  SET o = "-r win-x64"
)

dotnet publish %c% %o% --self-contained=false
