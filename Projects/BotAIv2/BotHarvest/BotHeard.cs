using System.Collections.Generic;
using Server.Engines.Harvest;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// What the harvest system last said to each bot, and which of its sentences mean what.
///
/// <para>
/// <b>Patrick's order of 09.09.2026: a vein counts as worked out when the bot <em>sees</em> the message that
/// there is nothing in it.</b> Everything before this inferred it — a run of quiet swings, or the engine's
/// bank read from the side — and inference was wrong about two write-offs in three. The engine has been
/// saying it plainly the whole time: <c>NoResourcesMessage</c>, "There is no metal here to mine." The
/// sentence simply had nowhere to land, because a bot has no <c>NetState</c> and
/// <c>Mobile.SendLocalizedMessage</c> writes straight to one without being virtual.
/// </para>
///
/// <para>
/// So the engine now raises <c>HarvestDefinition.Said</c> before it sends — see
/// <c>engine-patches/HarvestDefinition-said.patch</c> — and this listens. It is the whole difference between
/// a miner that knows why its swing was quiet and one that has to guess: the same system distinguishes six
/// cases and we could read none of them.
/// </para>
///
/// <list type="table">
/// <item><term>no resources</term><description>the vein is worked out. This is the one Patrick asked for.</description></item>
/// <item><term>double harvest</term><description>somebody else took the last of it first — same conclusion.</description></item>
/// <item><term>fail</term><description>a missed roll. The rock is fine; swing again.</description></item>
/// <item><term>out of range</term><description>the bot moved between the swing and its resolution.</description></item>
/// <item><term>pack full</term><description>the ore was taken out of the bank and destroyed. Go and unload.</description></item>
/// <item><term>tool broke</term><description>no pickaxe; the errand is over.</description></item>
/// </list>
///
/// <para>
/// Kept as one row per bot rather than a queue, because only the last thing said matters and a queue would be
/// a table that grows for the life of the shard. Rows are dropped when a bot is.
/// </para>
/// </summary>
public static class BotHeard
{
    /// <summary>What the harvest system said, in the only terms it uses.</summary>
    public enum Word
    {
        /// <summary>Nothing has been said to this bot yet, or nothing since it was last read.</summary>
        Nothing,

        /// <summary>There is no metal here to mine: the vein is worked out.</summary>
        Empty,

        /// <summary>Somebody has gotten to the metal before you: worked out by another hand.</summary>
        Taken,

        /// <summary>You loosen some rocks but fail to find any useable ore: a missed roll.</summary>
        Missed,

        /// <summary>You have moved too far away to continue: the swing was cancelled, not rolled.</summary>
        Adrift,

        /// <summary>Your backpack is full, so the ore you mined is lost.</summary>
        Full,

        /// <summary>You have worn out your tool.</summary>
        Broken,

        /// <summary>Something else the system says, kept apart so the counters stay honest.</summary>
        Other
    }

    /// <summary>Whether the ear is open at all.</summary>
    public static bool Running { get; private set; }

    /// <summary>Sentences heard, by kind, for the ground's line.</summary>
    private static readonly Dictionary<Word, long> _tally = [];

    private static readonly Dictionary<Serial, (Word Said, long When, HarvestDefinition Of)> _last = [];

    /// <summary>
    /// How recent a sentence has to be to be acted on, in milliseconds.
    ///
    /// <para>
    /// <b>Because a sentence outlives the swing that caused it, and the first cut of this let it.</b> The ear
    /// records one row per bot; a bot that was cutting wood a minute ago and is mining now would have had its
    /// axe's "there is no wood here" read as a verdict on the first rock it swung at. Measured within twenty
    /// minutes of the ear being opened: <b>1,105 "there is nothing left here" against 24 rocks written
    /// off</b> — the great majority of them were the lumberjacks, and mining was reading them.
    /// </para>
    ///
    /// <para>
    /// Two guards rather than one, because they answer different questions. This one says the sentence is
    /// about the swing just taken; <see cref="Last"/>'s definition argument says it is about the same craft.
    /// A little over the swing interval, so the sentence from the last swing still counts and the one before
    /// it does not.
    /// </para>
    /// </summary>
    public static int FreshMs { get; set; } = 2500;

