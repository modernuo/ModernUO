<p align="center">
  <img src="https://user-images.githubusercontent.com/3953314/92417551-a00d7600-f117-11ea-9c28-bb03bbdb1954.png" width=128px />
</p>

ModernUO [![Discord](https://img.shields.io/discord/751317910504603701?logo=discord&style=social)](https://discord.gg/NUhe7Pq9gF) [![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/modernuo?style=social&label=/r/modernuo)](https://www.reddit.com/r/ModernUO/) [![Twitter Follow](https://img.shields.io/twitter/follow/modernuo?label=@modernuo&style=social)](https://twitter.com/modernuo) [![Gitter](https://img.shields.io/gitter/room/modernuo/modernuo?logo=gitter&logoColor=46BC99&style=social)](https://gitter.im/modernuo/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
=====

##### Ultima Online Server Emulator for the modern era!
[![.NET](https://img.shields.io/badge/.NET-%205.0-5C2D91)](https://dotnet.microsoft.com/download/dotnet/5.0)
<br />
![Windows](https://img.shields.io/badge/-server%202019-0078D6?logo=windows)
![OSX](https://img.shields.io/badge/-big%20sur-222222?logo=apple&logoColor=white)
![Debian](https://img.shields.io/badge/-buster-A81D33?logo=debian)
![Ubuntu](https://img.shields.io/badge/-20LTS-E95420?logo=ubuntu&logoColor=white)
![CentOS](https://img.shields.io/badge/-8.3-262577?logo=centos&logoColor=white)
![Fedora](https://img.shields.io/badge/-33-0B57A4?logo=fedora&logoColor=white)
![RedHat](https://img.shields.io/badge/-8-BE0000?logo=red%20hat&logoColor=white)
<br/>
[![GitHub license](https://img.shields.io/github/license/modernuo/ModernUO?color=blue)](https://github.com/modernuo/ModernUO/blob/master/LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/issues)
<br />
[![GitHub build](https://img.shields.io/github/workflow/status/modernuo/ModernUO/Build?logo=github)](https://github.com/modernuo/ModernUO/actions)
[![AzurePipelines build](https://dev.azure.com/modernuo/modernuo/_apis/build/status/Build?branchName=main)](https://dev.azure.com/modernuo/modernuo/_build/latest?definitionId=1&branchName=main)

## Building the server
#### Requirements
- [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)

#### Publishing Builds
- Using terminal or powershell: `./publish.cmd [release|debug (default: release)] [os]`
  - `os` - [Supported operating systems](https://github.com/dotnet/core/blob/master/release-notes/5.0/5.0-supported-os.md)
    - `win` - Windows 8.1/10/2016/2019
    - `osx` - MacOS 10.13+/11.0 (High Sierra, Mojave, Catalina, & Big Sur)
    - `ubuntu.16.04`, `ubuntu.18.04` `ubuntu.20.04` - Ubuntu LTS
    - `debian.9`, `debian.10` - Debian
    - `centos.7`, `centos.8` - CentOS
    - `fedora.32`, `fedora.33` - Fedora
    - `rhel.7`, `rhel.8` - Redhat
    - If blank, the operating system running the build is used

## Deploying / Running Server
- Follow the [publish](https://github.com/modernuo/ModernUO#publishing-a-build) instructions
- Copy `Distribution` directory to the production server

#### Requirements
- [.NET 5 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0)

#### Running
- `dotnet ModernUO.dll`

#### Cleaning
- `dotnet clean`

## Thanks
- RunUO Team & Community
- ServUO Team & Community
- [Karasho](https://github.com/andreakarasho), [Jaedan](https://github.com/jaedan) and the ClassicUO Community

## Troubleshooting / FAQ
- See [FAQ](./FAQ.md)

## Want to sponsor?
Thank you for supporting us! You can find out how by visiting the [sponsors](./SPONSORS.md) page.

</br></br>
<p align=center>Development Tools provided with &hearts; by <br><a href="https://www.jetbrains.com/?from=ModernUO"><img src="https://user-images.githubusercontent.com/3953314/86882249-cfb2ea00-c0a4-11ea-9cec-bf3f3bcc6f28.png" width="100px" /></a></p>
