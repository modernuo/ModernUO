# Frequently Asked Questions
- [Where do I go to run ModernUO?](#where-do-i-go-to-run-modernuo)
- [I don't see my scripts compiling, what happened?](#i-dont-see-my-scripts-compiling-what-happened)
- [Why can't I run ModernUO on a 32-bit operating system?](#why-cant-i-run-modernuo-on-a-32-bit-operating-system)

## Where do I go to run ModernUO?
Everything is run from the `Distribution` directory.
This folder is portable, so it can be moved to the production server for deployments.

## I don't see my scripts compiling, what happened?
NET Core does not support dynamic compiling. Scripts are now compiled separately and moved to the `Distribution/Assemblies` directory.
This also allows content to be divided into `modules` that can be included by dropping in the DLL.

## Why can't I run ModernUO on a 32-bit operating system?
ModernUO is optimized for performance and modern hardware.
