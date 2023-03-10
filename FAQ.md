# Frequently Asked Questions

## What has changed since RunUO 2.7?
We have compiled a non-exhaustive list of the features changes, technical improvements, and new additions.
- See: [RunUO to ModernUO](./RUNUO_TO_MODERNUO.md)

## Why do I have thousands of compile errors?
ModernUO uses a _very_ new technique called source generation. Most likely the compile errors are related to the source generator crashing.
To resolve the compile errors make sure you are using Visual Studio 2022, or Rider 2022+ for development and the newest .NET available.
Specifically for Visual Studio, you must build the entire solution once, restart Visual Studio entirely, and then build again.

It is possible that while you are developing, many of your files will go red and properties/symbols cannot be resolved. Contact us on discord
and let us know how to reproduce it so we can get it fixed! Under the same vein, we are working on building an analyzer for the IDE so if there
are errors, you are not in the dark.

## Where do I go to run ModernUO?
Everything is run from the `Distribution` directory.
This folder is portable, so it can be moved to the production server for deployments.

## I don't see my scripts compiling, what happened?
Scripts are now compiled separately (UOContent project) and moved to the `Distribution/Assemblies` directory.

## Why can't I run ModernUO on a 32-bit operating system?
ModernUO is optimized for modern hardware. We cannot support 32-bit systems, sorry.

## I get an InvalidTimeZoneException or TimeZoneNotFoundException error, how can I fix it?
1. Make sure you are on a version of Windows that is supported by ModernUO.
2. Update Windows completely, usually the latest cumulative update will be enough.
3. Use this registry: [TimeZones-Windows-11-22H2.zip](https://github.com/modernuo/ModernUO/files/10939937/TimeZones-Windows-11-22H2.zip)

