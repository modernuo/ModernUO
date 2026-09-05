using System;
using System.Collections.Generic;
using System.IO;
using Server.Logging;

namespace Server.Commands;

/// <summary>
/// Serializes each entity twice into scratch buffers and reports the types whose bytes differ
/// between the two passes. A stable entity is a prerequisite for delta saves: if its bytes move
/// with no mutation, hash-compare rewrites it on every save and dirty tracking can never skip it.
/// </summary>
public static class SaveStability
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SaveStability));

    public readonly record struct SaveStabilityReport(
        int Checked,
        int Unstable,
        Dictionary<Type, int> UnstableByType,
        List<(Type Type, Serial Serial, int Offset)> Examples
    );

    public static void Configure()
    {
        CommandSystem.Register("SaveStability", AccessLevel.Administrator, SaveStability_OnCommand);
    }

    [Usage("SaveStability [items|mobiles|guilds|all]")]
    [Description("Serializes every entity twice and reports types whose bytes change between passes.")]
    private static void SaveStability_OnCommand(CommandEventArgs e)
    {
        var scope = e.Length > 0 ? e.GetString(0) : "all";
        var from = e.Mobile;

        IEnumerable<ISerializable> entities = scope.ToLowerInvariant() switch
        {
            "items"   => World.Items.Values,
            "mobiles" => World.Mobiles.Values,
            "guilds"  => World.Guilds.Values,
            _         => AllEntities()
        };

        from.SendMessage("Checking save stability. This walks the whole world once.");

        var report = Check(entities);

        from.SendMessage($"Checked {report.Checked} entities, {report.Unstable} unstable.");

        foreach (var (type, count) in report.UnstableByType)
        {
            from.SendMessage($"  {type.Name}: {count}");
            logger.Warning("Unstable serialization: {Type} ({Count} entities)", type.FullName, count);
        }

        foreach (var (type, serial, offset) in report.Examples)
        {
            from.SendMessage($"  e.g. {type.Name} {serial} differs at byte {offset}");
        }
    }

    private static IEnumerable<ISerializable> AllEntities()
    {
        foreach (var item in World.Items.Values)
        {
            yield return item;
        }

        foreach (var mobile in World.Mobiles.Values)
        {
            yield return mobile;
        }

        foreach (var guild in World.Guilds.Values)
        {
            yield return guild;
        }
    }

    public static SaveStabilityReport Check(IEnumerable<ISerializable> entities, int maxExamples = 5)
    {
        var first = new BufferWriter(true);
        var second = new BufferWriter(true);
        var unstableByType = new Dictionary<Type, int>();
        var examples = new List<(Type, Serial, int)>();
        var checkedCount = 0;
        var unstable = 0;

        foreach (var entity in entities)
        {
            if (entity.Deleted)
            {
                continue;
            }

            checkedCount++;

            first.Seek(0, SeekOrigin.Begin);
            entity.Serialize(first);
            second.Seek(0, SeekOrigin.Begin);
            entity.Serialize(second);

            var a = first.Buffer.AsSpan(0, (int)first.Position);
            var b = second.Buffer.AsSpan(0, (int)second.Position);

            var offset = FirstDifference(a, b);
            if (offset < 0)
            {
                continue;
            }

            unstable++;
            var type = entity.GetType();
            unstableByType[type] = unstableByType.GetValueOrDefault(type) + 1;

            if (examples.Count < maxExamples)
            {
                examples.Add((type, entity.Serial, offset));
            }
        }

        first.Close();
        second.Close();

        return new SaveStabilityReport(checkedCount, unstable, unstableByType, examples);
    }

    private static int FirstDifference(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var common = Math.Min(a.Length, b.Length);
        var prefix = a[..common].CommonPrefixLength(b[..common]);

        if (prefix < common)
        {
            return prefix;
        }

        return a.Length == b.Length ? -1 : common;
    }
}
