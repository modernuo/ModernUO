using System;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Closing with a member of a guild yours is at war with, and fighting them.
///
/// <para>
/// <b>The one errand on this shard that strikes another bot, and it exists because Patrick asked why the
/// bailiff's sentence had no second half.</b> <c>BotEvict</c> says "this is the land of The Crown, move
/// along" and its own note says a blow is what a war is for — and there was no war, and nothing that struck
/// one. This is that half: a licence held by <see cref="BotFeud.Quarrel"/>, and never by this errand's own
/// opinion of anybody.
/// </para>
///
/// <para>
/// <b>The fighting is the engine's, exactly as it is for a hunt.</b> Setting <c>Combatant</c> starts the
/// server's own swing timer; everything about hit chance, damage, the weapon's speed and the disturbing of a
/// bandage is the engine's business. This errand only closes the distance, sets the combatant, renews the
/// guild's call so mates keep coming, and knows when it is over.
/// </para>
///
/// <para>
/// <b>It ends on a fall and never on a corpse.</b> A bot that dies is resurrected by the population and its
/// pack goes with it — looting a guildmate's corpse is a separate order and is not assumed here. So the
/// ending is "they are down", and what happens to the fallen belongs to whoever wrote the resurrection.
/// </para>
/// </summary>
public sealed class BotQuarrel : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "quarrel";

    /// <summary>
    /// What fighting a guild's enemy is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// High, and for the same reason <c>BotEvict.Prior</c> had to be raised: this errand is rare by its
    /// conditions — a declared war, an enemy within sight, both out of a guarded region — and an errand that
    /// is rare by conditions and also loses every auction is an errand that never runs at all. Below a
    /// company's hunt, so a bot with an ogre in front of it keeps the ogre.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 400.0;

    /// <summary>
    /// What defending the guild's own ground is reckoned at per minute.
    ///
    /// Above a company's hunt and above everything else a bot is offered, by Patrick's order of 13.09.2026:
    /// an enemy on the yard is the one thing worth dropping an ogre for. Still a price rather than a command
    /// — the floor that protects work already paid for still holds, and so does the health check.
    /// </summary>
    public static double DefendPrior { get; set; } = 700.0;

    /// <summary>Quarrels taken up to defend the guild's own ground.</summary>
    public static long Defences { get; private set; }

    /// <summary>How long a quarrel is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>How near the enemy the bot must be to swing. The engine's own weapon reach at melee.</summary>
    public static int Reach { get; set; } = 1;

    /// <summary>How long one may run before it is given up as a chase that will not end.</summary>
    public static int FightMs { get; set; } = 120000;

    /// <summary>Enemies brought down.</summary>
    public static long Felled { get; private set; }

    /// <summary>Quarrels the bot lost.</summary>
    public static long Fallen { get; private set; }

    /// <summary>Quarrels given up because the enemy got away or the war ended under it.</summary>
    public static long Broken { get; private set; }

    private readonly BotMobile _them;

    private readonly string _ours;

    /// <summary>The guild this quarrel is fought for, by name. See <c>BotWar.Involved</c>.</summary>
    public string Ours => _ours;

    /// <summary>Whether this is the yard being defended rather than a quarrel happened upon.</summary>
    private readonly bool _defending;

    private long _began;

    private bool _begun;

    private bool _struck;

    public BotQuarrel(BotMobile them, string ours) : this(them, ours, false)
    {
    }

    public BotQuarrel(BotMobile them, string ours, bool defending)
    {
        _them = them;
        _ours = ours;
        _defending = defending;

        if (defending)
        {
            Defences++;
        }
    }

    public override string Kind => Trade;

    /// <summary>The guild's war, standing in front of the bot. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    public override Map Map => _them?.Map;

    public override Point3D Where => _them?.Location ?? Point3D.Zero;

    public override double Expects => _defending ? DefendPrior : Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    /// <summary>
    /// Not about money, and saying so is what lets it happen at all.
    ///
    /// A guild's war earns the bot nothing, and an errand judged on takings that has none is an errand the
    /// ledger learns to refuse within the hour. See <c>BotBaron</c>, where a bot stood twelve minutes because
    /// unpaid work was being marked down for being unpaid.
    /// </summary>
    public override bool Unpaid => true;

    public override string Stage =>
        (_defending ? "defending our ground: " : "")
        + (_struck ? $"fighting {_them?.Name} of the enemy" : $"closing with {_them?.Name} of the enemy");

    /// <summary>One named bot. If the way to it is shut, the quarrel is over rather than re-routed.</summary>
    public override bool Bend(IBotWilful bot) => false;

    /// <summary>
    /// Defending the yard jumps the work in hand; an ordinary quarrel waits its turn.
    ///
    /// Patrick's order of 13.09.2026 said "priority", and a price alone is not one: the floor that keeps a
    /// bot on work it has already paid for held a miner at its seam while the enemy stood on the hall's
    /// step. Pressing is what a rescue and a company's call already have, for the same reason.
    /// </summary>
    public override bool Pressing(IBotWilful bot) => _defending;

    public override void Drop(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body != null && body.Combatant == _them)
        {
            body.Combatant = null;
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _them is not { Deleted: false } || _them.Map == null || _them.Map == Map.Internal)
        {
            Broken++;

            return BotDoing.Failed("whoever it was is gone");
        }

        if (!_begun)
        {
            _begun = true;
            _began = Core.TickCount;
        }

        if (!_them.Alive)
        {
            Felled++;
            body.Combatant = null;

            // The guild is done with this one. Anything still walking at them is walking at a corpse.
            BotFeud.Drop(body.Guild as Guild);

            return BotDoing.Done($"{_them.Name} of the enemy is down");
        }

        if (!body.Alive)
        {
            Fallen++;

            return BotDoing.Failed($"went down fighting {_them.Name}");
        }

        // Asked every beat and not only at the start: a war can end under a fight — see BotRegard.Amity —
        // and a bot that goes on swinging after the peace is a bot committing a crime in front of a guard.
        if (!BotFeud.Quarrel(body, _them))
        {
            Broken++;
            body.Combatant = null;

            return BotDoing.Failed($"no quarrel with {_them.Name} any more");
        }

        if (Core.TickCount - (_began + FightMs) >= 0)
        {
            Broken++;
            body.Combatant = null;

            return BotDoing.Failed($"{_them.Name} was not brought down inside {FightMs / 1000}s");
        }

        // Renewed while somebody is actually standing in the fight, which is the only evidence the call is
        // still worth anybody answering. See BotFeud.CallMs.
        BotFeud.Call(body.Guild as Guild, _them);

        if (!body.InRange(_them.Location, Reach))
        {
            // Followed rather than aimed at: an enemy moves, and a walk order to the tile it stood on a
            // moment ago is a walk order replaced every beat. The same lesson BotEvict carries.
            return BotDoing.Walk(_them.Map, _them, BotArrival.Within(Reach), $"at {_them.Name} of the enemy");
        }

        body.Combatant = _them;
        _struck = true;

        return BotDoing.Work($"fighting {_them.Name}");
    }

    public static string Describe() =>
        Felled + Fallen + Broken == 0
            ? "no blows have been struck between guilds"
            : $"{Felled} enemies brought down, {Fallen} members lost doing it, {Broken} quarrels broken off, {Defences} taken up in defence of the guild's own ground at {DefendPrior:F0}/min";

    public static void Forget()
    {
        Felled = 0;
        Fallen = 0;
        Broken = 0;
        Defences = 0;
    }
}
