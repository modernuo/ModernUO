using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the relics' registry across restarts (07.10.2026, relics20): every relic with its holder, its place, the guild a stolen
/// relic or a captured banner came from, when it changed hands, when its holder last called the Great Fair, and the serial of the
/// item that is it — the world save keeps the item itself. Also the warden standing, so a restart takes it off the island.
///
/// <para>
/// <b>Settled at the module's start, not resumed</b> (<see cref="Apply"/>): a relic in a lair goes back into hiding and its chest
/// goes, because the lair's companies and clock are not kept; every other relic stays where it was — a banner on its hall's floor,
/// a relic in a carrier's or a thief's pack — and the registry's next look puts right whatever moved.
/// </para>
/// </summary>
public sealed class BotRelicStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelicStore));

    private const int Shape = 1;

    private static BotRelicStore _store;

    public static void Configure() => _store ??= new BotRelicStore();

    public BotRelicStore() : base("BotRelics", 23)
    {
    }

    private static Serial _warden = Serial.MinusOne;

    private static int _read;

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        var all = BotRelics.All;

        writer.WriteEncodedInt(all.Count);

        for (var i = 0; i < all.Count; i++)
        {
            var r = all[i];

            writer.Write(r.Key);
            writer.Write(r.Name);
            writer.Write(r.Town);
            writer.Write(r.Of);
            writer.WriteEncodedInt(r.ItemId);
            writer.WriteEncodedInt(r.Hue);
            writer.Write((byte)r.Place);
            writer.Write(r.Guild);
            writer.Write(r.Bot);
            writer.Write(r.Former);
            writer.Write(r.Since);
            writer.Write(r.LastFair);
            writer.WriteEncodedInt(r.Hands);
            writer.Write(r.Item is { Deleted: false } item ? item.Serial : Serial.MinusOne);
        }

        writer.Write(BotRelicLair.Warden is { Deleted: false } warden ? warden.Serial : Serial.MinusOne);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning(
                "The saved relics are shape {Found} and this build reads {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotRelics/BotRelics.bin is deleted",
                shape,
                Shape
            );

            return;
        }

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var r = new BotRelic
            {
                Key = reader.ReadString(),
                Name = reader.ReadString(),
                Town = reader.ReadString(),
                Of = reader.ReadString(),
                ItemId = reader.ReadEncodedInt(),
                Hue = reader.ReadEncodedInt(),
                Place = (BotRelicPlace)reader.ReadByte(),
                Guild = reader.ReadString(),
                Bot = reader.ReadSerial(),
                Former = reader.ReadString(),
                Since = reader.ReadDateTime(),
                LastFair = reader.ReadDateTime(),
                Hands = reader.ReadEncodedInt()
            };

            var item = reader.ReadSerial();

            r.Item = World.FindItem(item) as BotRelicItem;
            BotRelics.Add(r);
        }

        _warden = reader.ReadSerial();
        _read = count;
    }

    /// <summary>What was read back, settled: the town relics seeded, the items linked, a lair's relic hidden, the warden away.</summary>
    public static void Apply()
    {
        BotRelics.Seed();
        BotRelics.Link();

        var hidden = 0;
        var all = BotRelics.All;

        for (var i = 0; i < all.Count; i++)
        {
            var r = all[i];

            if (r.Place != BotRelicPlace.Lair)
            {
                continue;
            }

            var chest = r.Item?.Parent as Container;

            BotRelics.Hide(r, "the shard restarted with it in a lair");

            if (chest is { Deleted: false } && chest.Items.Count == 0)
            {
                chest.Delete();
            }

            hidden++;
        }

        var gone = false;

        if (_warden != Serial.MinusOne && World.FindMobile(_warden) is BaseCreature { Deleted: false } warden)
        {
            warden.Delete();
            gone = true;
        }

        _warden = Serial.MinusOne;
        BotRelics.Recount();

        logger.Information(
            "The relics were read back: {Read} in the registry, {Count} now; {Hidden} in a lair hidden again, {Warden}",
            _read,
            all.Count,
            hidden,
            gone ? "its warden taken off the island" : "no warden standing"
        );

        _read = 0;
    }
}
