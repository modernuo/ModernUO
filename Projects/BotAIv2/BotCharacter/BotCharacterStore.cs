using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps every bot's character across restarts: its traits, its kills and veteran's title, the counts and the three tales of
/// its biography, and its ties. See <see cref="BotLife"/>.
///
/// <para>
/// <b>By name, never by serial.</b> A bot is raised afresh at every boot with a new serial; the name is what
/// <c>BotProgress</c> carries over, and it is what a tie names the other bot by. A record for a name nobody bears any more is
/// kept (a bot resting through the boot is still somebody's friend) and costs a few dozen bytes. A shape this build cannot read
/// is dropped whole rather than guessed at, and then every nature is drawn again from the names, which gives the same natures.
/// </para>
/// </summary>
public sealed class BotCharacterStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCharacterStore));

    private const int Shape = 1;

    private static BotCharacterStore _store;

    public static void Configure() => _store ??= new BotCharacterStore();

    public BotCharacterStore() : base("BotCharacter", 20)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        writer.WriteEncodedInt(BotLives.Count);

        foreach (var life in BotLives.All)
        {
            writer.Write(life.Name);
            writer.WriteEncodedInt((int)life.Traits);
            writer.Write(life.Kills);
            writer.WriteEncodedInt(life.Deaths);
            writer.WriteEncodedInt(life.SavedTimes);
            writer.WriteEncodedInt(life.RaisedTimes);
            writer.WriteEncodedInt(life.Robbed);
            writer.WriteEncodedInt(life.WarKills);
            writer.WriteEncodedInt(life.DuelsWon);
            writer.WriteEncodedInt(life.DuelsLost);
            writer.WriteEncodedInt(life.BestSale);
            writer.Write(life.Veteran);
            writer.Write(life.VeteranAt);

            writer.WriteEncodedInt(life.Tales.Count);

            for (var i = 0; i < life.Tales.Count; i++)
            {
                var tale = life.Tales[i];

                writer.Write(tale.Kind);
                writer.Write(tale.Other);
                writer.Write(tale.Text);
                writer.Write(tale.Weight);
                writer.WriteEncodedInt(tale.Number);
                writer.Write(tale.At);
            }

            writer.WriteEncodedInt(life.Ties.Count);

            for (var i = 0; i < life.Ties.Count; i++)
            {
                var tie = life.Ties[i];

                writer.Write(tie.Name);
                writer.Write(tie.Regard);
                writer.WriteEncodedInt(tie.Saved);
                writer.WriteEncodedInt(tie.Wronged);
                writer.WriteEncodedInt(tie.Beside);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved characters are shape {Found} and this build reads {Wanted}; every nature is drawn again from the names, and no story or tie is kept", shape, Shape);

            return;
        }

        var count = reader.ReadEncodedInt();
        var ties = 0;
        var tales = 0;
        var veterans = 0;

        for (var n = 0; n < count; n++)
        {
            var life = new BotLife(reader.ReadString())
            {
                Traits = (BotTrait)reader.ReadEncodedInt(),
                Drawn = true,
                Kills = reader.ReadDouble(),
                Deaths = reader.ReadEncodedInt(),
                SavedTimes = reader.ReadEncodedInt(),
                RaisedTimes = reader.ReadEncodedInt(),
                Robbed = reader.ReadEncodedInt(),
                WarKills = reader.ReadEncodedInt(),
                DuelsWon = reader.ReadEncodedInt(),
                DuelsLost = reader.ReadEncodedInt(),
                BestSale = reader.ReadEncodedInt(),
                Veteran = reader.ReadBool(),
                VeteranAt = reader.ReadDateTime()
            };

            var taleCount = reader.ReadEncodedInt();

            for (var i = 0; i < taleCount; i++)
            {
                life.Tales.Add(
                    new BotTale
                    {
                        Kind = reader.ReadString(),
                        Other = reader.ReadString(),
                        Text = reader.ReadString(),
                        Weight = reader.ReadDouble(),
                        Number = reader.ReadEncodedInt(),
                        At = reader.ReadDateTime()
                    }
                );
            }

            var tieCount = reader.ReadEncodedInt();

            for (var i = 0; i < tieCount; i++)
            {
                life.Ties.Add(
                    new BotTie
                    {
                        Name = reader.ReadString(),
                        Regard = reader.ReadDouble(),
                        Saved = reader.ReadEncodedInt(),
                        Wronged = reader.ReadEncodedInt(),
                        Beside = reader.ReadEncodedInt()
                    }
                );
            }

            if (string.IsNullOrEmpty(life.Name))
            {
                continue;
            }

            life.Rewound();
            BotLives.Put(life);

            ties += life.Ties.Count;
            tales += life.Tales.Count;
            veterans += life.Veteran ? 1 : 0;
        }

        logger.Information(
            "Characters read back: {Count} bots with their natures, {Tales} tales, {Ties} ties and {Veterans} veterans",
            count,
            tales,
            ties,
            veterans
        );
    }
}
