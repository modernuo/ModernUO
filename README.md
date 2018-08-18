ModernUO
=====

### Requirements to Build/Run
- .NET Framework 4.7 or Mono 5.10+
- zlib (Linux only)

### Requirements to Dev
- Visual Studio 2017 or Visual Studio for Mac 2017

### Building for Windows
`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc /optimize /unsafe /t:exe /out:RunUO.exe /win32icon:Server\runuo.ico /d:NEWTIMERS /d:NEWPARENT /recurse:Server\\*.cs`

### Building for Mac/Linux (MONO)
`mcs -optimize+ -unsafe -t:exe -out:RunUO.exe -win32icon:Server/runuo.ico -nowarn:219,414 -d:NEWTIMERS -d:NEWPARENT -d:MONO -reference:System.Drawing -recurse:Server/*.cs`

### Running on Mac/Linux (MONO)
`mono RunUO.exe`