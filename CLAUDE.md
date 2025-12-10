# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ModernUO is an Ultima Online Server Emulator built with .NET 10.0 and C# 14. It's a modernized fork of the RunUO/ServUO lineage, focusing on performance, clean code, and modern .NET features.

## Build & Run Commands

```bash
# Build for development (debug)
dotnet build

# Build for production
./publish.cmd release win x64     # Windows
./publish.sh release linux x64    # Linux/macOS

# Run tests
dotnet test

# Run a single test file
dotnet test --filter "FullyQualifiedName~ClassName"

# Run the server (after publishing)
./Distribution/ModernUO.exe       # Windows
dotnet ./Distribution/ModernUO.dll  # Cross-platform
```

## Project Architecture

### Solution Structure
- **Application** - Entry point (`ModernUO.exe`), minimal bootstrapper that calls `Core.Setup()`
- **Server** - Core engine: networking, serialization, world management, base entity types
- **UOContent** - Game content: items, mobiles, engines, gumps, spells, skills
- **Logger** - Serilog-based logging infrastructure
- **Server.Tests / UOContent.Tests** - xUnit test projects

### Key Architectural Patterns

**Entity System**: `Mobile` and `Item` are the base classes for all game objects in `Projects/Server/Mobiles/Mobile.cs` and `Projects/Server/Items/Item.cs`. Both inherit from `ISerializable` and use serial numbers for identification.

**Serialization**: Uses source-generated serialization via `ModernUO.Serialization.Generator`. Classes marked with `[SerializationGenerator]` get automatic `Serialize`/`Deserialize` methods. Migration schemas are in `Projects/*/Migrations/*.v*.json`.

**Event Loop**: Single-threaded game loop in `Core.RunEventLoop()` (`Projects/Server/Main.cs`). Processes mobile/item delta queues, timers, and network slices.

**Timer System**: Timer wheel implementation in `Projects/Server/Timer/`. Use `Timer.DelayCall()` for scheduling. Timers are pooled for performance.

**Networking**: Packet-based UDP/TCP networking in `Projects/Server/Network/`. `NetState` represents a client connection. Outgoing packets are in `Packets/Outgoing*.cs`.

**Gump System**: UI system in `Projects/UOContent/Gumps/`. Legacy gumps use `GumpEntry` classes; modern gumps use `DynamicGump` with `GumpLayoutBuilder`.

**Configuration**: JSON-based configuration in `Distribution/Data/`. Server settings load via `ServerConfiguration` class. Assembly loading configured via `Distribution/Data/assemblies.json`.

**Regions**: Geographic zones defined in `Distribution/Data/regions.json`, loaded by `RegionJsonSerializer`. Base `Region` class in `Projects/Server/Regions/`.

### Assembly Loading
UOContent.dll is loaded dynamically at runtime. The Server project references UOContent but doesn't copy it - it's loaded from `Distribution/Assemblies/`. Methods are invoked via reflection using `AssemblyHandler.Invoke("Configure")` and `AssemblyHandler.Invoke("Initialize")`.

### World Persistence
World save/load handled in `Projects/Server/World/World.cs`. Uses memory-mapped files and background serialization threads. Entities tracked via `GenericEntityPersistence`.

## Code Conventions

- Namespace is `Server` for both Server and UOContent projects
- Uses file-scoped namespaces (`namespace Server;`)
- Prefers `var` for local variables
- Serializable entities need: `[SerializationGenerator]` attribute, constructor taking `Serial`, and serialization version
- Use `Utility.Random*` methods for randomness (thread-safe, mockable)
- Configuration values accessed via `ServerConfiguration.GetOrUpdateSetting<T>()`

## Testing
Tests use xUnit. Server.Tests and UOContent.Tests both copy `Distribution/Data/` to output for test runs. Mock `Core._now` and `Core._tickCount` for time-sensitive tests.
