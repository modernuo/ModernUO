ModernUO is the Ultima Online Server Emulator for the modern era.

## Publishing a build
#### Requirements
- [.NET Core 3.1.7 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)
<br />or
- [.NET 5.0 (Preview) SDK](https://dotnet.microsoft.com/download/dotnet/5.0)

#### Publishing Builds
- Using terminal or powershell:
<br />`./publish.cmd [os] [framework] [release|debug (default: release)]`
    - Supported `os`:
      - `win` for Windows 8/10/2019
      - `osx` for MacOS x64
      - `ubuntu.16.04`, `ubuntu.18.04` `ubuntu.20.04` for Ubuntu LTS
      - `debian.9`, `debian.10` for Debian
      - `centos.7`, `centos.8` for CentOS
      - If blank, will use host operating system
    - Supported `framework`:
      - `core` for .NET Core 3.1.7
      - `net` for .NET 5.0

## Deploying / Running Server
- Follow the [publish](https://github.com/modernuo/ModernUO#publishing-a-build) instructions
- Copy `Distribution` directory to the production server

#### Requirements
- [.NET Core 3.1.7 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)
<br />or
- [.NET 5.0 (Preview) Runtime](https://dotnet.microsoft.com/download/dotnet/5.0)

#### Running
- `dotnet ModernUO.dll`

## Thanks
- RunUO Team & Community
- ServUO Team & Community
- [Jaedan](https://github.com/jaedan) and the ClassicUO Community

## Troubleshooting / FAQ
- See [FAQ](./FAQ.md)


</br></br>
<p align=center>Development Tools provided with &hearts; by <br><a href="https://www.jetbrains.com/?from=ModernUO"><img src="https://user-images.githubusercontent.com/3953314/86882249-cfb2ea00-c0a4-11ea-9cec-bf3f3bcc6f28.png" width="100px" /></a></p>
