---
name: migrate-persistence
description: >
  Trigger: when converting RunUO EventSink.WorldSave/WorldLoad manual binary persistence to ModernUO GenericPersistence.
  Covers: GenericPersistence subclassing, IGenericWriter/IGenericReader, MarkDirty pattern.
---

# RunUO -> ModernUO Persistence Migration

## When This Activates
- Converting `EventSink.WorldSave`/`EventSink.WorldLoad` patterns
- Converting manual `BinaryFileWriter`/`BinaryFileReader` persistence
- Systems that save custom data outside of Item/Mobile serialization

## Conversion Steps
1. Create class inheriting `GenericPersistence`: `class MySystem : GenericPersistence`
2. Add static instance + `Configure()`: `_instance = new MySystem();`
3. Call `base("SaveName", 10)` in constructor
4. Move save logic to `override Serialize(IGenericWriter writer)`
5. Move load logic to `override Deserialize(IGenericReader reader)`
6. Remove `EventSink.WorldSave`/`WorldLoad` subscriptions
7. Remove all file management code (Directory.Create, File.Exists, FileStream)
8. Add `_instance.MarkDirty()` wherever data changes
9. Replace `writer.Write(mobile.Serial.Value)` -> `writer.Write(mobile)`
10. Replace `World.FindMobile(reader.ReadInt32())` -> `reader.ReadEntity<Mobile>()`

## Template
```csharp
public class MySystem : GenericPersistence
{
    private static MySystem _instance;
    public static void Configure() => _instance = new MySystem();
    public MySystem() : base("MySystem", 10) { }
    public override void Serialize(IGenericWriter writer) { /* save */ }
    public override void Deserialize(IGenericReader reader) { /* load */ }
}
```

## See Also
- `dev-docs/runuo-migration-docs/08-persistence.md` -- detailed migration reference with before/after
- `dev-docs/serialization.md` -- ModernUO serialization system (IGenericWriter/IGenericReader)
- `dev-docs/claude-skills/modernuo-serialization.md` -- ModernUO serialization skill
