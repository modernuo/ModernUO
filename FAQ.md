# Frequently Asked Questions
- [Where do I go to run ModernUO?](#where-do-i-go-to-run-modernuo-)
- [I built using Visual Studio, where are the files?](#i-built-using-visual-studio--where-are-the-files-)
- [I don't see my scripts compiling, what happened?](#i-don-t-see-my-scripts-compiling--what-happened-)
- [Why can't I run ModernUO on a 32-bit operating system?](#why-can-t-i-run-modernuo-on-a-32-bit-operating-system-)

## Where do I go to run ModernUO?
Everything is run from the `Distribution` directory.
This folder is portable, so it can be moved to the production server for deployments.

## I built using Visual Studio, where are the files?
Using Visual Studio to build will put the files into the default `Project/<ProjectName>/bin` folder. This is different from publishing using the instructions outlined in the [README.md](/README.md).

## I don't see my scripts compiling, what happened?
NET Core does not support dynamic compiling. Scripts are now compiled separately and moved to the `Distribution/Assemblies` directory.
This also allows content to be divided into `modules` that can be included by dropping in the DLL.

## Why can't I run ModernUO on a 32-bit operating system?
ModernUO is optimized for performance and modern hardware.
