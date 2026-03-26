# Persistence Migration

## Overview

RunUO systems that persist custom data use `EventSink.WorldSave`/`EventSink.WorldLoad` with manual `BinaryFileWriter`/`BinaryFileReader`. ModernUO replaces this with `GenericPersistence` (for system state) and `GenericEntityPersistence<T>` (for custom entity collections). The persistence framework handles save/load lifecycle, file management, and integration with ModernUO's multi-threaded save pipeline automatically.

## RunUO Pattern

```csharp
using System;
using System.IO;
using Server;

namespace Server.Custom
{
    public class JailSystem
    {
        private static Dictionary<Mobile, JailRecord> m_Records = new Dictionary<Mobile, JailRecord>();

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }

        private static void Load()
        {
            string filePath = Path.Combine("Saves/Custom", "JailSystem.bin");

            if (!File.Exists(filePath))
                return;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(fs);
                int version = reader.ReadInt32();

                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    Mobile m = World.FindMobile(reader.ReadInt32());
                    string reason = reader.ReadString();
                    DateTime releaseDate = new DateTime(reader.ReadInt64());

                    if (m != null)
                        m_Records[m] = new JailRecord(reason, releaseDate);
                }
            }
        }

        private static void Save(WorldSaveEventArgs e)
        {
            string dirPath = "Saves/Custom";
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string filePath = Path.Combine(dirPath, "JailSystem.bin");

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                BinaryWriter writer = new BinaryWriter(fs);
                writer.Write((int)0); // version
                writer.Write(m_Records.Count);

                foreach (var kvp in m_Records)
                {
                    writer.Write(kvp.Key.Serial.Value);
                    writer.Write(kvp.Value.Reason);
                    writer.Write(kvp.Value.ReleaseDate.Ticks);
                }
            }
        }
    }
}
```

## ModernUO Equivalent (GenericPersistence)

```csharp
using Server;
using Server.Serialization;

namespace Server.Custom;

public class JailSystem : GenericPersistence
{
    private static JailSystem _instance;
    private static Dictionary<Mobile, JailRecord> _records = new();

    public static void Configure()
    {
        _instance = new JailSystem();
    }

    public JailSystem() : base("JailSystem", 10) { }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(_records.Count);

        foreach (var (mobile, record) in _records)
        {
            writer.Write(mobile);
            writer.Write(record.Reason);
            writer.Write(record.ReleaseDate);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var reason = reader.ReadString();
            var releaseDate = reader.ReadDateTime();

            if (mobile != null)
                _records[mobile] = new JailRecord(reason, releaseDate);
        }
    }

    // Public API for the system
    public static void JailPlayer(Mobile m, string reason, DateTime releaseDate)
    {
        _records[m] = new JailRecord(reason, releaseDate);
        _instance.MarkDirty();
    }

    public static bool IsJailed(Mobile m) => _records.ContainsKey(m);

    public static void Release(Mobile m)
    {
        _records.Remove(m);
        _instance.MarkDirty();
    }
}
```

## When to Use Which

| Scenario | Use |
|---|---|
| System state (jail records, virtue data, faction data, scores) | `GenericPersistence` |
| Custom entity collections with their own serial ranges | `GenericEntityPersistence<T>` |
| Item/Mobile subclasses (normal serialization) | `[SerializationGenerator]` — not persistence |

Most RunUO `EventSink.WorldSave` patterns should convert to `GenericPersistence`.

## Migration Mapping Table

| RunUO | ModernUO | Notes |
|---|---|---|
| `EventSink.WorldSave += Save` | `class MySystem : GenericPersistence` | Subclass instead |
| `EventSink.WorldLoad += Load` | `override Deserialize(IGenericReader)` | Method on class |
| Manual `Save(WorldSaveEventArgs)` | `override Serialize(IGenericWriter)` | Method on class |
| Manual `Load()` | `override Deserialize(IGenericReader)` | Method on class |
| `new BinaryFileWriter(path, true)` | Handled by framework | No file management |
| `new BinaryFileReader(new FileStream(...))` | Handled by framework | No file management |
| `writer.Write((int)0)` version | `writer.WriteEncodedInt(0)` | Encoded preferred |
| `reader.ReadInt()` count | `reader.ReadEncodedInt()` | Encoded preferred |
| `writer.Write(mobile.Serial.Value)` | `writer.Write(mobile)` | Write entity directly |
| `World.FindMobile(reader.ReadInt32())` | `reader.ReadEntity<Mobile>()` | Generic method |
| `World.FindItem(reader.ReadInt32())` | `reader.ReadEntity<Item>()` | Generic method |
| `Directory.CreateDirectory(...)` | Handled by framework | Automatic |
| `File.Exists(path)` check | Handled by framework | Automatic |

## Step-by-Step Conversion

### Step 1: Create Persistence Class
```csharp
public class MySystem : GenericPersistence
{
    private static MySystem _instance;

    public static void Configure()
    {
        _instance = new MySystem();
    }

    public MySystem() : base("MySystem", 10) { }
    // "MySystem" = save file name
    // 10 = priority (lower = saved first)
}
```

### Step 2: Move Save Logic to Serialize
```csharp
public override void Serialize(IGenericWriter writer)
{
    writer.WriteEncodedInt(0); // version

    // Convert BinaryWriter calls to IGenericWriter calls
    writer.WriteEncodedInt(_data.Count);
    foreach (var (key, value) in _data)
    {
        writer.Write(key);    // Can write Mobile/Item directly
        writer.Write(value);
    }
}
```

