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

if [[ $2 ]]
then
  o=" -r $1-x64"
fi
dotnet publish ${c}${o} --self-contained=false
exit $?

:CMDSCRIPT
Tools\build-native-libraries.cmd

IF "%~2" == "" (
  SET c = "-c Release"
) ELSE (
  SET c = "-c %~2"
)

IF "%~1" != "" (
  SET o = " -r %~1-x64"
)

dotnet publish %c%%o% --self-contained=false
