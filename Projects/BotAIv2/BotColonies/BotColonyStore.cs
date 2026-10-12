using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the guilds' colonies across restarts: whose, on which island, where the store stands and what the colony has brought in.
///
/// <para>
/// <b>A file of its own</b>, as <see cref="BotSeizureStore"/> is beside the war ledger, so a build without colonies ignores it. The
/// store is a chest in the world and is saved by the engine; this keeps its serial and finds it again after the load
/// (<see cref="BotColonies"/>' first look). Who is on the land is not kept: it is looked at again, and a shift's clock starts again
/// at the boot. Registered by the engine's own call of every <c>Configure</c>, after <see cref="BotCore.Configure"/>, so that
/// <c>BotCore</c> is not touched.
/// </para>
/// </summary>
public sealed class BotColonyStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotColonyStore));

    private const int Shape = 1;

    private const int Oldest = 1;

    private static BotColonyStore _store;

    /// <summary>After <c>BotCore.Configure</c> (priority 40), which reads whether the bots are on at all.</summary>
    [CallPriority(41)]
    public static void Configure()
    {
        if (BotCore.Enabled)
        {
            _store ??= new BotColonyStore();
        }
    }

    public BotColonyStore() : base("BotColonies", 16)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        var colonies = BotColonies.ForSave();

        writer.WriteEncodedInt(colonies.Count);

        for (var i = 0; i < colonies.Count; i++)
        {
            var c = colonies[i];

            writer.Write(c.Guild ?? "");
            writer.Write(c.Island ?? "");
            writer.WriteEncodedInt(c.Map?.MapID ?? 0);
            writer.Write(c.Site.X);
            writer.Write(c.Site.Y);
            writer.Write(c.Site.Z);
            writer.Write((uint)c.StoreSerial);
            writer.Write(c.FoundedAt.Ticks);
            writer.Write(c.Claimed);
            writer.Write(c.OreIn);
            writer.Write(c.IngotsIn);
            writer.Write(c.LogsIn);
            writer.Write(c.ColonistMs);
            writer.Write(c.Runs);
            writer.Write(c.CargoUnits);
            writer.Write(c.CargoGold);
            writer.Write(c.ChestGold);
            writer.Write(c.CrewGold);
            writer.Write(c.CaptainGold);
            writer.Write(c.Plundered);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            logger.Warning(
                "The saved colonies are shape {Found} and this build reads {Oldest} to {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotColonies/BotColonies.bin is deleted",
                shape,
                Oldest,
                Shape
            );

            return;
        }

        var many = reader.ReadEncodedInt();

        for (var i = 0; i < many; i++)
        {
            var colony = new BotColony
            {
                Guild = reader.ReadString(),
                Island = reader.ReadString(),
                Map = Map.Maps[Math.Clamp(reader.ReadEncodedInt(), 0, Map.Maps.Length - 1)]
            };

            var x = reader.ReadInt();
            var y = reader.ReadInt();
            var z = reader.ReadInt();

            colony.Site = new Point3D(x, y, z);
            colony.StoreSerial = (Serial)reader.ReadUInt();
            colony.FoundedAt = new DateTime(reader.ReadLong(), DateTimeKind.Utc);
            colony.Claimed = reader.ReadBool();
            colony.OreIn = reader.ReadLong();
            colony.IngotsIn = reader.ReadLong();
            colony.LogsIn = reader.ReadLong();
            colony.ColonistMs = reader.ReadLong();
            colony.Runs = reader.ReadLong();
            colony.CargoUnits = reader.ReadLong();
            colony.CargoGold = reader.ReadLong();
            colony.ChestGold = reader.ReadLong();
            colony.CrewGold = reader.ReadLong();
            colony.CaptainGold = reader.ReadLong();
            colony.Plundered = reader.ReadLong();
            colony.Last = "read back from the save";

            if (!string.IsNullOrEmpty(colony.Guild))
            {
                BotColonies.Restore(colony);
            }
        }

        if (many > 0)
        {
            logger.Information("{Many} colonies read back off the save", many);
        }
    }
}
