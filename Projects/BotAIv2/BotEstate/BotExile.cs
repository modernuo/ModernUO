using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Who won a war, and the debt the loser owes because of it: its hall goes outside the winner's yard.
///
/// <para>
/// <b>Patrick's order of 10.09.2026: the losers of a war must move their house further from the winner's,
/// out of his zone of influence.</b> Until this, a war ended the way it began — as a number crossing a
/// threshold — and left the island exactly as it found it. Nothing on the map ever recorded that anything
/// had happened, so a war was an expensive way of producing corpses.
/// </para>
///
/// <para>
/// <b>Won on blood and nothing else.</b> Trespasses, defiances and border drift are what <em>start</em> a
/// war; who won it is who killed more of the other, counted only while the two were actually at war. That
/// is the one measure both sides can be said to have taken part in, and it is a fact rather than a
/// preference — a guild that declared war and then hid in Britain has not won anything.
/// </para>
///
/// <para>
/// <b>A drawn war moves nobody, and that is its own outcome rather than a failure.</b> Equal blood, or none
/// at all, means the island learned that these two cannot hurt each other, and pushing somebody's hall
/// across the map for that would be arbitrary. Counted apart in <see cref="Drawn"/>, because a shard where
/// every war is drawn and a shard where no war is fought look the same from the estate's line.
/// </para>
/// </summary>
public static class BotExile
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotExile));

    /// <summary>Whether losing a war costs a guild its ground at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How far from the winner's hall the loser's new one has to stand.
    ///
    /// <para>
    /// Derived rather than chosen: it is <c>BotLand.Reach</c>, the distance a hall's land already reaches,
    /// because "out of his zone of influence" is a sentence about that number and not about a new one. Two
    /// numbers meaning the same thing is how this project has hurt itself most often; if the yard grows,
    /// this grows with it.
    /// </para>
    /// </summary>
    public static int Clear => BotLand.Reach;

    /// <summary>How long a member's claim on the removal lasts after it was last heard from.</summary>
    public static int ClaimMs { get; set; } = 240000;

    /// <summary>This officer's name in the shared register of who is on what.</summary>
    public const string Office = "remover";

    /// <summary>Wars whose end was judged.</summary>
    public static long Judged { get; private set; }

    /// <summary>Of those, the ones where neither side drew more blood than the other.</summary>
    public static long Drawn { get; private set; }

    /// <summary>Verdicts that fell on a guild with no hall to move, or against one with no hall to move away from.</summary>
    public static long Groundless { get; private set; }

    /// <summary>Halls actually picked up and put down again outside the winner's yard.</summary>
    public static long Moved { get; private set; }

    /// <summary>Who owes a move, and the hall they are being put outside of.</summary>
    private static readonly Dictionary<string, string> _owed = [];

    /// <summary>
    /// A war ended with nobody ahead. Nobody moves, and it is counted apart so that it can be told from a
    /// shard where nothing is ever fought.
    ///
    /// The score itself lives in <c>BotWar</c> now, which is what decides a war: this only ever hears the
    /// verdict. The first cut kept its own tally of blood here, counted whether or not the two were at
    /// war, and the two books disagreed about what a war was.
    /// </summary>
    public static void Draw(string one, string other)
    {
        if (!Running || one == null || other == null || one == other)
        {
            return;
        }

        Judged++;
        Drawn++;

        logger.Information("The war between {One} and {Other} ended with nobody ahead; nobody moves", one, other);
    }

    /// <summary>
    /// A war has been won. Files what the loser owes: its hall out of the winner's yard.
    /// </summary>
    public static void Sentence(string winner, string loser)
    {
        if (!Running || winner == null || loser == null || winner == loser)
        {
            return;
        }

        // <b>Superseded.</b> Patrick's order of 26.09.2026: the yard is only the square with the hall, and a hall moves only
        // when that square is lost (BotClaim, BotEstate.RazeHall). A loser's hall never stands in the winner's square, so
        // the old sentence — carry it outside the winner's yard — has nothing left to ask; and on the morning of 26.09.2026
        // it carried The Lantern towards The Blade three times, into the next war each time.
        if (!AfterWar)
        {
            Superseded++;

            return;
        }

        Judged++;

        // Both halls have to exist for the sentence to mean anything: no hall of the winner's is no yard to
        // be outside of, and no hall of the loser's is nothing to move.
        if (BaseGuild.FindByName(winner) is not Guild won || BaseGuild.FindByName(loser) is not Guild lost ||
            BotEstate.Hall(won) is not { Deleted: false } || BotEstate.Hall(lost) is not { Deleted: false })
        {
            Groundless++;

            return;
        }

        _owed[loser] = winner;

        logger.Warning(
            "{Loser} lost its war with {Winner} and must move its hall outside the winner's yard",
            loser,
            winner
        );
    }

    /// <summary>Every move still owed, as loser and winner. For the store.</summary>
    public static IEnumerable<KeyValuePair<string, string>> Owing => _owed;

    /// <summary>A move read back off the save. See <c>BotWarStore</c>.</summary>
    public static void Restore(string loser, string winner)
    {
        if (loser != null && winner != null)
        {
            _owed[loser] = winner;
        }
    }

    /// <summary>The hall this guild has to put itself outside of, or nothing if it owes no move.</summary>
    public static BaseHouse Owed(Guild guild)
    {
        if (!Running || !AfterWar || guild == null || !_owed.TryGetValue(guild.Name, out var winner))
        {
            return null;
        }

        if (BaseGuild.FindByName(winner) is not Guild won || BotEstate.Hall(won) is not { Deleted: false } hall)
        {
            // The winner's hall has gone since the verdict. There is nothing to be outside of any more, so
            // the debt goes with it rather than sending somebody to walk away from a ruin.
            _owed.Remove(guild.Name);

            return null;
        }

        return hall;
    }

    /// <summary>Whether a lost war still sends the loser's hall out of the winner's yard. Off: see <see cref="Sentence"/>.</summary>
    public static bool AfterWar { get; set; }

    /// <summary>Verdicts that would once have sent a hall away, passed over since the yard became one square.</summary>
    public static long Superseded { get; private set; }

    /// <summary>The debt is paid: the hall stands somewhere else now.</summary>
    public static void Paid(Guild guild)
    {
        if (guild != null && _owed.Remove(guild.Name))
        {
            Moved++;
        }
    }

    /// <summary>Part of the estate's line.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "a lost war costs no ground";
        }

        return Judged == 0
            ? "no war has been judged"
            : $"{Judged} wars judged, {Drawn} of them drawn, {Groundless} with no hall to move or none to move away from; "
            + $"{Moved} halls carried outside the winner's yard, {_owed.Count} still owing a move";
    }

    public static void Forget()
    {
        _owed.Clear();
        Judged = 0;
        Drawn = 0;
        Groundless = 0;
        Moved = 0;
    }
}
