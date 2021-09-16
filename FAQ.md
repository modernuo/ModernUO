# Frequently Asked Questions
- [Why do I have thousands of compile errors](#why-do-i-have-thousands-of-compile-errors)
- [Where do I go to run ModernUO?](#where-do-i-go-to-run-modernuo)
- [I don't see my scripts compiling, what happened?](#i-dont-see-my-scripts-compiling-what-happened)
- [Why can't I run ModernUO on a 32-bit operating system?](#why-cant-i-run-modernuo-on-a-32-bit-operating-system)

## Why do I have thousands of compile errors?
ModernUO uses a new technique called source generation. Most likely the compile errors are related to that.
To resolve the compile errors make sure you are using Visual Studio 2019 v16.10+, or Rider 2021.2+ for development and the newest .NET available.
Specifically for Visual Studio, you must build the entire solution once, restart Visual Studio entirely, and then build again.
This is required every time code changes in the `SerializationGenerator` or `SerializationSchemaGenerator` projects.

## Where do I go to run ModernUO?
Everything is run from the `Distribution` directory.
This folder is portable, so it can be moved to the production server for deployments.

## I don't see my scripts compiling, what happened?
.NET 5 does not support dynamic compiling newer versions of C#. Scripts are now compiled separately and moved to the `Distribution/Assemblies` directory.

## Why can't I run ModernUO on a 32-bit operating system?
ModernUO is optimized for performance and modern hardware. For this reason we no longer support 32-bit operating systems
