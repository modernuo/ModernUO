ModernUO
=====

### Contacts
[Join Discord Channel](https://discord.gg/VdyCpjQ)

### Goals
- See [Goals](./GOALS.md)

### Requirements
- .NET Core 3.0 (Preview 7) or newer
- zlib (Linux only)

### Building with Visual Studio 2019
- Publish `Server` project
- Build `Scripts` project

### Building with .NET Core 3.0 SDK
`dotnet publish /p:PublishProfiles=<profile>`
- Windows x64: `Windows`
- Linux/MacOSX x64 w/ .NET Core 3 installed: `Unix-Portable`
- Linux x64 w/ .NET Core 3: `Linux-SelfContained`
- MacOSX x64 /wo .NET Core 3: `MacOSX-SelfContained`

#### Running on Windows
- Follow the build instructions
- Run `Distribution\ModernUO.exe` or `dotnet ModernUO.dll`

#### Running on MacOSX or Linux
- Follow the build instructions
- Run `Distribution\ModernUO` or `dotnet ModernUO.dll`

#### Running on Linux
- Follow the build instructions
- Run `Distribution\ModernUO` or `dotnet ModernUO.dll`

### Troubleshooting / FAQ
