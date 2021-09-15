<p align="center">
  <img src="https://user-images.githubusercontent.com/3953314/92417551-a00d7600-f117-11ea-9c28-bb03bbdb1954.png" width=128px />
</p>

ModernUO [![Discord](https://img.shields.io/discord/751317910504603701?logo=discord&style=social)](https://discord.gg/NUhe7Pq9gF) [![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/modernuo?style=social&label=/r/modernuo)](https://www.reddit.com/r/ModernUO/) [![Twitter Follow](https://img.shields.io/twitter/follow/modernuo?label=@modernuo&style=social)](https://twitter.com/modernuo) [![Gitter](https://img.shields.io/gitter/room/modernuo/modernuo?logo=gitter&logoColor=46BC99&style=social)](https://gitter.im/modernuo/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
=====

##### Ultima Online Server Emulator for the modern era!
[![GitHub license](https://img.shields.io/github/license/modernuo/ModernUO?color=blue)](https://github.com/modernuo/ModernUO/blob/master/LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/issues)
<br />
[![GitHub build](https://img.shields.io/github/workflow/status/modernuo/ModernUO/Build?logo=github)](https://github.com/modernuo/ModernUO/actions)
[![AzurePipelines build](https://dev.azure.com/modernuo/modernuo/_apis/build/status/Build?branchName=main)](https://dev.azure.com/modernuo/modernuo/_build/latest?definitionId=1&branchName=main)

## Requirements
#### Supported Operating Systems
[![Windows 8.1/10/2016/2019](https://img.shields.io/badge/-server%202019-0078D6?logo=windows)](https://www.microsoft.com/en-US/evalcenter/evaluate-windows-server-2019)
![MacOS 10/11](https://img.shields.io/badge/-big%20sur-222222?logo=apple&logoColor=white)
[![Debian 9/10](https://img.shields.io/badge/-buster-A81D33?logo=debian)](https://www.debian.org/distrib/)
[![Ubuntu 16/18/20 LTS](https://img.shields.io/badge/-20LTS-E95420?logo=ubuntu&logoColor=white)](https://ubuntu.com/download/server)
[![CentOS 7/8](https://img.shields.io/badge/-8.3-262577?logo=centos&logoColor=white)](https://www.centos.org/download/)
[![Fedora 32/33/34](https://img.shields.io/badge/-33-0B57A4?logo=fedora&logoColor=white)](https://getfedora.org/en/server/download/)
[![RedHat 7/8](https://img.shields.io/badge/-8-BE0000?logo=red%20hat&logoColor=white)](https://access.redhat.com/downloads)

#### Running the server
[![.NET](https://img.shields.io/badge/.NET-%205.0-5C2D91)](https://dotnet.microsoft.com/download/dotnet/5.0)

#### Development
[![git](https://img.shields.io/badge/-git-F05032?logo=git&logoColor=white)](https://git-scm.com/downloads)
[![.NET](https://img.shields.io/badge/.NET-%205.0.10%20SDK-5C2D91)](https://dotnet.microsoft.com/download/dotnet/5.0)

#### Supported IDEs
[<img width="64" alt="Jetbrains Rider 2021.2" src="https://user-images.githubusercontent.com/3953314/133473479-734e425c-fbb6-433a-af2d-2cc8444398e8.png">](https://www.jetbrains.com/rider/download)
[<img width="64" alt="Visual Studio 2019" src="https://user-images.githubusercontent.com/3953314/133473556-35fd48b4-6460-49b1-b7c5-b4a8c529cc04.png">](https://visualstudio.microsoft.com/downloads)
<br />
###### Note: VS Code is not currently supported.

## Getting Started
- Install prerequisite [requirements](https://github.com/modernuo/ModernUO#requirements)
- Clone this repository (or download the [latest](https://github.com/modernuo/ModernUO/archive/refs/heads/main.zip)):
  - `git clone https://github.com/modernuo/ModernUO.git`
- Open `ModernUO.sln` to start developing

## Building/Publishing
- Run `./publish.cmd [release|debug (default: release)] [os]`
  - `os` - [Supported operating systems](https://github.com/dotnet/core/blob/master/release-notes/5.0/5.0-supported-os.md)
    - `win` - Windows 8.1/10/2016/2019
    - `osx` - MacOS 10.13+/11.0 (High Sierra, Mojave, Catalina, & Big Sur)
    - `ubuntu.16.04`, `ubuntu.18.04` `ubuntu.20.04` - Ubuntu LTS
    - `debian.9`, `debian.10` - Debian
    - `centos.7`, `centos.8` - CentOS
    - `fedora.32`, `fedora.33`, `fedora.34` - Fedora
    - `rhel.7`, `rhel.8` - Redhat
    - If blank, the operating system running the build is used

**Note:** Building in Visual Studio (or Rider) will not run the schema migration. The schema migration ensures future changes
to the code will be backward compatible.

## Running the Server
- Follow the [publish](https://github.com/modernuo/ModernUO#publishing-builds) instructions
- Run `ModernUO.exe` or `dotnet ModernUO.dll` from the `Distribution` directory on the server

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
