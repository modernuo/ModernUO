---
name: modernuo-configuration
description: >
  Trigger when adding server settings, reading config values, or working with modernuo.json or JsonConfig.
---

# ModernUO Configuration System

## When This Activates
- Adding new server settings
- Reading config values with `ServerConfiguration`
- Using `JsonConfig.Serialize/Deserialize<T>`
- Working with `modernuo.json` or custom config files

## Key Rules

1. **Read settings in `Configure()`** static method
2. **Use `GetOrUpdateSetting()`** when you want the setting created with a default if missing
3. **Use `GetSetting()`** for read-only access (won't write default to file)
4. **Custom config files use `JsonConfig.Serialize/Deserialize<T>()`**
5. **Config path base**: `Distribution/Configuration/`

## ServerConfiguration

### GetOrUpdateSetting (Creates Default If Missing)
```csharp
// In Configure() method:
var maxAccounts = ServerConfiguration.GetOrUpdateSetting("accountHandler.maxAccountsPerIP", 1);
var saveDelay = ServerConfiguration.GetOrUpdateSetting("autosave.saveDelay", TimeSpan.FromMinutes(5));
var enabled = ServerConfiguration.GetOrUpdateSetting("mySystem.enabled", true);
```

If the key doesn't exist in `modernuo.json`, it writes the default value and returns it.

### GetSetting (Read-Only)
```csharp
var statMax = ServerConfiguration.GetSetting("stats.statMax", 100);
var usePub45 = ServerConfiguration.GetSetting("stats.usePub45StatGain", false);
```

Returns the default if key is missing but does NOT write to the config file.

### Supported Types
```csharp
ServerConfiguration.GetSetting(string key, int defaultValue)
ServerConfiguration.GetSetting(string key, bool defaultValue)
ServerConfiguration.GetSetting(string key, double defaultValue)
ServerConfiguration.GetSetting(string key, TimeSpan defaultValue)
ServerConfiguration.GetSetting<T>(string key, T defaultValue) where T : struct, Enum
```

### SetSetting
```csharp
ServerConfiguration.SetSetting("mySystem.customValue", "42");
// Immediately persisted to modernuo.json
```

## Configuration Pattern

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

    // System logic uses _enabled, _maxItems, _cooldown...
}
```

## Custom Config Files with JsonConfig

For complex configuration that doesn't fit in `modernuo.json`:

```csharp
using Server.Json;

public static class MyComplexSystem
{
    private static MyConfig _config;
    private static readonly string ConfigPath =
        Path.Combine(Core.BaseDirectory, "Configuration/MySystem/config.json");

    public static void Configure()
    {
        _config = JsonConfig.Deserialize<MyConfig>(ConfigPath)
            ?? new MyConfig();
    }

    public static void SaveConfig()
    {
        JsonConfig.Serialize(ConfigPath, _config);
    }
}

public class MyConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxItems { get; set; } = 100;
    public List<string> BlockedNames { get; set; } = new();
    public Dictionary<string, int> Scores { get; set; } = new();
}
```

### JsonConfig Options
- Pretty-printed (WriteIndented = true)
- Comments allowed (ReadCommentHandling = Skip)
- Trailing commas allowed
- Null values omitted (WhenWritingNull)
- Enums serialized as strings (JsonStringEnumConverter)
- Built-in converters for: `ClientVersion`, `Guid`, `Map`, `Point3D`, `Rectangle3D`, `TimeSpan`, `IPEndPoint`, `Type`, `WorldLocation`, `TextDefinition`

## modernuo.json Structure

Location: `Distribution/Configuration/modernuo.json`

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
    "stats.statMax": "100",
    "timer.initialPoolCapacity": "1024",
    "mySystem.enabled": "True"
  }
}
```

All settings are stored as strings in the `settings` dictionary.

## Naming Convention for Keys

Use dot-separated hierarchical keys:
```
systemName.settingName
systemName.subSystem.settingName
```

Examples:
- `accountHandler.maxAccountsPerIP`
- `stats.statMax`
- `movement.delay.walkFoot`
- `autosave.saveDelay`

## Anti-Patterns

- **Reading config in constructors**: Use `Configure()` static method instead
- **Hardcoding values**: Use `ServerConfiguration.GetOrUpdateSetting()` for tunable values
- **Complex objects in modernuo.json**: Use `JsonConfig` with separate files instead
- **Not providing defaults**: Always pass a sensible default value

## Real Examples
- ServerConfiguration: `Projects/Server/Configuration/ServerConfiguration.cs`
- JsonConfig: `Projects/Server/Json/JsonConfig.cs`
- Config usage: `Projects/UOContent/Skills/SkillCheck.cs` (Configure method)
- Timer pool config: `Projects/Server/Timer/Timer.Pool.cs`
- Main config: `Distribution/Configuration/modernuo.json`

## See Also
- `dev-docs/configuration.md` - Complete configuration documentation
- `dev-docs/claude-skills/modernuo-era-expansion.md` - Expansion configuration
- `dev-docs/claude-skills/modernuo-events.md` - Configure() pattern
