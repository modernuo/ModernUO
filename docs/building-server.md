---
title: Creating a build
---

# Creating a build

The server software must be built before it can be run.

=== "Windows"
    Using _windows terminal_, _git bash_, or _powershell_, run:
    ```bash
    ./publish.cmd <release|debug> <os> <arch>
    ```

=== "OSX/Linux"
    Using _terminal_, run:
    ```bash
    ./publish.sh <release|debug> <os> <arch>
    ```
<br><br>
`os` (_Optional_)<br>
The operating system to build the server against. If not specified then the server will be built for the same operating system.

:fontawesome-brands-windows:{: .windows } `win`<br>
:fontawesome-brands-apple:{: .apple } `osx`<br>
:fontawesome-brands-linux:{: .linux } `linux`<br>

<br><br>
`arch` (_Optional_)<br>
The architecture to build the server against. If not specified then the server will be built for x64.

`x64`<br>
`arm64`<br>
