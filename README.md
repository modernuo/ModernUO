ModernUO
=====

Ultima Online Server Emulator for the modern era!

### Join and Follow!
[Discord Channel](https://discord.gg/VdyCpjQ)
[Twitter](https://www.twitter.com/modernuo)
[Reddit](https://www.reddit.com/r/modernuo)

### Goals
- See [Goals](./GOALS.md)

# Requirements to Compile
- [.NET Core 3.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0)
- [System.IO.Pipelines 4.6.0](https://www.nuget.org/packages/System.IO.Pipelines/) (Restored automatically during publishing)

### Requirements to Run
- zlib (Linux only)

### Building with .NET Core 3.0 SDK
`dotnet publish /p:PublishProfile=[platform][-SelfContained]`
- `platform` can be `Windows`, `Linux`, or `OSX` (capitalization matters)
- Appending `-SelfContained` will export all .NET Core files required to run portably.

### Running
- Follow the build instructions
- Go to `Distribution` directory

##### Windows
- Run `ModernUO.exe` or `dotnet ModernUO.dll`

##### OSX or Linux
- Run `./ModernUO` or `dotnet ./ModernUO.dll`

### Thanks
- RunUO Team & Community
- ServUO Team & Community
- [Jaedan](https://github.com/jaedan) and the ClassicUO Community

### Troubleshooting / FAQ

##### Where do I go to run ModernUO?
Everything is run from the `Distribution` directory.
This folder is portable, so it can be moved to the production server for deployments.

##### My scripts folder is not compiling during runtime, what happened?
NET Core does not support dynamic compiling. Scripts are now compiled separately and moved to the `Distribution/Assemblies` directory.
This also allows us to divide content into packages that can be included by dropping in the DLL.

##### Why can't I run ModernUO on a 32-bit operating system?
ModernUO is optimized for performance and modern hardware.
