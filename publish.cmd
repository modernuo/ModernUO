:<<"::SHELLSCRIPT"
@ECHO OFF
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0publish.ps1" %*
exit /b %ERRORLEVEL%

::SHELLSCRIPT
# If run from bash (e.g., CI on Linux/macOS), dispatch to publish.sh
path=$(dirname "$0")
exec "$path/publish.sh" "$@"
