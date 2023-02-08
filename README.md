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
[![GitHub build](https://img.shields.io/github/actions/workflow/status/modernuo/ModernUO/build-test.yml?branch=main&logo=github)](https://github.com/modernuo/ModernUO/actions)
[![AzurePipelines build](https://dev.azure.com/modernuo/modernuo/_apis/build/status/Build?branchName=main)](https://dev.azure.com/modernuo/modernuo/_build/latest?definitionId=1&branchName=main)

## Requirements
#### Supported Operating Systems
[![Windows 10/11/2016/2019/2022](https://img.shields.io/badge/-server%202022-0078D6?logo=windows&logoColor=0078D6&labelColor=222222)](https://www.microsoft.com/en-US/evalcenter/evaluate-windows-server-2022)
![MacOS 10.15+](https://img.shields.io/badge/-ventura-222222?logo=apple&logoColor=white&labelColor=222222)
[![Debian 10+](https://img.shields.io/badge/-bullseye-A81D33?logo=debian&logoColor=A81D33&labelColor=222222)](https://www.debian.org/distrib/)
[![Ubuntu 16+ LTS](https://img.shields.io/badge/-22LTS-E95420?logo=ubuntu&logoColor=E95420&labelColor=222222)](https://ubuntu.com/download/server)
<br />
[![Alpine 3.15+](https://img.shields.io/badge/-3.17-0D597F?logo=alpinelinux&logoColor=0D597F&labelColor=222222)](https://alpinelinux.org/downloads/)
[![Fedora 33+](https://img.shields.io/badge/-37-51a2da?logo=fedora&logoColor=51a2da&labelColor=222222)](https://getfedora.org/en/server/download/)
[![RedHat 7/8](https://img.shields.io/badge/-8-BE0000?logo=redhat&logoColor=BE0000&labelColor=222222)](https://access.redhat.com/downloads)
[![CentOS 7/8/9](https://img.shields.io/badge/-9-262577?logo=centos&logoColor=white&labelColor=222222)](https://www.centos.org/download/)
[![openSUSE 15+](https://img.shields.io/badge/-15-73BA25?logo=openSUSE&logoColor=73BA25&labelColor=222222)](https://get.opensuse.org/)
[![SUSE Enterprise 12 SP2+](https://img.shields.io/badge/-12%20SP2-0C322C?logo=suse&logoColor=30BA78&labelColor=222222)](https://www.suse.com/download/sles/)
[![Linux Mint 17+](https://img.shields.io/badge/-20-87CF3E?logo=linux%20mint&logoColor=87CF3E&labelColor=222222)](https://linuxmint.com/download.php)
[![Arch](https://img.shields.io/badge/-Arch-1793D1?logo=archlinux&logoColor=1793D1&labelColor=222222)](https://archlinux.org/download/)

#### Running the server
[![.NET](https://img.shields.io/badge/-7.0.1-5C2D91?logo=.NET&logoColor=white&labelColor=222222)](https://dotnet.microsoft.com/download/dotnet/7.0)

#### Development
[![git](https://img.shields.io/badge/-git-F05032?logo=git&logoColor=F05032&labelColor=222222)](https://git-scm.com/downloads)
[![.NET](https://img.shields.io/badge/-%207.0.101%20SDK-5C2D91?logo=.NET&logoColor=white&labelColor=222222)](https://dotnet.microsoft.com/download/dotnet/7.0)

#### Supported IDEs

<p align="left"><a href="https://www.jetbrains.com/rider/download"><img height="64" title="Jetbrains Rider 2022.3.1+"
      alt="Jetbrains Rider 2022.3.1+"
      src="https://user-images.githubusercontent.com/3953314/133473479-734e425c-fbb6-433a-af2d-2cc8444398e8.png"></a><img
      alt="space" width="32" src="https://user-images.githubusercontent.com/3953314/200151935-3c1521ec-16cb-487b-85a2-7454d347c585.png"><a href="https://code.visualstudio.com/download"><img height="64" title="VSCode"
      alt="VSCode"
      src="https://user-images.githubusercontent.com/3953314/200161017-7697171f-8f13-4829-95d0-8a25b59ee4c9.png"></a><img alt="space" width="32"
      src="https://user-images.githubusercontent.com/3953314/200151935-3c1521ec-16cb-487b-85a2-7454d347c585.png"><a href="https://visualstudio.microsoft.com/vs/community/"><img height="64" title="Visual Studio 2022 v17.4+"
      alt="Visual Studio 2022 v17.4+"
      src="https://user-images.githubusercontent.com/3953314/133473556-35fd48b4-6460-49b1-b7c5-b4a8c529cc04.png"></a></p>

## Getting Started
- Install prerequisite [requirements](https://github.com/modernuo/ModernUO#requirements)
- Clone this repository (or download the [latest](https://github.com/modernuo/ModernUO/archive/refs/heads/main.zip)):
  - `git clone https://github.com/modernuo/ModernUO.git`
- Open `ModernUO.sln` to start developing

## Building/Publishing
- Run `./publish.cmd [release|debug (default: release)] [os] [arch (default: x64)]`
  - `os` - [Supported operating systems](https://github.com/dotnet/core/blob/main/release-notes/7.0/supported-os.md)
    - `win` - Windows 10/11/2016/2019/2022
    - `osx` - MacOS 10.15/11/12/13 (Catalina, Big Sur, Monterey, Ventura)
    - `linux` - Linux
  - `arch`
    - `x64` - Intel 64-bit
    - `arm64` - ARM 64-bit

## Linux Prerequisites
### Fedora, CentOS, RHEL, etc
```shell
dnf upgrade --refresh -y
# CentOS does not come with EPEL enabled
dnf install -y epel-release epel-next-release
# Note: libargon2 is old.
# Download/symlink from here: https://github.com/modernuo/Argon2.Bindings/tree/main/runtimes
dnf install -y findutils libicu zlib-devel zstd libargon2-devel tzdata
```

### Ubuntu, Debian, etc
```shell
apt-get update -y
apt-get install -y libicu-dev libz-dev zstd libargon2-dev tzdata
```

## Running the Server
- Follow the [publish](https://github.com/modernuo/ModernUO#publishing-builds) instructions
- Run `ModernUO.exe` or `dotnet ModernUO.dll` from the `Distribution` directory on the server

## Troubleshooting / FAQ
- See [FAQ](./FAQ.md)

## Want to sponsor?
Thank you for supporting us! You can find out how by visiting the [sponsors](./SPONSORS.md) page.

## Collaborators
[![Kamron Batman](https://images.weserv.nl/?url=avatars.githubusercontent.com/u/3953314&h=64&w=64&fit=cover&mask=circle&maxage=1d)](https://github.com/kamronbatman)
[![Mark1145](https://images.weserv.nl/?url=avatars.githubusercontent.com/u/15312181&h=64&w=64&fit=cover&mask=circle&maxage=1d)](https://github.com/mark1145)

## Thanks
- RunUO Team & Community
- [Voxpire](https://github.com/Voxpire), the ServUO Team & Community
- [Karasho](https://github.com/andreakarasho), [Jaedan](https://github.com/jaedan) and the ClassicUO Community

</br></br>
<p align=center>Development Tools & Plugins provided with &hearts; by <br /><a href="https://www.jetbrains.com/?from=ModernUO"><img align=middle src="https://user-images.githubusercontent.com/3953314/86882249-cfb2ea00-c0a4-11ea-9cec-bf3f3bcc6f28.png" height="64px" alt="JetBrains" title="JetBrains" /></a>
<a href="https://material-theme.com/"><img align=center src="https://material-theme.com/img/logo/material-oceanic.svg" width="64px" alt="Material Theme" title="Material Theme"></a>
</p>
