runuo
=====

RunUO Git Repository

Typical Windows Build

PS C:\runuo> C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc /optimize /unsafe /t:exe /out:RunUO.exe /win32icon:Server\runuo.ico /recurse:Server\\*.cs


Typical Linux Build (MONO)

~/runuo$ dmcs -optimize+ -unsafe -t:exe -out:RunUO.exe -win32icon:Server/runuo.ico -nowarn:219,414 -d:MONO -recurse:Server/*.cs


zlib is required for certain functionality. Windows zlib builds are packaged with releases and can also be obtained separately here: https://github.com/msturgill/zlib/releases/tag/v1.2.8

Latest Razor builds can be found at https://bitbucket.org/msturgill/razor-releases/downloads

Latest UOSteam builds (previously AssistUO) can be found at http://uosteam.com

IRC: chat.freenode.net #runuo
