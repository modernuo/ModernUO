ModernUO [![Discord](https://img.shields.io/discord/458277173208547350?logo=discord&style=social)](https://discord.gg/VdyCpjQ) [![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/modernuo?style=social&label=/r/modernuo)](https://www.reddit.com/r/ModernUO/) [![Twitter Follow](https://img.shields.io/twitter/follow/modernuo?label=@modernuo&style=social)](https://twitter.com/modernuo) [![Gitter](https://img.shields.io/gitter/room/modernuo/modernuo?logo=gitter&logoColor=46BC99&style=social)](https://gitter.im/modernuo/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
=====

##### Ultima Online Server Emulator for the modern era!
[![.NET Core](https://img.shields.io/badge/.NET-Core%203.1-5C2D91)](https://dotnet.microsoft.com/download/dotnet-core/3.1)
![Windows](https://img.shields.io/badge/-server%202019-0078D6?logo=windows)
![OSX](https://img.shields.io/badge/-catalina-222222?logo=apple&logoColor=white)
![Debian](https://img.shields.io/badge/-buster-A81D33?logo=debian)
![Ubuntu](https://img.shields.io/badge/-20LTS-E95420?logo=ubuntu&logoColor=white)
![CentOS](https://img.shields.io/badge/-8.1-262577?logo=centos&logoColor=white)
<br/>
[![GitHub license](https://img.shields.io/github/license/modernuo/ModernUO?color=blue)](https://github.com/modernuo/ModernUO/blob/master/LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/issues)
[![GitHub build](https://img.shields.io/github/workflow/status/modernuo/ModernUO/Build?logo=github)](https://github.com/modernuo/ModernUO/actions)
[![AzurePipelines build](https://dev.azure.com/modernuo/modernuo/_apis/build/status/Build?branchName=master)](https://dev.azure.com/modernuo/modernuo/_build/latest?definitionId=1&branchName=master)

## Goals
- See [Goals](./GOALS.md)

## Publishing a build
#### Requirements
- [.NET Core 3.1.4 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)

#### Publishing Builds
- `./Tools/publish.cmd [os] [Release|Debug (default: Release)]`
  - Supported `os`:
    - `win` for Windows 8/10/2019
    - `osx` for MacOS
    - `ubuntu.16.04`, `ubuntu.18.04` for Ubuntu (20.04 will be supported in the next .NET Core Release)
    - `debian.9`, `debian.10` for Debian
    - `centos.7`, `centos.8` for CentOS
    - If blank, will use host operating system

## Deploying / Running Server
- Follow the [publish](https://github.com/modernuo/ModernUO#publishing-a-build) instructions
- Copy `Distribution` directory to the production server

#### Requirements
- [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)

#### Running
- `dotnet ModernUO.dll`

#### Cleaning
- `dotnet clean`

## Thanks
- RunUO Team & Community
- ServUO Team & Community
- [Jaedan](https://github.com/jaedan) and the ClassicUO Community

## Troubleshooting / FAQ
- See [FAQ](./FAQ.md)
