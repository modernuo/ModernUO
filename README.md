ModernUO
=====

### Contacts
[Join Discord Channel](https://discord.gg/VdyCpjQ)

### Goals
- See [Goals](./GOALS.md)

### Requirements
- .NET Framework 4.7 or Mono 5.10+
- zlib (Linux only)
- [DotNetCompilerPlatform](https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform) v2.0+ (Windows Only)

### Building using Visual Studio (Recommended)
- Build `Server` project
  - Building with Visual Studio for Windows will install `DotNetCompilerPlatform` automatically.

### Building for Windows (Without Visual Studio)
`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc /optimize /unsafe /t:exe /out:RunUO.exe /win32icon:Server\runuo.ico /d:NEWTIMERS /d:NEWPARENT /recurse:Server\\*.cs`
- DotNetCompilerPlatform must be installed with the `csc.exe` file in the `roslyn` folder at the root of the repository.

### Building for Mac/Linux (Without Visual Studio)
`mcs -optimize+ -unsafe -t:exe -out:RunUO.exe -win32icon:Server/runuo.ico -nowarn:219,414 -d:NEWTIMERS -d:NEWPARENT -d:MONO -reference:System.Drawing -recurse:Server/*.cs`

### Running on Mac/Linux (MONO)
`mono RunUO.exe`

### Troubleshooting / FAQ

#### Scripts fail to compile on Windows
This is usually caused nuget packages not being installed. Install the required packages and rebuild the Server project.
Note: The required nuget packages can be set to automatically restore on build with this [setting](https://docs.microsoft.com/en-us/nuget/consume-packages/media/restore-01-autorestoreoptions.png)
