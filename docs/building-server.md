---
title: Creating a build
---

# Creating a build

The server software must be built before it can be run.

=== "Windows"
    Using _command prompt_, _git bash_, or _powershell_, run:
    ```bash
    ./publish.cmd <release|debug> <os>
    ```

=== "OSX/Linux"
    Using _terminal_, run:
    ```bash
    ./publish.sh <release|debug> <os>
    ```
<br><br>
`os` (_Optional_)<br>
The operating system to build the server against. If not specified then the server will be built for the same operating system.

:fontawesome-brands-windows:{: .windows } `win`<br>
:fontawesome-brands-apple:{: .apple } `osx`<br>
:fontawesome-brands-ubuntu:{: .ubuntu } `ubuntu.16.04`, `ubuntu.18.04` `ubuntu.20.04`<br>
:brands-debian:{: .debian } `debian.9`, `debian.10`<br>
:fontawesome-brands-centos:{: .centos } `centos.7`, `centos.8`
