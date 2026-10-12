using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps who owns which square of the island across restarts.
///
/// <para>
/// <b>Patrick's order of 10.09.2026: "claims must be written down, because the bots pay for them".</b> Every other
/// board on this shard is cleared on a world reload and that is right — a market stall, a want, an errand
/// and a squad are all about a moment. Ground is not. A guild spends four free claims and then five thousand
/// gold a square, stands three of its members in a field for five minutes to earn each one, and until this
/// existed the whole register was thrown away by the next rebuild. That is not a board going stale, it is
/// the shard taking something a guild bought.
/// </para>
///
/// <para>
/// <b>Names and numbers only, exactly as <see cref="BotQuadStore"/> is.</b> A holding is a facet id, two
/// square coordinates, a guild's name and whether it was paid for. Nothing here points at a mobile, an item
/// or a serial, so it outlives a world the rest of the shard cannot.
/// </para>
///
/// <para>
/// <b>A claim still running is written too, and it comes back with its clock reset.</b> The money went when
/// the claim was declared, so a restart in the middle of a muster would otherwise charge a guild five
/// thousand gold for nothing. Restoring the remaining seconds would be worse than useless — the population
/// is rebuilt scattered and thirty seconds is not enough for anybody to walk anywhere — so what is returned
/// is the attempt, whole. The tick it began at is deliberately not written: a tick count means nothing in
/// the next process, and on some hosts it is the machine's uptime.
/// </para>
/// </summary>
public sealed class BotClaimStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotClaimStore));

    /// <summary>
    /// The shape of what is written below. 2 (06.10.2026, build 408): each claim being made carries the price held for it
    /// (<c>BotClaim.Bid.Escrow</c>), and the failed musters still resting follow the claims, so neither a held price nor a
    /// square's rest is lost to a restart. A shape-1 save reads as claims that hold nothing — they paid when declared.
    /// </summary>
    private const int Shape = 2;

    private const int Oldest = 1;

    private static BotClaimStore _store;

    /// <summary>Registered from <c>BotCore.Configure</c>: a persistence must exist before the world loads.</summary>
    public static void Configure() => _store ??= new BotClaimStore();

    /// <summary>The priority is only an ordering among save files; nothing else depends on it.</summary>
    public BotClaimStore() : base("BotClaims", 14)
    {
    }

    /// <summary>Squares read back from the last save, for the start-up line to report.</summary>
    public static int Restored { get; private set; }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);

        var held = 0;

        foreach (var _ in BotClaim.Owned())
        {
            held++;
        }

        writer.WriteEncodedInt(held);

        foreach (var (key, guild, bought) in BotClaim.Owned())
        {
            writer.WriteEncodedInt(key.Map);
            writer.WriteEncodedInt(key.X);
            writer.WriteEncodedInt(key.Y);
            writer.Write(guild);
            writer.Write(bought);
        }

        var making = 0;

        foreach (var _ in BotClaim.Bids)
        {
            making++;
        }

        var owed = BotClaim.Owed;

        writer.WriteEncodedInt(making + owed.Count);

        foreach (var bid in BotClaim.Bids)
        {
            writer.WriteEncodedInt(bid.Key.Map);
            writer.WriteEncodedInt(bid.Key.X);
            writer.WriteEncodedInt(bid.Key.Y);
            writer.Write(bid.Guild);
            writer.WriteEncodedInt((int)bid.Want);
            writer.Write(bid.From ?? "");
            writer.WriteEncodedInt(bid.Escrow);
        }

        // A held price still owed to a chest from the last boot is written as a claim, and is owed again on the next.
        for (var i = 0; i < owed.Count; i++)
        {
            writer.WriteEncodedInt(owed[i].Key.Map);
            writer.WriteEncodedInt(owed[i].Key.X);
            writer.WriteEncodedInt(owed[i].Key.Y);
            writer.Write(owed[i].Guild);
            writer.WriteEncodedInt((int)BotClaim.Kind.Settle);
            writer.Write("");
            writer.WriteEncodedInt(owed[i].Coin);
        }

        var rests = 0;

        foreach (var _ in BotClaim.Rests())
        {
            rests++;
        }

        writer.WriteEncodedInt(rests);

        foreach (var (guild, key, failures, ago) in BotClaim.Rests())
        {
            writer.Write(guild);
            writer.WriteEncodedInt(key.Map);
            writer.WriteEncodedInt(key.X);
            writer.WriteEncodedInt(key.Y);
            writer.WriteEncodedInt(failures);
            writer.Write(ago);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            logger.Warning(
                "The saved claims are shape {Found} and this build reads {Oldest} to {Wanted}; they cannot be read, and the shard will stop on the engine's own prompt until Saves/BotClaims/BotClaims.bin is deleted",
                shape,
                Oldest,
                Shape
            );

            return;
        }

        var held = reader.ReadEncodedInt();

        for (var i = 0; i < held; i++)
        {
            var facet = reader.ReadEncodedInt();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var guild = reader.ReadString();
            var bought = reader.ReadBool();

            BotClaim.Restore(facet, x, y, guild, bought);
        }

        var making = reader.ReadEncodedInt();
        var back = 0;
        var held2 = 0;

        for (var i = 0; i < making; i++)
        {
            var facet = reader.ReadEncodedInt();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var guild = reader.ReadString();
            var want = reader.ReadEncodedInt();
            var from = reader.ReadString();
            var escrow = shape >= 2 ? reader.ReadEncodedInt() : 0;

            if (BotClaim.Reopen(facet, x, y, guild, (BotClaim.Kind)want, from, escrow))
            {
                back++;
                held2 += escrow;
            }
        }

        var rests = shape >= 2 ? reader.ReadEncodedInt() : 0;

        for (var i = 0; i < rests; i++)
        {
            var guild = reader.ReadString();
            var facet = reader.ReadEncodedInt();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var failures = reader.ReadEncodedInt();
            var ago = reader.ReadLong();

            BotClaim.Rest(guild, (facet, x, y), failures, ago);
        }

        Restored = held;

        logger.Information(
            "The register of ground was read back: {Held} squares belong to somebody, {Back} claims that were still being made have their five minutes again ({Escrow}gp held for them), and {Rests} failed musters still rest their squares",
            held,
            back,
            held2,
            rests
        );
    }
}
