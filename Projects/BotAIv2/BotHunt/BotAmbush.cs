using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The blow struck out of hiding: three swings' worth in one, and the victim stunned where it stands.
///
/// <para>
/// <b>Patrick's order of 17.09.2026, night.</b> "A bandit striking from ambush (out of stealth) stuns the bot and deals
/// three times the damage, an archer all but one-shots another bot. A hand blow stuns for three seconds, an arrow for
/// one." Until this, a robbery that had stalked its mark for a minute and come out of hiding beside it opened with an
/// ordinary swing, and the whole of the stalk bought nothing but surprise the engine does not model.
/// </para>
///
/// <para>
/// <b>Five seconds for both, by his order of the evening of 18.09.2026</b>, after a day in which the hold decided
/// every robbery on the island: three times a crossbow is 51 against 63 to 68 hit points, four fifths and not a kill,
/// and a mark left standing after one second outran a bowman to a ward every time. Seven robberies that day, seven
/// endings that read "the victim reached the town". The blow is still worth three; what changed is how long the mark
/// is held for the second one. See <c>BotQuarter</c> for what it does when it comes to.
/// </para>
///
/// <para>
/// The blow is the weapon's own (<c>BaseWeapon.ComputeDamage</c>, the same reckoning a real swing uses, tactics,
/// strength and anatomy included) taken <see cref="Times"/> over, and the stun is the engine's own
/// <c>Mobile.Paralyze</c>, which has its own expiry. Nothing here is a new rule of combat: it is the shard's own swing,
/// thrice, and the engine's own hold. The arrow holds for a shorter while because its advantage is the damage — an
/// archer that opens from hiding at fourteen tiles will usually not need a second.
/// </para>
/// </summary>
public static class BotAmbush
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAmbush));

    /// <summary>Whether a blow out of hiding is worth more than any other. False makes an ambush an ordinary opening.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>What the blow out of hiding is worth against an ordinary swing of the same weapon.</summary>
    public static double Times { get; set; } = 3.0;

    /// <summary>How long a victim struck by a hand weapon stands stunned.</summary>
    public static int MeleeStunMs { get; set; } = 5000;

    /// <summary>How long a victim struck by an arrow stands stunned.</summary>
    public static int ArrowStunMs { get; set; } = 5000;

    /// <summary>
    /// How long a backstab holds the mark. Patrick's order of the evening of 18.09.2026: five seconds for anybody,
    /// six for a bot that already knows how to move while hidden, which strikes from behind rather than out of a
    /// bush. The extra second is the whole of the difference: the blow itself is worth the same <see cref="Times"/>.
    /// </summary>
    public static int BackstabMs { get; set; } = 6000;

    /// <summary>Blows struck by a bot that can move hidden: a backstab rather than an ambush.</summary>
    public static long Backstabs { get; private set; }

    /// <summary>What a bare-handed ambusher's swing is worth when it carries no weapon at all.</summary>
    public static int BareBlow { get; set; } = 5;

    /// <summary>Blows struck out of hiding.</summary>
    public static long Struck { get; private set; }

    /// <summary>Of those, the ones struck with a bow.</summary>
    public static long ByArrow { get; private set; }

    /// <summary>Victims the blow alone left dead.</summary>
    public static long Felled { get; private set; }

    /// <summary>Hit points taken by ambushes in all.</summary>
    public static long Dealt { get; private set; }

    /// <summary>Ambushes on a victim that was already dead, gone or out of the world by the time the blow landed.</summary>
    public static long Missed { get; private set; }

    /// <summary>
    /// Strikes the opening blow of a robbery that came out of hiding. Called once, at the moment the stalk ends and the
    /// fight begins; the ordinary fight follows immediately and needs no help from here.
    /// </summary>
    /// <param name="backstab">
    /// Whether the striker can move while hidden, which makes this a backstab and holds the mark a second longer.
    /// </param>
    /// <returns>How long the mark is held, in milliseconds, so that the caller knows when the hold ends.</returns>
    public static int Strike(Mobile robber, Mobile victim, bool backstab = false)
    {
        if (!Running)
        {
            return 0;
        }

        if (robber is not { Deleted: false, Alive: true } || victim is not { Deleted: false, Alive: true }
            || robber.Map == null || robber.Map != victim.Map)
        {
            Missed++;

            return 0;
        }

        var weapon = robber.Weapon as BaseWeapon;
        var arrow = weapon is BaseRanged;
        var swing = weapon?.ComputeDamage(robber, victim) ?? BareBlow;
        var damage = (int)Math.Round(Math.Max(1, swing) * Times);

        // The engine reveals a harmful action by itself; saying it plainly first keeps the order of the log honest.
        robber.RevealingAction();
        robber.DoHarmful(victim);

        var before = victim.Hits;

        victim.Damage(damage, robber);

        var stun = backstab ? BackstabMs : arrow ? ArrowStunMs : MeleeStunMs;

        if (stun > 0 && victim.Alive)
        {
            victim.Paralyze(TimeSpan.FromMilliseconds(stun));
        }

        Struck++;
        Dealt += Math.Max(0, before - victim.Hits);

        if (backstab)
        {
            Backstabs++;
        }

        if (arrow)
        {
            ByArrow++;
        }

        if (!victim.Alive)
        {
            Felled++;
        }

        logger.Information(
            "{Name} {How} {Victim} for {Damage} with {Weapon}, {Left} hit points left of {Before}, held {Stun}s",
            robber.Name,
            backstab ? "backstabbed" : "struck out of hiding",
            victim.Name,
            damage,
            weapon?.GetType().Name ?? "bare hands",
            Math.Max(0, victim.Hits),
            before,
            stun / 1000.0
        );

        return victim.Alive ? stun : 0;
    }

    public static string Describe() =>
        $"{Struck} blows struck out of hiding ({ByArrow} of them with a bow, {Backstabs} of them backstabs), {Dealt} hit points taken, {Felled} victims left dead by the blow alone, {Missed} that found nothing to strike";

    public static void Forget()
    {
        Struck = 0;
        ByArrow = 0;
        Backstabs = 0;
        Felled = 0;
        Dealt = 0;
        Missed = 0;
    }
}
