# ModernUO Configuration System

This document covers ModernUO's configuration system, including ServerConfiguration for global settings, JsonConfig for custom config files, and best practices.

## Overview

ModernUO has two configuration mechanisms:
1. **ServerConfiguration**: Global key-value settings stored in `modernuo.json`
2. **JsonConfig**: Custom JSON configuration files for complex data structures

## ServerConfiguration

Defined in `Projects/Server/Configuration/ServerConfiguration.cs`.

### Reading Settings

#### GetSetting (Read-Only)

Returns the configured value or the default. Does NOT write the default to the config file.

```csharp
int statMax = ServerConfiguration.GetSetting("stats.statMax", 100);
bool enabled = ServerConfiguration.GetSetting("mySystem.enabled", true);
TimeSpan delay = ServerConfiguration.GetSetting("autosave.saveDelay", TimeSpan.FromMinutes(5));
double rate = ServerConfiguration.GetSetting("stats.gainChanceMultiplier", 1.0);
Expansion exp = ServerConfiguration.GetSetting("core.expansion", Expansion.ML);
```

Supported types:
- `int`
- `bool`
- `double`
- `TimeSpan`
- `T where T : struct, Enum`

#### GetOrUpdateSetting (Read-Write)

Returns the configured value. If the key doesn't exist, writes the default to the config file and returns it.

```csharp
int maxAccounts = ServerConfiguration.GetOrUpdateSetting("accountHandler.maxAccountsPerIP", 1);
bool autoCreate = ServerConfiguration.GetOrUpdateSetting("accountHandler.enableAutoAccountCreation", true);
int poolSize = ServerConfiguration.GetOrUpdateSetting("timer.initialPoolCapacity", 1024);
```

Use this when you want new settings to appear in `modernuo.json` automatically with sensible defaults.

#### SetSetting

Directly sets a value and immediately persists to disk:

```csharp
ServerConfiguration.SetSetting("mySystem.enabled", "true");
ServerConfiguration.SetSetting("mySystem.maxItems", "100");
```

### Configuration Pattern

Read settings in your `Configure()` static method:

```csharp
namespace Server.Custom;

public static class MySystem
{
    private static bool _enabled;
    private static int _maxItems;
    private static TimeSpan _cooldown;

    public static void Configure()
    {
        _enabled = ServerConfiguration.GetOrUpdateSetting("mySystem.enabled", true);
        _maxItems = ServerConfiguration.GetOrUpdateSetting("mySystem.maxItems", 100);
        _cooldown = ServerConfiguration.GetOrUpdateSetting("mySystem.cooldown", TimeSpan.FromMinutes(5));
    }

    public static void Initialize()
    {
        if (!_enabled)
            return;

        // System initialization that depends on config values
    }
}
```

### Key Naming Convention

Use dot-separated hierarchical keys:

```
systemName.settingName
systemName.subSystem.settingName
```

Examples from the codebase:
```
accountHandler.enableAutoAccountCreation
accountHandler.enablePlayerPasswordCommand
accountHandler.maxAccountsPerIP
autosave.enabled
autosave.saveDelay
world.savePath
world.useMultithreadedSaves
movement.delay.walkFoot
movement.delay.runFoot
stats.statMax
stats.gainChanceMultiplier
stats.primaryStatGainChance
stats.gainDelay
stats.petGainDelay
stats.usePub45StatGain
timer.initialPoolCapacity
timer.maxPoolCapacity
core.enableIdleCPU
```

### modernuo.json Structure

Located at `Distribution/Configuration/modernuo.json`:

```json
{
  "assemblyDirectories": ["./Assemblies"],
  "dataDirectories": ["C:\\Ultima Online Classic"],
  "listeners": ["0.0.0.0:2593"],
  "settings": {
    "accountHandler.enableAutoAccountCreation": "True",
    "accountHandler.maxAccountsPerIP": "1",
    "autosave.enabled": "True",
    "autosave.saveDelay": "00:05:00",
    "world.savePath": "Saves",
    "stats.statMax": "100"
  }
}
```

