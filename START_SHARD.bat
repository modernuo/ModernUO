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
rem   3. Runs the task. The task runs this same file with --run in a VISIBLE
rem      console window titled "ModernUO shard" (Patrick, 13.10.2026: "remove
rem      headless, I want to see the process"). That mode names the log at start
rem      time - logs\session-yyyy-MM-dd_HH-mm.log, with seconds added if the
rem      name is taken, so a restart never overwrites an old log - and runs
rem      ModernUO.exe in the foreground through Git's tee.exe, so every line
rem      goes both to the window and to the log, and the task reads "Running"
rem      exactly while the shard lives. The server's console takes commands:
rem      "save", "shutdown", "help".
rem   4. Waits for "Listening: 127.0.0.1:2593" in the new log (a start takes
rem      about 15 s) and prints the address and the log; on a failure it
rem      prints the end of the log.
rem
rem  The console reaches the log through a pipe into tee.exe (Git for Windows),
rem  which passes the bytes through untouched, so the log is the same UTF-8 as
rem  before. Without tee the log gets cmd's own ">" redirection and the window
rem  stays blank. (-RedirectStandardOutput lost the process after 80 s on
rem  19.08.2026.) QuickEdit is switched off in that window: a stray click would
rem  start a selection, pause the console and with it the pipe, and the shard's
rem  log would drop lines while the selection stood.
rem
rem  Stopping: CLOSING THE WINDOW OR CTRL+C STOPS THE SHARD WITHOUT A SAVE.
rem  Type "save" in the window and wait for the save to finish, then
rem  "shutdown"; or Argus "do save" and "schtasks /End /TN ModernUO-shard".
rem  Killing inside an autosave truncates the world.
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
set "TEE=%ProgramFiles%\Git\usr\bin\tee.exe"

rem PowerShell, base64 UTF-16LE: SetConsoleMode on this console's input with
rem ENABLE_QUICK_EDIT_MODE (0x40) cleared and ENABLE_EXTENDED_FLAGS (0x80) set.
rem Encoded so cmd never has to parse its quotes and brackets.
set "QUICKEDITOFF=JABzAD0AJwBbAEQAbABsAEkAbQBwAG8AcgB0ACgAIgBrAGUAcgBuAGUAbAAzADIALgBkAGwAbAAiACkAXQAgAHAAdQBiAGwAaQBjACAAcwB0AGEAdABpAGMAIABlAHgAdABlAHIAbgAgAFMAeQBzAHQAZQBtAC4ASQBuAHQAUAB0AHIAIABHAGUAdABTAHQAZABIAGEAbgBkAGwAZQAoAGkAbgB0ACAAaAApADsAIABbAEQAbABsAEkAbQBwAG8AcgB0ACgAIgBrAGUAcgBuAGUAbAAzADIALgBkAGwAbAAiACkAXQAgAHAAdQBiAGwAaQBjACAAcwB0AGEAdABpAGMAIABlAHgAdABlAHIAbgAgAGIAbwBvAGwAIABHAGUAdABDAG8AbgBzAG8AbABlAE0AbwBkAGUAKABTAHkAcwB0AGUAbQAuAEkAbgB0AFAAdAByACAAaAAsACAAbwB1AHQAIAB1AGkAbgB0ACAAbQApADsAIABbAEQAbABsAEkAbQBwAG8AcgB0ACgAIgBrAGUAcgBuAGUAbAAzADIALgBkAGwAbAAiACkAXQAgAHAAdQBiAGwAaQBjACAAcwB0AGEAdABpAGMAIABlAHgAdABlAHIAbgAgAGIAbwBvAGwAIABTAGUAdABDAG8AbgBzAG8AbABlAE0AbwBkAGUAKABTAHkAcwB0AGUAbQAuAEkAbgB0AFAAdAByACAAaAAsACAAdQBpAG4AdAAgAG0AKQA7ACcAOwAgACQAawAgAD0AIABBAGQAZAAtAFQAeQBwAGUAIAAtAE0AZQBtAGIAZQByAEQAZQBmAGkAbgBpAHQAaQBvAG4AIAAkAHMAIAAtAE4AYQBtAGUAIABRAGUAIAAtAE4AYQBtAGUAcwBwAGEAYwBlACAAUwBoAGEAcgBkACAALQBQAGEAcwBzAFQAaAByAHUAOwAgACQAaAAgAD0AIAAkAGsAOgA6AEcAZQB0AFMAdABkAEgAYQBuAGQAbABlACgALQAxADAAKQA7ACAAJABtACAAPQAgAFsAdQBpAG4AdAAzADIAXQAwADsAIABpAGYAIAAoACQAawA6ADoARwBlAHQAQwBvAG4AcwBvAGwAZQBNAG8AZABlACgAJABoACwAIABbAHIAZQBmAF0AJABtACkAKQAgAHsAIABbAHYAbwBpAGQAXQAkAGsAOgA6AFMAZQB0AEMAbwBuAHMAbwBsAGUATQBvAGQAZQAoACQAaAAsACAAKAAoACQAbQAgAC0AYgBhAG4AZAAgACgALQBiAG4AbwB0ACAAWwB1AGkAbgB0ADMAMgBdADAAeAA0ADAAKQApACAALQBiAG8AcgAgAFsAdQBpAG4AdAAzADIAXQAwAHgAOAAwACkAKQAgAH0A"

rem --- Run mode: what the task executes. One parenthesised block, which cmd
rem     reads whole before running it, so editing this file while the shard
rem     runs cannot make cmd resume at a shifted offset when the shard exits.
if /i "%ARG%"=="--run" (
    if not exist "%LOGS%" mkdir "%LOGS%"
    "%SYS%\tasklist.exe" /FI "IMAGENAME eq ModernUO.exe" /NH 2>nul | "%SYS%\find.exe" /I "ModernUO.exe" >nul && exit /b 0
    for /f "usebackq delims=" %%L in (`%PS% -NoProfile -Command "$d = Get-Date; $p = Join-Path $env:LOGS ('session-{0:yyyy-MM-dd_HH-mm}.log' -f $d); if (Test-Path $p) { $p = Join-Path $env:LOGS ('session-{0:yyyy-MM-dd_HH-mm-ss}.log' -f $d) }; $p"`) do set "LOG=%%L"
    title ModernUO shard - type save, then shutdown. Closing this window stops it without a save.
    "%SYS%\chcp.com" 65001 >nul
    "%PS%" -NoProfile -NonInteractive -EncodedCommand %QUICKEDITOFF% >nul 2>&1
    cd /d "%DIST%"
    rem Each side of a pipe runs in its own cmd without delayed expansion, and
    rem that cmd does not expand a variable on its own: "call" makes it, so the
    rem log's name set just above reaches tee (tested 13.10.2026; without the
    rem call tee wrote to a file named %%LOG%%).
    if exist "%TEE%" (
        "%EXE%" 2>&1 | call "%TEE%" -a "%%LOG%%"
    ) else (
        echo   tee.exe not found at "%TEE%": the log gets everything, this window nothing.
        "%EXE%" > "!LOG!" 2>&1
    )
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

rem The task runs:  powershell -Command & '<this file>' --run
rem in the user's session, so its console window is visible for the shard's
rem lifetime. Closing it kills the shard without a save - that is how the
rem 11.10.2026 start ended (exit code 0xC000013A), when the window was hidden
rem for exactly that reason; since 13.10.2026 it is shown on purpose.
set "TASKARG=-NoProfile -ExecutionPolicy Bypass -Command & '%~f0' --run; exit $LASTEXITCODE"
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
echo   The shard runs in its own window "ModernUO shard"; this one may close.
echo   Do not close the shard's window: that stops it without a save.
echo   To stop: type "save" there, wait for the save, then "shutdown".
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
