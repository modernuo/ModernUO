using Server.SkillHandlers;

namespace Server.BotAI.V2;

/// <summary>
/// Hiding, moving unseen and searching for the hidden, as a bot does them: the engine's own three skills, used without a client.
///
/// <para>
/// <b>Patrick's order of 17.09.2026, evening: criminals act unseen.</b> They hide, train their hiding, and stalk their
/// victims hidden; and a bot that finds a body searches for the killer with Detect Hidden and tells the Baron. Nothing here
/// bends the era. The skills are the engine's (<c>Hiding</c>, <c>Stealth</c>, <c>DetectHidden</c>), used through
/// <c>Skills.UseSkill</c> as a player's macro would, and gained at the engine's own rate — Patrick chose the honest rate
/// over a faster one for bots. The Renaissance rules bind them: a hidden mobile's first step reveals it unless Stealth has
/// granted it quiet steps (<c>Mobile.OnMove</c>), and running or riding reveals it anyway; Stealth wants Hiding 80 and
/// armour under 26 (<c>Stealth.HidingRequirement</c>, <c>Stealth.GetArmorRating</c>) and grants a step for every ten
/// points; Detect Hidden searches a tenth of its skill in tiles, half that on a failed roll, and a player-mobile with any of
/// it notices a stealther within four tiles by itself (<c>PlayerMobile.OnMovement</c>). Every use costs the ten seconds of
/// the skill clock.
/// </para>
/// </summary>
public static class BotShadow
{
    /// <summary>
    /// Whether criminals act unseen at all. Off, a robber goes in openly as before build 112 and nobody is pressed to practise
    /// or to lie low; bodies are still found and reported (<see cref="BotInquest.Running"/>).
    /// </summary>
    public static bool Running { get; set; } = true;

    /// <summary>The Hiding a robber must have before it robs at all; below it the temptation sends it to practise. Patrick's choice of 17.09.2026.</summary>
    public static double RobHiding { get; set; } = 30.0;

    /// <summary>The Stealth a robber must have to stalk its victim hidden rather than lie in wait beside it: three quiet steps a use.</summary>
    public static double StalkStealth { get; set; } = 30.0;

    /// <summary>Uses of Hiding.</summary>
    public static long HideTries { get; private set; }

    /// <summary>Tries at hiding that had to put up a weapon first, because the engine refuses to hide a bot at war.</summary>
    public static long Sheathed { get; private set; }

    /// <summary>Of those, the ones that left the bot hidden.</summary>
    public static long Hid { get; private set; }

    /// <summary>Uses of Stealth.</summary>
    public static long QuietTries { get; private set; }

    /// <summary>Of those, the ones that left the bot hidden with quiet steps to take.</summary>
    public static long WentQuiet { get; private set; }

    /// <summary>Searches made with Detect Hidden.</summary>
    public static long Searches { get; private set; }

    /// <summary>Whether the engine will take a skill from this one now: its skill clock is run down and it is casting nothing.</summary>
    public static bool Ready(Mobile m) =>
        m is { Deleted: false, Alive: true } && Core.TickCount - m.NextSkillTime >= 0 && m.Spell == null;

    /// <summary>Whether this one hides well enough to rob. See <see cref="RobHiding"/>.</summary>
    public static bool CanHide(Mobile m) => m != null && m.Skills.Hiding.Base >= RobHiding;

    /// <summary>Whether the engine would let this one move hidden at all: Hiding enough for Stealth, and armour light enough. Riding is the walk's to settle (<c>BotDeed.Afoot</c>).</summary>
    public static bool MayStealth(Mobile m) =>
        m != null && m.Skills.Hiding.Base >= Stealth.HidingRequirement && Stealth.GetArmorRating(m) < (Core.AOS ? 42 : 26);

    /// <summary>Whether this one moves hidden well enough to stalk. See <see cref="StalkStealth"/>.</summary>
    public static bool CanStalk(Mobile m) => MayStealth(m) && m.Skills.Stealth.Value >= StalkStealth;

    /// <summary>Uses Hiding if the engine will take it now. True when the bot is hidden afterwards.</summary>
    public static bool Hide(Mobile m)
    {
        if (!Ready(m))
        {
            return m?.Hidden == true;
        }

        // <b>A bot still at war with something cannot hide at all, and it does not say so in words a bot hears.</b>
        // The engine's Hiding refuses outright while the mobile's own combatant is within (100 - Hiding) / 2 + 8 tiles
        // — eighteen for anybody below sixty-four — with line of sight, or while anything within that range has this
        // one as its combatant. A robber comes to its victim straight off a prowl with its last creature still set as
        // its combatant, so on the night of 17.09.2026 robbery after robbery ended "could not hide near X" without one
        // roll of the skill ever being made. Sheathing first is what a player does before hiding, and it is free.
        if (m.Combatant != null || m.Warmode)
        {
            m.Combatant = null;
            m.Warmode = false;
            Sheathed++;
        }

        HideTries++;
        global::Server.Skills.UseSkill(m, SkillName.Hiding);

        if (m.Hidden)
        {
            Hid++;
        }

        return m.Hidden;
    }

    /// <summary>
    /// Uses Stealth on a hidden bot if the engine will take it now. True when it is hidden with quiet steps afterwards; a failed
    /// roll reveals it, as the engine does to anybody.
    /// </summary>
    public static bool Quiet(Mobile m)
    {
        if (!Ready(m) || !m.Hidden)
        {
            return m is { Hidden: true, AllowedStealthSteps: > 0 };
        }

        QuietTries++;
        global::Server.Skills.UseSkill(m, SkillName.Stealth);

        var quiet = m is { Hidden: true, AllowedStealthSteps: > 0 };

        if (quiet)
        {
            WentQuiet++;
        }

        return quiet;
    }

    /// <summary>Searches around this bot with Detect Hidden, as a player targeting itself does. True when the search was made.</summary>
    public static bool Search(Mobile m)
    {
        if (!Ready(m) || !global::Server.Skills.UseSkill(m, SkillName.DetectHidden) || m.Target == null)
        {
            return false;
        }

        Searches++;
        m.Target.Invoke(m, m);

        return true;
    }

    public static string Describe() =>
        $"hiding used {HideTries} times and hid {Hid} ({Sheathed} had to put up a weapon first), stealth used {QuietTries} times and moved quietly {WentQuiet}, {Searches} searches with Detect Hidden";

    public static void Forget()
    {
        HideTries = 0;
        Sheathed = 0;
        Hid = 0;
        QuietTries = 0;
        WentQuiet = 0;
        Searches = 0;
    }
}