Key points:
- All settings are stored as strings in the `settings` dictionary
- Top-level fields (`assemblyDirectories`, `dataDirectories`, `listeners`) are structural
- `GetOrUpdateSetting` adds new entries to `settings` automatically

---

## JsonConfig

For complex configuration that doesn't fit in flat key-value pairs, use `JsonConfig`.

Defined in `Projects/Server/Json/JsonConfig.cs`.

### API

```csharp
// Deserialize from file (returns default if file doesn't exist)
T config = JsonConfig.Deserialize<T>(filePath);
T config = JsonConfig.Deserialize<T>(filePath, customOptions);

// Serialize to file (creates directory if needed)
JsonConfig.Serialize(filePath, config);
JsonConfig.Serialize(filePath, config, customOptions);

// Default options (available for customization)
JsonSerializerOptions options = JsonConfig.DefaultOptions;
```

### Default JSON Options

```csharp
WriteIndented = true                           // Pretty-printed
AllowTrailingCommas = true                     // Forgiving parser
ReadCommentHandling = JsonCommentHandling.Skip // Comments allowed
DefaultIgnoreCondition = WhenWritingNull       // Null values omitted
Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
```

### Built-in Converters

JsonConfig includes converters for ModernUO types:
- `ClientVersion`
- `Guid`
- `Map`
- `Point3D`
- `Rectangle3D`
- `TimeSpan`
- `IPEndPoint`
- `Type`
- `WorldLocation`
- `TextDefinition`
- All enums (as strings via `JsonStringEnumConverter`)

### Custom Config File Pattern

```csharp
using Server.Json;

namespace Server.Custom;

public class MySystemConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxItems { get; set; } = 100;
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(5);
    public List<string> BlockedNames { get; set; } = new();
    public Dictionary<string, int> Scores { get; set; } = new();
}

public static class MySystem
{
    private static MySystemConfig _config;
    private static readonly string ConfigPath =
        Path.Combine(Core.BaseDirectory, "Configuration/MySystem/config.json");

    public static void Configure()
    {
        _config = JsonConfig.Deserialize<MySystemConfig>(ConfigPath);

        if (_config == null)
        {
            _config = new MySystemConfig();
            JsonConfig.Serialize(ConfigPath, _config);
        }
    }

    public static void SaveConfig()
    {
        JsonConfig.Serialize(ConfigPath, _config);
    }
}
```

This creates a config file like:
```json
{
  "Enabled": true,
  "MaxItems": 100,
  "Cooldown": "00:05:00",
  "BlockedNames": [],
  "Scores": {}
}
```

### Custom Converters

Add custom converters via `JsonConfig.GetOptions()`:

```csharp
var options = JsonConfig.GetOptions(new MyCustomConverterFactory());
var data = JsonConfig.Deserialize<MyType>(path, options);
```

---

## Configuration File Locations

| File | Purpose |
|---|---|
| `Distribution/Configuration/modernuo.json` | Main server settings |
| `Distribution/Configuration/expansion.json` | Target expansion |
| `Distribution/Data/expansions.json` | Expansion metadata |
| `Distribution/Configuration/` | Custom config directory |

Custom config files should be placed under `Distribution/Configuration/` in a subdirectory named after your system.

## Best Practices

1. **Read in `Configure()`** -- called before `Initialize()`, ensures settings are available early
2. **Use `GetOrUpdateSetting`** for new features -- ensures defaults appear in config file
3. **Use `GetSetting`** for optional/advanced settings -- doesn't clutter config file
4. **Use JsonConfig for complex data** -- lists, dictionaries, nested objects
5. **Provide sensible defaults** -- system should work without manual configuration
6. **Use era-aware defaults** -- `Core.LBR ? 125 : 100` for values that vary by expansion
7. **Document key names** -- use clear hierarchical naming

## Key File References

| File | Description |
|---|---|
| `Projects/Server/Configuration/ServerConfiguration.cs` | ServerConfiguration class |
| `Projects/Server/Json/JsonConfig.cs` | JsonConfig utility |
| `Distribution/Configuration/modernuo.json` | Main config file |
| `Projects/UOContent/Skills/SkillCheck.cs` | Config usage example |
| `Projects/Server/Timer/Timer.Pool.cs` | Config usage example |