    /// <summary>Opens the ear. Called once, by the harvest module.</summary>
    public static void Listen()
    {
        if (Running)
        {
            return;
        }

        HarvestDefinition.Said += Hear;
        Running = true;
    }

    /// <summary>Closes it again, so a world reload does not leave two ears on one head.</summary>
    public static void Forget()
    {
        if (Running)
        {
            HarvestDefinition.Said -= Hear;
            Running = false;
        }

        _last.Clear();
        _tally.Clear();
        Stale = 0;
    }

    private static void Hear(Mobile from, HarvestDefinition def, TextDefinition message)
    {
        if (from is not BotMobile { Deleted: false } bot || def == null)
        {
            return;
        }

        var said = Which(def, message);

        _last[bot.Serial] = (said, Core.TickCount, def);

        _tally.TryGetValue(said, out var many);
        _tally[said] = many + 1;
    }

    /// <summary>
    /// Which of the system's own sentences this was.
    ///
    /// Compared against the definition's own fields rather than against numbers written down here: the
    /// clilocs differ between mining, lumberjacking and sand, and a table of them would be a second copy of
    /// something the definition already holds.
    /// </summary>
    private static Word Which(HarvestDefinition def, TextDefinition message)
    {
        if (message == null)
        {
            return Word.Other;
        }

        if (Same(message, def.NoResourcesMessage))
        {
            return Word.Empty;
        }

        if (Same(message, def.DoubleHarvestMessage))
        {
            return Word.Taken;
        }

        if (Same(message, def.FailMessage))
        {
            return Word.Missed;
        }

        if (Same(message, def.TimedOutOfRangeMessage) || Same(message, def.OutOfRangeMessage))
        {
            return Word.Adrift;
        }

        if (Same(message, def.PackFullMessage))
        {
            return Word.Full;
        }

        if (Same(message, def.ToolBrokeMessage))
        {
            return Word.Broken;
        }

        return Word.Other;
    }

    private static bool Same(TextDefinition said, TextDefinition mine) =>
        mine != null && (said.Number > 0 ? said.Number == mine.Number : said.String == mine.String);

    /// <summary>
    /// The last thing said to this bot, and how long ago in milliseconds.
    ///
    /// Read rather than consumed: the same sentence answers two questions in one beat — whether the rock is
    /// empty and whether the swing was even rolled — and a read that cleared it would let the first caller
    /// hide the answer from the second.
    /// </summary>
    public static Word Last(Mobile bot, HarvestDefinition about, out long since)
    {
        since = long.MaxValue;

        if (bot == null || !_last.TryGetValue(bot.Serial, out var heard))
        {
            return Word.Nothing;
        }

        since = Core.TickCount - heard.When;

        // The same craft, and recent enough to be about the swing just taken. See FreshMs: a bot that was
        // cutting wood a minute ago must not have its axe's verdict read as a verdict on this rock.
        if (about != null && heard.Of != about || since > FreshMs)
        {
            Stale++;

            return Word.Nothing;
        }

        return heard.Said;
    }

    /// <summary>Sentences passed over as being about another craft, or about an older swing.</summary>
    public static long Stale { get; private set; }

    /// <summary>Forgets what was said to this bot, once it has been acted on.</summary>
    public static void Clear(Mobile bot)
    {
        if (bot != null)
        {
            _last.Remove(bot.Serial);
        }
    }

    /// <summary>How many of each sentence has been heard, for the ground's line.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "nobody is listening to what the harvest system says";
        }

        var empty = Count(Word.Empty);
        var taken = Count(Word.Taken);
        var missed = Count(Word.Missed);
        var adrift = Count(Word.Adrift);
        var full = Count(Word.Full);
        var broken = Count(Word.Broken);

        return $"the harvest system said: {empty} times there is nothing left here, {taken} somebody got there first, "
            + $"{missed} a missed swing, {adrift} moved too far to finish, {full} a full pack, {broken} a worn-out tool, "
            + $"{Count(Word.Other)} something else, and {Stale} sentences were passed over as being about another craft or an older swing";
    }

    private static long Count(Word said) => _tally.TryGetValue(said, out var many) ? many : 0;
}
