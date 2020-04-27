ModernUO
=====
##### Ultima Online Server Emulator for the modern era!

[![GitHub stars](https://img.shields.io/github/stars/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/issues)
![Platforms](https://img.shields.io/badge/platforms-linux%20%7C%20osx%20%7C%20windows-lightgrey)
![Build status](https://img.shields.io/circleci/build/gh/modernuo/ModernUO/master?logo=circleci)
[![GitHub license](https://img.shields.io/github/license/modernuo/ModernUO?color=blue)](https://github.com/modernuo/ModernUO/blob/master/LICENSE)
![Discord](https://img.shields.io/discord/458277173208547350?logo=discord)

## Join and Follow!
- [Discord](https://discord.gg/VdyCpjQ)
- [Twitter](https://www.twitter.com/modernuo)
- [Reddit](https://www.reddit.com/r/modernuo)

## Goals
- See [Goals](./GOALS.md)

## Publishing a build
#### Requirements
- [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)

#### Cleaning
- `dotnet clean`

#### Publishing
- Linux: `./Tools/publish-linux.sh` or `Tools/publish-linux.cmd`
- OSX: `./Tools/publish-osx.sh` or `Tools/publish-osx.cmd`
- Windows `./Tools/publish-windows.sh` or `Tools/publish-windows.cmd`

## Deploying / Running Server
- Follow the [publish](https://github.com/modernuo/ModernUO#publishing-a-build) instructions
- Copy `Distribution` directory to production server

#### Requirements
- [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- Zlib
  - Linux: `apt get zlib` or equiv for that distribution
  - OSX: `brew install zlib`
  - Windows: Included during publishing
- Optional: compile and install [Intel DRNG](https://github.com/modernuo/libdrng)

#### Windows
- Run `ModernUO.exe` or `dotnet ModernUO.dll`

#### OSX and Linux
- Run `./ModernUO` or `dotnet ./ModernUO.dll`

## Thanks
- RunUO Team & Community
- ServUO Team & Community
- [Jaedan](https://github.com/jaedan) and the ClassicUO Community

## Troubleshooting / FAQ
- See [FAQ](./FAQ.md)
