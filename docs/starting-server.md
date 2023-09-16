---
title: Starting the server
---

# Starting the server

Now that the software has been built, all you need to do is run it!
Everything is run from the _Distribution_ folder.

=== "Windows"
    Using _windows terminal_, _git bash_, or _powershell_, run:
    ```bash
    cd Distribution
    ModernUO.exe
    ```

=== "OSX/Linux"
    Using _terminal_, run:
    ```bash
    cd Distribution
    dotnet run ModernUO.dll
    ```

=== "Game Files"
    !!! Note
        Game files are required to initiate a server. Upon the initial launch, you will encounter a prompt asking for the location of your game files or ClassicUO installation. ClassicUO installations should be configured to reference game files directory within settings.json file.


    !!! Tip
        Game files are not directly distributed in ModernUO, find the latest version to download these at [client download](https://uo.com/Client-Download/)