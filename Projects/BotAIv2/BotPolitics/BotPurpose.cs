using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// What a guild's chest saves for, by the members' vote: a hall, a house in Britain, a widening, a war chest, a ransom fund, or
/// nothing kept back at all.
///
/// <para>
/// <b>Patrick's order of 07.10.2026, night: "the guild chest with a vote on what we save for (a hall, a house, a war, a
/// ransom)".</b> Until now the goal was a rule: the hall's price while the guild had none, then a house in Britain if it wished
/// for one, else nothing (<c>BotDues.Goal</c>). The goal decides one thing only — how much of the chest is kept back from the
/// everyday draws that pay for members' supplies (<c>BotDues.Spare</c>: at most <c>BotDues.SaveShare</c> of what it holds,
/// never more than the goal) — so the vote moves no coin; it decides what the chest is slow to spend.
/// </para>
///
/// <para>
/// <b>The vote.</b> Every <see cref="EveryMs"/>, when the goal is reached, and when it stops making sense (the hall stands, the
/// war is over), the head proposes the first that applies of: the hall (none stands), a war chest of <see cref="WarChest"/>gp
/// (at war, or paying tribute), the house in Britain (the guild wishes for one), a ransom fund (members jailed, when
/// <see cref="BotPolitics.Ransom"/> is wired), a widening (the guild is full), or nothing. Each member votes by its own need:
/// the hall while there is none, the war chest if it fights and the guild is at war, the ransom when members are jailed,
/// nothing when it stands under the keep itself (the poor want the chest to pay for their supplies), and otherwise the head's
/// proposal. The most votes win; a tie goes to the head. Until a guild has voted the old rule stands.
/// </para>
/// </summary>
public static class BotPurpose
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPurpose));

    /// <summary>Whether members vote on what the chest saves for. Off, and the goal is the old rule.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often a guild votes again: two hours.</summary>
    public static int EveryMs { get; set; } = 7200000;

    /// <summary>A guild's first vote within this of the module starting, spread. Half an hour.</summary>
    public static int FirstMs { get; set; } = 1800000;

    /// <summary>What a war chest keeps back.</summary>
    public static int WarChest { get; set; } = 3000;

    public enum Aim
    {
        Hall,
        House,
        Widening,
        WarChest,
        Ransom,
        Nothing
    }

    private static readonly string[] _words = ["the hall", "a house in Britain", "a widening", "a war chest", "a ransom fund", "nothing"];

    private sealed class Ballot
    {
        public bool Voted;

        public Aim Aim;

        public Aim Proposed;

        public int Price;

        public long At;

        public int For;

        public int Of;

        /// <summary>Whether the chest has held the price since this vote: a goal is reached once, not every minute after.</summary>
        public bool Met;
    }

    private static readonly Dictionary<string, Ballot> _ballots = new(StringComparer.OrdinalIgnoreCase);

    private static readonly long[] _won = new long[6];

    private static readonly List<Guild> _guilds = [];

    /// <summary>Votes held, and of them won against the head's proposal; goals reached; votes called because a goal stopped making sense.</summary>
    public static long Held { get; private set; }

    public static long Overruled { get; private set; }

    public static long Reached { get; private set; }

    public static long Lapsed { get; private set; }

    public static string Word(Aim aim) => _words[(int)aim];

    /// <summary>
    /// What the members voted to save for and its price, once they have voted and while it still applies; false, and the old
    /// rule stands, otherwise. Asked by <c>BotDues.Goal</c>. "Nothing" is true with no price: the chest keeps nothing back.
    /// </summary>
    public static bool Goal(Guild guild, out string what, out int price)
    {
        what = null;
        price = 0;

        if (!Running || guild?.Name == null || !_ballots.TryGetValue(guild.Name, out var b) || !b.Voted || !Applies(guild, b.Aim))
        {
            return false;
        }

        price = Price(guild, b.Aim);
        what = price > 0 ? Word(b.Aim) : null;

        return true;
    }

    private static bool Applies(Guild guild, Aim aim) =>
        aim switch
        {
            Aim.Hall      => BotEstate.Running && BotEstate.Hall(guild) == null,
            Aim.House     => BotGuildHouses.Saving(guild) > 0,
            Aim.Widening  => BotGuilds.WidenBy > 0 && BotGuilds.Widened(guild) < BotGuilds.MostWidenings
                             && (guild.Members?.Count ?? 0) >= BotGuilds.Ceiling(guild),
            Aim.WarChest  => WarChest > 0 && (BotWar.Fighting(guild.Name) > 0 || BotTribute.Pays(guild.Name)),
            Aim.Ransom    => BotPolitics.RansomOf(guild) > 0,
            _             => true
        };

    private static int Price(Guild guild, Aim aim) =>
        aim switch
        {
            Aim.Hall     => BotEstate.Price,
            Aim.House    => BotGuildHouses.Saving(guild),
            Aim.Widening => BotGuilds.WidenPrice,
            Aim.WarChest => WarChest,
            Aim.Ransom   => BotPolitics.RansomOf(guild),
            _            => 0
        };

    /// <summary>The head's proposal: the first that applies, in the order of the class note.</summary>
    private static Aim Propose(Guild guild)
    {
        if (Applies(guild, Aim.Hall))
        {
            return Aim.Hall;
        }

        if (Applies(guild, Aim.WarChest))
        {
            return Aim.WarChest;
        }

        if (Applies(guild, Aim.House))
        {
            return Aim.House;
        }

        if (Applies(guild, Aim.Ransom))
        {
            return Aim.Ransom;
        }

        return Applies(guild, Aim.Widening) ? Aim.Widening : Aim.Nothing;
    }

    /// <summary>One member's vote, by its own need. See the class note.</summary>
    private static Aim Wish(BotMobile member, Guild guild, Aim proposed, bool hall, bool war, bool ransom)
    {
        if (hall)
        {
            return Aim.Hall;
        }

        if (war && BotElection.Fighter(member))
        {
            return Aim.WarChest;
        }

        if (ransom)
        {
            return Aim.Ransom;
        }

        return BotYield.Spendable(member) < BotEstate.Keep ? Aim.Nothing : proposed;
    }

    /// <summary>Once a minute: every guild whose vote is due votes.</summary>
    public static void Look(long now)
    {
        if (!Running)
        {
            return;
        }

        _guilds.Clear();

        foreach (var guild in BotGuilds.Standing)
        {
            _guilds.Add(guild);
        }

        for (var g = 0; g < _guilds.Count; g++)
        {
            var guild = _guilds[g];

            if (guild is not { Disbanded: false } || BotUnderworld.Band(guild) || guild.Members == null || guild.Members.Count == 0)
            {
                continue;
            }

            if (!_ballots.TryGetValue(guild.Name, out var b))
            {
                _ballots[guild.Name] = b = new Ballot
                {
                    At = now - Math.Max(0, EveryMs) + Math.Max(0, FirstMs) / 2 + Utility.Random(Math.Max(1, FirstMs))
                };
            }

            string why;

            if (now - b.At >= EveryMs)
            {
                why = b.Voted ? $"{EveryMs / 60000} minutes since the last vote" : "the first vote";
            }
            else if (b.Voted && !Applies(guild, b.Aim))
            {
                Lapsed++;
                why = $"{Word(b.Aim)} no longer applies";
            }
            else if (b.Voted && !b.Met && b.Price > 0 && BotChest.Holds(guild.Name) >= b.Price)
            {
                Reached++;
                why = $"the chest reached {Word(b.Aim)}'s {b.Price}gp";
            }
            else
            {
                continue;
            }

            Vote(guild, b, why, now);
        }
    }

    private static void Vote(Guild guild, Ballot b, string why, long now)
    {
        var proposed = Propose(guild);
        var hall = Applies(guild, Aim.Hall);
        var war = Applies(guild, Aim.WarChest);
        var ransom = Applies(guild, Aim.Ransom);

        Span<int> counts = stackalloc int[6];
        var voters = 0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } member)
            {
                counts[(int)Wish(member, guild, proposed, hall, war, ransom)]++;
                voters++;
            }
        }

        var win = proposed;

        for (var a = 0; a < counts.Length; a++)
        {
            if (counts[a] > counts[(int)win])
            {
                win = (Aim)a;
            }
        }

        var was = b.Voted ? b.Aim : (Aim)(-1);

        b.Voted = true;
        b.Aim = win;
        b.Proposed = proposed;
        b.Price = Price(guild, win);
        b.At = now;
        b.For = counts[(int)win];
        b.Of = voters;
        b.Met = b.Price > 0 && BotChest.Holds(guild.Name) >= b.Price;

        Held++;
        _won[(int)win]++;

        if (win != proposed)
        {
            Overruled++;
        }

        var head = BotGuilds.Head(guild);

        logger.Information(
            "{Guild} voted to save for {Aim}{Price}: {For} of {Of} members, {Head} had proposed {Proposed}; chest {Chest}gp — over {Why}",
            guild.Name,
            Word(win),
            b.Price > 0 ? $" ({b.Price}gp)" : "",
            b.For,
            voters,
            head?.Name ?? "nobody",
            Word(proposed),
            BotChest.Holds(guild.Name),
            why
        );

        if (win != was)
        {
            BotVoice.ToGuild(
                guild.Name,
                win == Aim.Nothing ? "politics:purpose-none" : "politics:purpose",
                new Dictionary<string, string> { ["purpose"] = Word(win), ["price"] = b.Price.ToString() }
            );

            BotEvents.Post("politics", head, $"{guild.Name} votes to save for {Word(win)}{(b.Price > 0 ? $" ({b.Price}gp)" : "")}: {b.For} of {voters}");
        }
    }

    // ---- telling -----------------------------------------------------------------------------------------------------------

    public static void Write(Utf8JsonWriter w, string guild)
    {
        if (guild == null || !_ballots.TryGetValue(guild, out var b) || !b.Voted)
        {
            w.WriteNull("purpose");

            return;
        }

        w.WriteString("purpose", Word(b.Aim));
        w.WriteNumber("purposePrice", b.Price);
        w.WriteNumber("purposeVotes", b.For);
        w.WriteNumber("purposeVoters", b.Of);
        w.WriteString("purposeProposed", Word(b.Proposed));
    }

    /// <summary>The <c>Treasuries:</c> line's clause.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "the chests' purpose is the rule's, no vote";
        }

        using var say = ValueStringBuilder.Create(384);

        say.Append(
            $"the chests' purpose by the members' vote every {EveryMs / 60000}m: {Held} votes ({_won[0]} the hall, {_won[1]} a house in Britain, {_won[2]} a widening, {_won[3]} a war chest of {WarChest}gp, {_won[4]} a ransom fund, {_won[5]} nothing kept back), {Overruled} against the head's proposal, {Reached} goals reached, {Lapsed} that stopped applying; now: "
        );

        var first = true;

        foreach (var (name, b) in _ballots)
        {
            if (!b.Voted)
            {
                continue;
            }

            say.Append(first ? "" : ", ");
            first = false;
            say.Append($"{name} {Word(b.Aim)} {b.For}/{b.Of}");
        }

        if (first)
        {
            say.Append("no vote yet");
        }

        return say.ToString();
    }

    // ---- the store ---------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;
        var voted = 0;

        foreach (var (_, b) in _ballots)
        {
            if (b.Voted)
            {
                voted++;
            }
        }

        writer.WriteEncodedInt(voted);

        foreach (var (name, b) in _ballots)
        {
            if (!b.Voted)
            {
                continue;
            }

            writer.Write(name);
            writer.WriteEncodedInt((int)b.Aim);
            writer.WriteEncodedInt((int)b.Proposed);
            writer.WriteEncodedInt(b.Price);
            writer.Write(Math.Max(0L, now - b.At));
            writer.WriteEncodedInt(b.For);
            writer.WriteEncodedInt(b.Of);
        }
    }

    internal static int Load(IGenericReader reader)
    {
        var now = Core.TickCount;
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString();
            var aim = reader.ReadEncodedInt();
            var proposed = reader.ReadEncodedInt();
            var price = reader.ReadEncodedInt();
            var ago = reader.ReadLong();
            var votes = reader.ReadEncodedInt();
            var of = reader.ReadEncodedInt();

            if (string.IsNullOrEmpty(name) || aim < 0 || aim > (int)Aim.Nothing || proposed < 0 || proposed > (int)Aim.Nothing)
            {
                continue;
            }

            _ballots[name] = new Ballot
            {
                Voted = true,
                Aim = (Aim)aim,
                Proposed = (Aim)proposed,
                Price = price,
                At = now - Math.Clamp(ago, 0L, (long)int.MaxValue),
                For = votes,
                Of = of,
                Met = true
            };
        }

        return count;
    }

    public static void Forget()
    {
        Held = 0;
        Overruled = 0;
        Reached = 0;
        Lapsed = 0;
        Array.Clear(_won);
    }

    /// <summary>
    /// Every chest's purpose forgotten, for Patrick's reset (07.10.2026, reset26): exactly what <see cref="Save"/> writes. A purpose
    /// is what a chest saves for, not coin set aside — the coin stays in the chest — so forgetting it moves no gold. See
    /// <see cref="BotPolitics.Wipe"/>. Returns the ballots forgotten.
    /// </summary>
    public static int Wipe()
    {
        var ballots = _ballots.Count;

        _ballots.Clear();
        _guilds.Clear();
        Forget();

        return ballots;
    }
}
