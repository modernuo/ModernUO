@echo off
rem ===========================================================================
rem  START_SHARD.bat -- starts the ModernUO shard. This is the only launcher:
rem  nothing starts the shard on its own (no logon task, no Startup entry).
rem
rem  Double-click it, or from a script:  START_SHARD.bat --nopause
rem
rem  What it does:
rem   1. Refuses if ModernUO.exe is missing or a shard is already running.
rem   2. Registers the scheduled task "ModernUO-shard" with NO triggers, so it
rem      runs only when this file asks. Going through Task Scheduler is what
rem      lets the shard outlive whatever started it: a shard started as a child
rem      of an app's shell died together with that app twice on 05.09.2026.
rem   3. Runs the task. The task runs this same file with --run in a hidden
rem      window. That mode names the log at start time -
rem      logs\session-yyyy-MM-dd_HH-mm.log, with seconds added if the name is
rem      taken, so a restart never overwrites an old log - and runs
rem      ModernUO.exe in the foreground, so the task reads "Running" exactly
rem      while the shard lives.
rem   4. Waits for "Listening: 127.0.0.1:2593" in the new log (a start takes
rem      about 15 s) and prints the address and the log; on a failure it
rem      prints the end of the log.
rem
rem  The console reaches the log through cmd's own ">" redirection:
rem  -RedirectStandardOutput lost the process after 80 s on 19.08.2026.
rem
rem  Stopping: save first (Argus "do save", or [save in game), then
rem  "schtasks /End /TN ModernUO-shard". /End kills the process; it does not
rem  save, and killing inside an autosave truncates the world.
rem ===========================================================================
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
set "DIST=%ROOT%\Distribution"
set "EXE=%DIST%\ModernUO.exe"
set "LOGS=%ROOT%\logs"
set "TASK=ModernUO-shard"
set "ARG=%~1"

rem System tools by full path: from Git Bash, /usr/bin comes first in PATH and
rem its find and timeout would answer instead - "not running" while a shard
rem runs, and the re-registration below would then kill it without a save.
set "SYS=%SystemRoot%\System32"
set "PS=%SYS%\WindowsPowerShell\v1.0\powershell.exe"

rem --- Run mode: what the task executes. One parenthesised block, which cmd
rem     reads whole before running it, so editing this file while the shard
rem     runs cannot make cmd resume at a shifted offset when the shard exits.
if /i "%ARG%"=="--run" (
    if not exist "%LOGS%" mkdir "%LOGS%"
    "%SYS%\tasklist.exe" /FI "IMAGENAME eq ModernUO.exe" /NH 2>nul | "%SYS%\find.exe" /I "ModernUO.exe" >nul && exit /b 0
    for /f "usebackq delims=" %%L in (`%PS% -NoProfile -Command "$d = Get-Date; $p = Join-Path $env:LOGS ('session-{0:yyyy-MM-dd_HH-mm}.log' -f $d); if (Test-Path $p) { $p = Join-Path $env:LOGS ('session-{0:yyyy-MM-dd_HH-mm-ss}.log' -f $d) }; $p"`) do set "LOG=%%L"
    cd /d "%DIST%"
    "%EXE%" > "!LOG!" 2>&1
    exit /b !ERRORLEVEL!
)

rem --- Start mode.
echo.
echo   ModernUO shard
echo   --------------

if not exist "%EXE%" (
    echo   Not built: "%EXE%" is missing.
    echo   Build it first: dotnet build ModernUO.slnx -c Release
    goto :fail
)

call :is_running
if not errorlevel 1 (
    call :newest
    echo   Already running.
    if defined NEWEST echo   Log: %LOGS%\!NEWEST!
    goto :done
)

if not exist "%LOGS%" mkdir "%LOGS%"

rem The task runs:  powershell -WindowStyle Hidden -Command & '<this file>' --run
rem PowerShell hands the .bat to cmd inside its own hidden console, so no window
rem stays open for the shard's lifetime - closing such a window kills the shard
rem (that is how it ended on 11.10.2026, exit code 0xC000013A).
set "TASKARG=-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command & '%~f0' --run; exit $LASTEXITCODE"
"%PS%" -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; $a = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $env:TASKARG -WorkingDirectory $env:ROOT; $s = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -DontStopOnIdleEnd; Register-ScheduledTask -TaskName $env:TASK -Action $a -Settings $s -Description 'ModernUO shard. No triggers: only START_SHARD.bat starts it.' -Force | Out-Null"
if errorlevel 1 (
    echo   Could not register the scheduled task "%TASK%".
    goto :fail
)

call :newest
set "BEFORE=!NEWEST!"

"%SYS%\schtasks.exe" /Run /TN "%TASK%" >nul
if errorlevel 1 (
    echo   Task Scheduler would not run "%TASK%".
    goto :fail
)
echo   Starting...

set /a SECS=0
:wait
>nul "%SYS%\PING.EXE" -n 2 127.0.0.1
set /a SECS+=1
call :newest
if "!NEWEST!"=="!BEFORE!" (
    if !SECS! geq 30 (
        echo   No new log appeared in logs\ within 30 s.
        goto :fail
    )
    goto :wait
)
"%SYS%\findstr.exe" /c:"Listening: 127.0.0.1:2593" "%LOGS%\!NEWEST!" >nul && goto :up
call :is_running
if errorlevel 1 if !SECS! geq 5 (
    echo   The shard exited while starting. End of its log:
    goto :tail
)
if !SECS! geq 180 (
    echo   Not listening after 180 s. End of its log:
    goto :tail
)
goto :wait

:up
echo.
echo   Up after ~!SECS! s, listening on 127.0.0.1:2593
"%SYS%\findstr.exe" /c:"127.0.0.1:2599" "%LOGS%\!NEWEST!" >nul && echo   Dashboard: http://127.0.0.1:2599/
echo   Log: %LOGS%\!NEWEST!
echo   The shard keeps running after this window closes.
goto :done

:tail
echo.
"%PS%" -NoProfile -Command "Get-Content -LiteralPath (Join-Path $env:LOGS $env:NEWEST) -Tail 25"
goto :fail

:fail
echo.
echo   START FAILED
if /i not "%ARG%"=="--nopause" pause
exit /b 1

:done
echo.
if /i not "%ARG%"=="--nopause" "%SYS%\timeout.exe" /t 15
exit /b 0

:is_running
"%SYS%\tasklist.exe" /FI "IMAGENAME eq ModernUO.exe" /NH 2>nul | "%SYS%\find.exe" /I "ModernUO.exe" >nul
exit /b

:newest
set "NEWEST="
for /f "delims=" %%F in ('dir /b /a-d /o-d "%LOGS%\session-*.log" 2^>nul') do if not defined NEWEST set "NEWEST=%%F"
exit /b 0
