ModernUO
=====

The next generation Ultima Online Server Emulator.

### Join!
[Join Discord Channel](https://discord.gg/VdyCpjQ)

### Goals
- See [Goals](./GOALS.md)

# Requirements to Compile
- .NET Core 3.0 SDK

### Requirements to Run
- zlib (Linux only)

### Building with Visual Studio 2019
- Publish `Server` project
- Build `Scripts` project

### Building with .NET Core 3.0 SDK
`dotnet publish /p:PublishProfile=[platform]<-SelfContained>`
- `platform` can be `Windows`, `Linux`, or `OSX` (capitalization matters)
- Appending `-SelfContained` will export all .NET Core files required to run portably.

#### Running on Windows
- Follow the build instructions
- Run `Distribution\ModernUO.exe` or `dotnet ModernUO.dll`

#### Running on MacOSX or Linux
- Follow the build instructions
- Run `Distribution\ModernUO` or `dotnet ModernUO.dll`

#### Running on Linux
- Follow the build instructions
- Run `Distribution\ModernUO` or `dotnet ModernUO.dll`

### Thanks
- RunUO Team & Community
- ServUO Team & Community
- [Jaedan](https://github.com/jaedan) and the ClassicUO Community

### Troubleshooting / FAQ
