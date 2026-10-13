using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Five bots underground: what they have taken, whose it is, how many of them may still be put back on
/// their feet, and the promise that every one of them comes home.
///
/// <para>
/// <b>Patrick's order of 11.09.2026 is four rules, and they pull against each other in a way worth being
/// explicit about.</b> Gold is divided at the end, half to the leader and the rest evenly between the other
/// four; a bot that dies with no resurrection left goes back to the world and <i>keeps what it had already
/// accumulated, unchanged</i>; and only those still alive go on accumulating. The first rule alone would be
/// one division at the end, which cannot express the second: at the end there is no record of what anybody
/// had accumulated, because nothing accumulated anything.
/// </para>
///
/// <para>
/// <b>So the pot is kept as it fills.</b> Every beat, whatever a member has picked up since the last one is
/// swept into the pot and credited there and then — half to the leader, the rest evenly between the others
/// still down there. A bot sent home at nine minutes keeps the credit of the first nine minutes exactly as
/// it stood, and the four who fought on divide the last eleven between them. At the end each member is
/// handed its own credit in coin. Read across a whole delve with nobody lost, this is precisely the
/// fifty-fifty split of the first rule; read across one with a loss, it is the second and third.
/// </para>
///
/// <para>
/// <b>Sweeping is also what keeps the money safe.</b> A bot that dies drops its pack on its corpse, and a
/// corpse on the floor of Destard is gold out of the world. What the party has already taken is in the pot,
/// which is a number rather than an item, and nothing underground can kill a number.
/// </para>
///
/// <para>
/// <b>Three resurrections to a party, and the fourth death is the way out.</b> Raised where they fell,
/// because a delver carried home is a delver who has left; the ordinary revival — a minute face down and
/// then home to Britain — is exactly what should happen to the fourth, and it happens by this class doing
/// nothing at all. See <c>BotPopulation.Revive</c>, which asks here first.
/// </para>
///
/// <para>
/// <b>And the one promise that matters more than any of it: everybody comes up.</b> A bot left in a dungeon
/// is a bot with no road home — the block has no walkable connection to the island — so it would stand
/// there failing errands until the world was reloaded. Every party is on a list here, the list is walked on
/// the delve's own beat, and anything that has outlived its delve by <see cref="GraceMs"/> is lifted out
/// whatever state the errand got itself into. The count of those is printed: a backstop that starts doing
/// the ordinary work is a backstop that has become the design, and this project has watched that happen
/// twice.
/// </para>
/// </summary>
public sealed class BotDelveParty
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDelveParty));

    /// <summary>How long a fallen delver lies before its own party puts it back up.</summary>
    public static int RaiseMs { get; set; } = 10000;

    /// <summary>
    /// Resurrections a party has between them.
    ///
    /// Three, by order. It is the number that makes a delve a run of attrition rather than a fight that
    /// either goes well or ends: a party can afford to lose the line twice and still finish, and the third
    /// loss is the one that starts costing it members.
    /// </summary>
    public static int Raisings { get; set; } = 3;

    /// <summary>
    /// <b>Whether a party's raisings cost nothing (02.10.2026).</b> Until today a raising was ten seconds face down and up, with no
    /// scroll, no spell and nobody casting either; Patrick's word of 02.10, "the group should carry resurrection scrolls for such
    /// cases", is the era's own answer, and false makes it the rule: a party's three raisings are spent by a member that raises with a
    /// resurrection scroll, the spell from its book or a bandage, exactly as any company raises its fallen (<c>BotSquadRaise</c>,
    /// <see cref="BotRaise"/>), and a delver nobody down there can raise goes home with its raisings unspent. True puts back the free
    /// raising, its corpse now gone through at once.
    /// </summary>
    public static bool FreeRaisings { get; set; } = false;

    /// <summary>Members that went home with raisings left to the party, nobody down there able to raise them. See <see cref="FreeRaisings"/>.</summary>
    public static long Unraised { get; private set; }

    /// <summary>The leader's share of everything the party takes, as a fraction.</summary>
    public static double LeadersShare { get; set; } = 0.5;

    /// <summary>How long after its delve has ended a party is lifted out regardless. See the note above.</summary>
    public static int GraceMs { get; set; } = 60000;

    /// <summary>
    /// How long a settled party's members are given to walk out by the mouth before whoever is still down there is lifted
    /// out. Ten minutes: the rooms lie a few hundred tiles from a mouth. See <see cref="Settle(string, bool)"/> (29.09.2026).
    /// </summary>
    public static int LeaveMs { get; set; } = 600000;

    /// <summary>Members that walked out by the mouth after their delve settled, and those the watchdog had to lift after all.</summary>
    public static long WalkedOut { get; private set; }

    private long _settledTick;

    /// <summary>Parties that have gone down.</summary>
    public static long Delves { get; private set; }

    /// <summary>Gold swept into pots, all told.</summary>
    public static long Taken { get; private set; }

    /// <summary>Gold handed back out at the end.</summary>
    public static long Paid { get; private set; }

    /// <summary>Members put back on their feet where they fell.</summary>
    public static long Raised { get; private set; }

    /// <summary>Members who ran the party out of resurrections and went home.</summary>
    public static long Lost { get; private set; }

    /// <summary>Members lifted out by the watchdog rather than by their own delve. See <see cref="GraceMs"/>.</summary>
    public static long Stranded { get; private set; }

    /// <summary>Every party currently underground.</summary>
    private static readonly List<BotDelveParty> _parties = [];

    private sealed class Share
    {
        public BotMobile Who;

        /// <summary>What it had in its pack when the sweep last looked. Anything above it is the party's.</summary>
        public int Floor;

        /// <summary>What the pot owes it.</summary>
        public int Credit;

        /// <summary>Whether it is still down there and still earning.</summary>
        public bool Down = true;

        /// <summary>The death last counted as gone home unraised, so a ghost asked about every beat is counted once.</summary>
        public long Unraised;
    }

    private readonly List<Share> _shares = [];

    private readonly BotDungeon.Deep _deep;

    private readonly long _bornTick;

    public BotDelveParty(BotDungeon.Deep deep, BotMobile leader)
    {
        _deep = deep;
        Leader = leader;
        Left = Raisings;
        _bornTick = Core.TickCount;
        _parties.Add(this);
        Delves++;
    }

    public BotMobile Leader { get; }

    /// <summary>Resurrections left to the party.</summary>
    public int Left { get; private set; }

    /// <summary>What the pot holds and has already promised out.</summary>
    public int Pot { get; private set; }

    /// <summary>Whether the delve that owns this party has finished with it.</summary>
    public bool Ended { get; private set; }

    /// <summary>How many are still down there.</summary>
    public int Down
    {
        get
        {
            var down = 0;

            for (var i = 0; i < _shares.Count; i++)
            {
                if (_shares[i].Down && _shares[i].Who is { Deleted: false })
                {
                    down++;
                }
            }

            return down;
        }
    }

    /// <summary>Takes a member on, noting what it already had so that the sweep never takes its own money.</summary>
    public void Add(BotMobile bot)
    {
        if (bot is not { Deleted: false } || Holds(bot))
        {
            return;
        }

        _shares.Add(new Share { Who = bot, Floor = Purse(bot) });
    }

    public bool Holds(Mobile bot)
    {
        for (var i = 0; i < _shares.Count; i++)
        {
            if (ReferenceEquals(_shares[i].Who, bot))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sweeps whatever the party has picked up into the pot and credits it.
    ///
    /// <para>
    /// Anything a member is carrying <i>above</i> what it walked in with. The floor follows a purse
    /// downwards as well as up, so a bot that spends or is robbed does not spend the rest of the delve
    /// owing the pot money it never had.
    /// </para>
    /// </summary>
    public void Sweep()
    {
        var took = 0;

        for (var i = 0; i < _shares.Count; i++)
        {
            var share = _shares[i];

            // Only those still down there (02.10.2026): a member sent home — by the fourth death, or with nobody down there able to
            // raise it — keeps what it earns from then on, as the rule says it does. Until today its purse went on being swept.
            if (share.Who is not { Deleted: false } body || !share.Down)
            {
                continue;
            }

            var purse = Purse(body);

            if (purse < share.Floor)
            {
                share.Floor = purse;

                continue;
            }

            var gain = purse - share.Floor;

            if (gain <= 0 || body.Backpack?.ConsumeTotal(typeof(Gold), gain) != true)
            {
                continue;
            }

            share.Floor = Purse(body);
            took += gain;
        }

        if (took <= 0)
        {
            return;
        }

        Pot += took;
        Taken += took;

        Credit(took);
    }

    /// <summary>
    /// Half to the leader and the rest evenly between whoever is still down there.
    ///
    /// <para>
    /// A leader that is itself dead and gone still takes its half: the party is its, the pot exists because
    /// it called for one, and a rule that moved the half about would make killing the leader the most
    /// profitable thing that can happen to the other four. What a fallen <i>member</i> stops taking is its
    /// quarter, which is the rule as given.
    /// </para>
    /// </summary>
    private void Credit(int coins)
    {
        var leaders = (int)Math.Round(coins * LeadersShare, MidpointRounding.AwayFromZero);
        var rest = coins - leaders;

        var sharers = 0;

        for (var i = 0; i < _shares.Count; i++)
        {
            if (_shares[i].Down && !ReferenceEquals(_shares[i].Who, Leader) && _shares[i].Who is { Deleted: false })
            {
                sharers++;
            }
        }

        var each = sharers == 0 ? 0 : rest / sharers;

        // What the division could not split goes to the leader with its own half rather than being dropped:
        // a remainder thrown away is gold leaving the world, quietly, a few coins at a time.
        var leftover = rest - each * sharers;

        for (var i = 0; i < _shares.Count; i++)
        {
            var share = _shares[i];

            if (ReferenceEquals(share.Who, Leader))
            {
                share.Credit += leaders + leftover;

                continue;
            }

            if (share.Down && share.Who is { Deleted: false })
            {
                share.Credit += each;
            }
        }
    }

    /// <summary>
    /// One of ours is dead. Raised where it lies while the party has a resurrection, and let go when it has
    /// not.
    /// </summary>
    /// <returns>True when this party has dealt with it and the ordinary revival is to keep its hands off.</returns>
    public bool Fell(BotMobile bot)
    {
        var share = Of(bot);

        if (share == null || !share.Down)
        {
            return false;
        }

        if (Left <= 0)
        {
            // Out of the party, and out of the pot: it keeps every coin already credited to it and earns
            // nothing after this moment. Handed over to the ordinary revival, which will carry it home.
            share.Down = false;
            Lost++;

            logger.Information(
                "{Bot} has fallen for the last time in {Deep}: the party has no raisings left, so it goes back to the world with {Credit}gp of the pot",
                bot.Name,
                _deep?.Name ?? "the deep",
                share.Credit
            );

            return false;
        }

        // <b>With a member's own means, or not at all (02.10.2026).</b> Held while the company is at the body — a member with a scroll,
        // the spell or a bandage walking to it or rolling for it (BotRaise.Holding) — and spent when it stands up (Spent). A party with
        // nobody down there able to raise it lets it go home with the raisings unspent, out of the pot as a fourth death would be.
        if (!FreeRaisings)
        {
            if (BotRaise.Holding(bot))
            {
                return true;
            }

            if (share.Unraised != bot.FellTick)
            {
                share.Unraised = bot.FellTick;
                share.Down = false;
                Unraised++;

                logger.Information(
                    "{Bot} has fallen in {Deep} and nobody down there can raise it — no resurrection scroll read, no book with the spell, no healer of 80 — so it goes back to the world with {Credit}gp of the pot and {Left} raisings unspent",
                    bot.Name,
                    _deep?.Name ?? "the deep",
                    share.Credit,
                    Left
                );
            }

            return false;
        }

        if (Core.TickCount - (bot.FellTick + RaiseMs) < 0)
        {
            // Still too soon. Held rather than handed over: the ordinary revival would carry it home, and
            // being carried home is the one thing a raising is supposed to prevent.
            return true;
        }

        // And its own corpse gone through where it lies, before it stands (02.10.2026): see BotRaise.Rise for why before. The floor
        // follows the purse up so the sweep does not take back into the pot the coin the delver walked in with.
        if (bot.Remains is { Deleted: false } corpse)
        {
            BotReclaim.Strip(bot, corpse, out _, out _);

            if (corpse.Items.Count == 0)
            {
                bot.Remains = null;
            }
        }

        bot.Resurrect();

        if (!bot.Alive)
        {
            // The engine refused — it does that silently, and in a dungeon it is worth knowing about. Let
            // the ordinary revival have it rather than leaving a ghost underground for ever.
            share.Down = false;

            return false;
        }

        share.Floor = Purse(bot);

        Left--;
        Raised++;

        logger.Information(
            "{Bot} is back on its feet in {Deep} where it fell; the party has {Left} raisings left",
            bot.Name,
            _deep?.Name ?? "the deep",
            Left
        );

        return true;
    }

    /// <summary>
    /// The delve is over: everything still in the pot is handed out, and everybody still down there is put
    /// back on the island.
    /// </summary>
    public int Settle(string why) => Settle(why, true);

    /// <summary>
    /// The same; with <paramref name="lift"/> false, everybody still down there is left to walk out by the mouth — the delve
    /// leads them (BotDelve.Leaving) — and the watchdog lifts whoever is still below <see cref="LeaveMs"/> later. Patrick's
    /// order of 29.09.2026: the way out is the entrance, as the way in is. Returns how many bodies it surfaced: nought when
    /// walking them out, or when the party had settled already.
    /// </summary>
    public int Settle(string why, bool lift)
    {
        if (Ended)
        {
            return 0;
        }

        Ended = true;
        _settledTick = Core.TickCount;

        Sweep();

        var handed = 0;
        var surfaced = 0;

        for (var i = 0; i < _shares.Count; i++)
        {
            var share = _shares[i];

            if (share.Who is not { Deleted: false } body)
            {
                continue;
            }

            if (share.Credit > 0 && body.Backpack != null)
            {
                body.Backpack.DropItem(new Gold(share.Credit));
                handed += share.Credit;
                Paid += share.Credit;
            }

            // Only those still down there. A member sent home at nine minutes has been living its own life
            // since; yanking it back to the home ground to be paid would be the delve reaching out of the
            // dungeon to interrupt work it has nothing to do with.
            // Whoever is still down there comes up, whatever the book says about them: a member the
            // squad let go underground had Down cleared and stayed in the cave. See BotSquad.Release.
            if (lift && (share.Down || (_deep != null && _deep.Holds(body.Location))))
            {
                Surface(body);
                surfaced++;
            }

            if (lift)
            {
                share.Down = false;
            }
        }

        logger.Information(
            "The delve into {Deep} is over — {Why}: {Pot}gp taken, {Handed}gp handed out between {Count}, {Left} raisings unspent",
            _deep?.Name ?? "the deep",
            why,
            Pot,
            handed,
            _shares.Count,
            Left
        );

        if (lift)
        {
            _parties.Remove(this);
        }

        return surfaced;
    }

    /// <summary>
    /// Lifts out whoever of a settled party is still down there, and takes the party off the list. Returns how many were
    /// lifted; the rest walked out on their own. See <see cref="LeaveMs"/>.
    /// </summary>
    public int Lift()
    {
        var lifted = 0;

        for (var i = 0; i < _shares.Count; i++)
        {
            var share = _shares[i];

            if (share.Who is { Deleted: false } body && _deep != null && _deep.Holds(body.Location))
            {
                Surface(body);
                lifted++;
            }
            else if (share.Down)
            {
                WalkedOut++;
            }

            share.Down = false;
        }

        _parties.Remove(this);

        return lifted;
    }

    /// <summary>Puts one body back where the population lives, whatever it was doing.</summary>
    private static void Surface(BotMobile bot)
    {
        if (bot is not { Deleted: false })
        {
            return;
        }

        BotPopulation.Carry(bot);
        bot.Journey?.Finish();
    }

    /// <summary>What the pot owes this member so far. The leader's own is what a delve pays its caller.</summary>
    public int Owed(Mobile bot) => Of(bot)?.Credit ?? 0;

    private Share Of(Mobile bot)
    {
        for (var i = 0; i < _shares.Count; i++)
        {
            if (ReferenceEquals(_shares[i].Who, bot))
            {
                return _shares[i];
            }
        }

        return null;
    }

    private static int Purse(Mobile bot) => bot?.Backpack?.GetAmount(typeof(Gold)) ?? 0;

    /// <summary>
    /// Asked by <c>BotPopulation.Revive</c> before it does anything of its own: is this bot somebody's
    /// delver, and has its party taken care of it?
    /// </summary>
    public static bool Raise(BotMobile bot)
    {
        for (var i = 0; i < _parties.Count; i++)
        {
            if (_parties[i].Holds(bot))
            {
                return _parties[i].Fell(bot);
            }
        }

        return false;
    }

    /// <summary>
    /// One of a party was raised where it fell by a member's own means (<c>BotSquadRaise</c>): one of the party's raisings spent, and
    /// the member back down there and earning. Its floor follows its purse, which now holds what came back off its corpse — the coin
    /// it walked in with, not the party's to sweep.
    /// </summary>
    public static void Spent(BotMobile bot)
    {
        for (var i = 0; i < _parties.Count; i++)
        {
            var party = _parties[i];

            // Raised where it fell: a delver sent home that dies on the island is not the party's to raise or count.
            if (party.Ended || party.Of(bot) is not { } share || !BotUndertaker.Below(bot.Location))
            {
                continue;
            }

            share.Down = true;
            share.Floor = Purse(bot);
            party.Left = Math.Max(0, party.Left - 1);
            Raised++;

            logger.Information(
                "{Bot} is back on its feet in {Deep} where it fell, raised by one of its party; the party has {Left} raisings left",
                bot.Name,
                party._deep?.Name ?? "the deep",
                party.Left
            );

            return;
        }
    }

    /// <summary>Whether a company may raise this bot: not one of a party whose raisings are spent, which goes home by the rule.</summary>
    public static bool MayRaise(BotMobile bot)
    {
        for (var i = 0; i < _parties.Count; i++)
        {
            if (!_parties[i].Ended && _parties[i].Holds(bot) && BotUndertaker.Below(bot.Location))
            {
                return _parties[i].Left > 0;
            }
        }

        return true;
    }

    /// <summary>Whether this bot is still down there with a party that has not ended — not one its party sent home. See <c>BotUndertaker</c>.</summary>
    public static bool Below(Mobile bot)
    {
        for (var i = 0; i < _parties.Count; i++)
        {
            if (!_parties[i].Ended && _parties[i].Of(bot) is { Down: true })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether this bot is underground with a party right now.</summary>
    public static bool Delving(Mobile bot)
    {
        for (var i = 0; i < _parties.Count; i++)
        {
            if (_parties[i].Holds(bot) && !_parties[i].Ended)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// How many parties are down this dungeon right now.
    ///
    /// <para>
    /// <b>Because a dungeon has a spawn rate and it is the ceiling on everybody in it.</b> The ladder picks
    /// the richest dungeon a band can survive, which is the same answer for every band of about the same
    /// strength, so on the evening of 11.09.2026 all five guilds went into the Orc Caves at once: twenty-five
    /// bots in ten rooms, and between them they killed seventy things in twenty minutes where one party
    /// alone had killed twenty in eight. Nothing was wrong with any of them. They had eaten the cave.
    /// </para>
    /// </summary>
    public static int Inside(BotDungeon.Deep deep)
    {
        var count = 0;

        for (var i = 0; i < _parties.Count; i++)
        {
            if (!_parties[i].Ended && ReferenceEquals(_parties[i]._deep, deep))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// The watchdog. Walked on the delve module's own clock, and it exists for one failure: a delve whose
    /// errand was cancelled, whose leader was deleted, or which ended in some way nobody thought of, leaving
    /// bots in a place with no road out.
    /// </summary>
    public static void Watch()
    {
        var now = Core.TickCount;

        for (var i = _parties.Count - 1; i >= 0; i--)
        {
            var party = _parties[i];

            // A party that settled and is walking out: whoever is still below after LeaveMs is lifted, the rest walked.
            if (party.Ended)
            {
                if (now - (party._settledTick + LeaveMs) >= 0)
                {
                    var lifted = party.Lift();

                    if (lifted > 0)
                    {
                        Stranded += lifted;

                        logger.Information(
                            "{Lifted} of a settled party were still in {Deep} {Minutes} minutes after their delve ended and have been lifted out; the rest walked out by the mouth",
                            lifted,
                            party._deep?.Name ?? "a dungeon",
                            LeaveMs / 60000
                        );
                    }
                }

                continue;
            }

            // Two ways a party can be left behind, and neither of them is anything the delve itself does.
            // The leader was deleted, so nothing is advancing the errand; or the errand stopped being
            // advanced for some reason nobody thought of, and the party has outlived the longest delve the
            // shard allows by a clear margin.
            var gone = party.Leader is not { Deleted: false };
            var overrun = now - (party._bornTick + BotDelve.CapMs + GraceMs) >= 0;

            if (!gone && !overrun)
            {
                continue;
            }

            var down = party.Down;

            if (down > 0)
            {
                Stranded += down;

                logger.Warning(
                    "A party of {Down} was still in {Deep} {Minutes} minutes after going down, and {Why}; they have been lifted out",
                    down,
                    party._deep?.Name ?? "a dungeon",
                    (now - party._bornTick) / 60000,
                    gone ? "their leader is gone" : "nothing had ended the delve"
                );
            }

            party.Settle(gone ? "its leader is gone" : "it outlived its own delve");

            _parties.Remove(party);
        }
    }

    public static string Describe() =>
        Delves == 0
            ? "nobody has been down a dungeon"
            : $"{Delves} parties went down, {_parties.Count} are down now: {Taken}gp swept into their pots and {Paid}gp handed out, "
            + $"{Raised} raised where they fell out of {Raisings} to a party ({(FreeRaisings ? "for nothing" : "by a member's scroll, spell or bandage")}), {Lost} went home for want of one, {Unraised} with raisings left and nobody down there able to raise them, "
            + $"{WalkedOut} walked out by the mouth and {Stranded} had to be lifted out";

    public static void Forget()
    {
        _parties.Clear();
        Delves = 0;
        Taken = 0;
        Paid = 0;
        Raised = 0;
        Lost = 0;
        Unraised = 0;
        Stranded = 0;
        WalkedOut = 0;
    }
}
