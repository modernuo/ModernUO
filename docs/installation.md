---
title: Installation
---

# Installation

## Prerequisites

=== "Windows"

    - Download and install git from [here](https://git-scm.com/download/win)

    === "or via Scoop"
        - Install [scoop](https://scoop.sh)
        - Run:
        ```bash
        scoop install git
        ```

    === "or via Chocolatey"
        - Install [chocolatey](https://chocolatey.org)
        - Run:
        ```bash
        choco install git
        ```

    - Download and install the latest [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)

=== "OSX"
    - Install homebrew from [here](https://brew.sh)
    - Run:
      ```bash
      brew install git
      ```
    - Download and install the latest [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)

=== "Linux"
    - Run:
      ```bash
      sudo apt update && sudo apt install git
      ```
    - Download and install the latest [.NET Core SDK](instructions [here](https://docs.microsoft.com/en-us/dotnet/core/install/linux))

## Clone the Repository

=== "Windows"
    Navigate to the folder where you want to install ModernUO.

=== "OSX"
    Using _terminal_, navigate to the folder where you want to install ModernUO.

=== "Linux"
    Using _bash_, navigate to the folder where you want to install ModernUO.

Run:
```bash
git clone https://github.com/modernuo/modernuo
```

!!! Tip
    Operating System: :fontawesome-brands-windows:{: .windows }<br>&nbsp;&nbsp;&nbsp;&nbsp;Use _git bash_ as your command prompt
