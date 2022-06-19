---
title: Installation
---

# Installation

=== "Windows"
    ### Prerequisites
    1. Download and install the latest [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
    1. Download and install from [here](https://git-scm.com/download/win)

        !!! Tip
            Use Git Bash as your command prompt. This can be found in the Windows Start Menu after installation.

    ### Install ModernUO
    1. Navigate to the folder where you want to install ModernUO.
    1. Using _Git Bash_ run:
        ```bash
        git clone https://github.com/modernuo/modernuo
        cd modernuo
        ```

=== "OSX"
    ### Prerequisites
    1. Download and install the latest [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).
    1. Using _terminal_, install [homebrew](https://brew.sh) and git:
        ```bash
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
        brew install git
        ```
    ### Install ModernUO
    1. Using _terminal_, navigate to the folder where you want to install ModernUO and run:
       ```bash
        git clone https://github.com/modernuo/modernuo
        cd modernuo
       ```

=== "Linux"
    ### Prerequisites
    1. Download and install the latest [.NET 6 SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux).
    1. Using _bash_, install git:
        ```bash
        sudo apt update && sudo apt install git
        ```

        !!! Note
            The command to install git might be different for your flavor of linux. Consult your local Google search for answers.

    ### Install ModernUO
    1. Using _bash_, navigate to the folder where you want to install ModernUO and run:
       ```bash
        git clone https://github.com/modernuo/modernuo
        cd modernuo
       ```