### Step 3: Move Load Logic to Deserialize
```csharp
public override void Deserialize(IGenericReader reader)
{
    var version = reader.ReadEncodedInt();
    var count = reader.ReadEncodedInt();

    for (var i = 0; i < count; i++)
    {
        var key = reader.ReadEntity<Mobile>();  // Not World.FindMobile()
        var value = reader.ReadInt();

        if (key != null)
            _data[key] = value;
    }
}
```

### Step 4: Remove EventSink Subscriptions
Delete the `EventSink.WorldSave += ...` and `EventSink.WorldLoad += ...` lines.

### Step 5: Add MarkDirty() Calls
Whenever data changes, call `_instance.MarkDirty()` to flag the system for saving:
```csharp
public static void AddRecord(Mobile m, string data)
{
    _records[m] = data;
    _instance.MarkDirty();  // Required!
}
```

### Step 6: Remove File Management Code
Delete all `Directory.CreateDirectory`, `File.Exists`, `FileStream`, path construction. The framework handles this.

## IGenericWriter vs BinaryWriter

| BinaryWriter (RunUO) | IGenericWriter (ModernUO) |
|---|---|
| `writer.Write((int)value)` | `writer.Write(value)` or `writer.WriteEncodedInt(value)` |
| `writer.Write((string)value)` | `writer.Write(value)` |
| `writer.Write((bool)value)` | `writer.Write(value)` |
| `writer.Write(mobile.Serial.Value)` | `writer.Write(mobile)` |
| `writer.Write(item.Serial.Value)` | `writer.Write(item)` |
| `writer.Write(dateTime.Ticks)` | `writer.Write(dateTime)` |
| No encoded int | `writer.WriteEncodedInt(value)` — variable-length, saves space |

## IGenericReader vs BinaryReader

| BinaryReader (RunUO) | IGenericReader (ModernUO) |
|---|---|
| `reader.ReadInt32()` | `reader.ReadInt()` or `reader.ReadEncodedInt()` |
| `reader.ReadString()` | `reader.ReadString()` |
| `reader.ReadBoolean()` | `reader.ReadBool()` |
| `World.FindMobile(reader.ReadInt32())` | `reader.ReadEntity<Mobile>()` |
| `World.FindItem(reader.ReadInt32())` | `reader.ReadEntity<Item>()` |
| `new DateTime(reader.ReadInt64())` | `reader.ReadDateTime()` |

## Before/After: Complete System

**RunUO:**
```csharp
namespace Server.Custom
{
    public class VirtueSystem
    {
        private static Dictionary<Mobile, int> m_Points = new Dictionary<Mobile, int>();

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }

        private static void Load()
        {
            string path = Path.Combine("Saves/Custom", "Virtue.bin");
            if (!File.Exists(path)) return;

            using var fs = new FileStream(path, FileMode.Open);
            var reader = new BinaryReader(fs);

            int version = reader.ReadInt32();
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                Mobile m = World.FindMobile(reader.ReadInt32());
                int pts = reader.ReadInt32();
                if (m != null)
                    m_Points[m] = pts;
            }
        }

        private static void Save(WorldSaveEventArgs e)
        {
            string dir = "Saves/Custom";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var fs = new FileStream(Path.Combine(dir, "Virtue.bin"), FileMode.Create);
            var writer = new BinaryWriter(fs);

            writer.Write(0); // version
            writer.Write(m_Points.Count);
            foreach (var kvp in m_Points)
            {
                writer.Write(kvp.Key.Serial.Value);
                writer.Write(kvp.Value);
            }
        }

        public static void AddPoints(Mobile m, int points)
        {
            if (!m_Points.ContainsKey(m))
                m_Points[m] = 0;
            m_Points[m] += points;
        }
    }
}
```

**ModernUO:**
```csharp
namespace Server.Custom;

public class VirtueSystem : GenericPersistence
{
    private static VirtueSystem _instance;
    private static readonly Dictionary<Mobile, int> _points = new();

    public static void Configure()
    {
        _instance = new VirtueSystem();
    }

    public VirtueSystem() : base("VirtueSystem", 10) { }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(_points.Count);
        foreach (var (mobile, pts) in _points)
        {
            writer.Write(mobile);
            writer.Write(pts);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var pts = reader.ReadInt();
            if (mobile != null)
                _points[mobile] = pts;
        }
    }

    public static void AddPoints(Mobile m, int points)
    {
        _points.TryGetValue(m, out var current);
        _points[m] = current + points;
        _instance.MarkDirty();
    }
}
```

## Edge Cases & Gotchas

### 1. MarkDirty() Is Required
Without `MarkDirty()`, changes won't be saved. Call it whenever your persisted data changes.

### 2. GenericPersistence Constructor Name
The first argument to the base constructor is the save file name. It must be unique across all `GenericPersistence` instances.

### 3. Priority Argument
The second argument is save priority. Lower numbers save first. Use 10 for most systems.

### 4. Don't Mix EventSink.WorldSave with GenericPersistence
Don't subscribe to `EventSink.WorldSave` for data that `GenericPersistence` manages. The framework handles the lifecycle.

### 5. ReadEntity<T>() Returns Null for Deleted Entities
Unlike `World.FindMobile()`, `ReadEntity<T>()` will return null if the entity was deleted. Always null-check.

## See Also

- `dev-docs/serialization.md` — Serialization system overview
- `07-commands-events.md` — EventSink migration
- `01-foundation-changes.md` — Foundation changes
