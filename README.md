runuo
=====

RunUO Git Repository

Typical Windows Build

PS C:\runuo> C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc /optimize /unsafe /t:exe /out:RunUO.exe /d:Framework_4_0 /win32icon:Server\runuo.ico /recurse:Server\\*.cs

Typical Linux Build (MONO)

~/runuo$ gmcs -optimize+ -unsafe -t:exe -out:RunUO.exe -win32icon:Server/runuo.ico -d:MONO -recurse:Server/*.cs
