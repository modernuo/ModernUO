using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A bot's life story: the three most important things that ever happened to it, in its own words with the real names and
/// numbers, and the counts behind them; written from the hooks where those things happen, and told at the fire and now and
/// then on the road.
///
/// <para>
/// <b>Patrick's idea of 07.10.2026: "a biography — memory of its three most important events (first death, who saved it, who
/// robbed it) that it tells at the campfire and in the trade chat".</b> Each event is a <see cref="BotTale"/> with a weight;
/// <see cref="Keep"/> holds one tale of a kind and the three heaviest kinds. The first death weighs 50; being saved 40 and up to
/// 20 more the nearer to death it was; being raised 38; being robbed under threat 40 and up, murdered 60, killed in a war 55;
/// the best sale 15 and up to 25 more by its size; a war fought 30 and two a kill; a duel 35; a veteran's title 60. So a bot
/// that has been murdered, saved at three hits and made veteran tells those, and its best sale of 40gp is let go.
/// </para>
///
/// <para>
/// <b>Written where it happens, never reconstructed.</b> <c>BotMobile.OnDeath</c> (<see cref="Died"/>), <c>BotRescue</c> on a rescue
/// ending well (<see cref="Rescued"/>), <c>BotRaise</c> (<see cref="Raised"/>), <c>BotSalve</c> (<see cref="Healed"/>), <c>BotRob</c>'s
/// paid demand (<see cref="Extorted"/>), <c>BotHandover</c> (<see cref="Sold"/>), <c>BotSlay</c> on a lone kill and <c>BotSquad</c> on a
/// company's (<see cref="Slew"/>, <see cref="CompanyKill"/>), and the grudge duels (<see cref="Dueled"/>). Each also moves the ties
/// (<see cref="BotTies"/>) and the kills that make a veteran (<see cref="BotTraits.Credit"/>).
/// </para>
///
/// <para>
/// <b>Told, counted.</b> At a fire or a meeting on the road one line in <see cref="TaleChance"/> is a tale or a word about a
/// friend or an enemy instead of the news (<c>BotCampTalk.Topic</c>); after a mood line somebody heard, one in
/// <see cref="VoiceChance"/> is followed by a tale in the local channel, and a tale of a sale goes to the trade room one time in
/// <see cref="TradeChance"/> (<c>BotVoice.Mood</c>).
/// </para>
/// </summary>
public static class BotBiography
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBiography));

    public static bool On { get; set; } = true;

    /// <summary>How many tales one bot keeps. Three.</summary>
    public static int MostTales { get; set; } = 3;

    /// <summary>The share of a fire's or a road's lines that are a tale or a word about a friend or an enemy. 0.3.</summary>
    public static double TaleChance { get; set; } = 0.3;

    /// <summary>After a mood line somebody heard, the chance of a tale in the local channel. 0.12.</summary>
    public static double VoiceChance { get; set; } = 0.12;

    /// <summary>The chance a tale of a sale told that way goes to the trade room rather than the local channel. 0.25.</summary>
    public static double TradeChance { get; set; } = 0.25;

    public static long Written { get; private set; }

    public static long PushedOut { get; private set; }

    public static long TooLight { get; private set; }

    public static long ToldAtFire { get; private set; }

    public static long ToldLocal { get; private set; }

    public static long ToldTrade { get; private set; }

    public static long FriendTales { get; private set; }

    public static long EnemyTales { get; private set; }

    public static double KillsCredited { get; private set; }

    private static readonly Dictionary<string, long> _byKind = new(StringComparer.Ordinal);

    /// <summary>
    /// Keeps a tale: replaces one of the same kind when at least as heavy, else takes a free place, else the lightest place when
    /// heavier than it. Returns whether it was kept.
    /// </summary>
    public static bool Keep(BotLife life, string kind, string other, string text, double weight, int number)
    {
        if (!On || life == null || string.IsNullOrEmpty(text))
        {
            return false;
        }

        var tales = life.Tales;
        var at = -1;

        for (var i = 0; i < tales.Count; i++)
        {
            if (tales[i].Kind == kind)
            {
                at = i;

                break;
            }
        }

        if (at >= 0 && tales[at].Weight > weight)
        {
            TooLight++;

            return false;
        }

        if (at < 0)
        {
            if (tales.Count < Math.Max(1, MostTales))
            {
                at = tales.Count;
                tales.Add(null);
            }
            else
            {
                var lightest = 0;

                for (var i = 1; i < tales.Count; i++)
                {
                    if (tales[i].Weight < tales[lightest].Weight)
                    {
                        lightest = i;
                    }
                }

                if (tales[lightest].Weight >= weight)
                {
                    TooLight++;

                    return false;
                }

                at = lightest;
                PushedOut++;
            }
        }

        tales[at] = new BotTale { Kind = kind, Other = other, Text = text, Weight = weight, Number = number, At = DateTime.UtcNow };
        Written++;
        _byKind[kind] = _byKind.GetValueOrDefault(kind) + 1;

        logger.Information("{Name} will remember this ({Kind}, weighing {Weight:F0}): \"{Text}\"", life.Name, kind, weight, text);

        return true;
    }

    /// <summary>Where a thing happened, as a bot says it: the dungeon or town region's name, else near the nearest town, else the spot.</summary>
    public static string Place(Map map, Point3D at)
    {
        if (map != null && map != Map.Internal)
        {
            var region = Region.Find(at, map);

            while (region != null && string.IsNullOrEmpty(region.Name))
            {
                region = region.Parent;
            }

            if (region != null && !string.IsNullOrEmpty(region.Name))
            {
                return $"in {region.Name}";
            }
        }

        var town = BotTowns.Nearest(at)?.Name;

        return town != null ? $"near {town}" : $"at ({at.X}, {at.Y})";
    }

    private static string Who(Mobile m) =>
        m == null ? "something" : m is BotMobile ? m.Name : BotVoice.Words(m.Name) ?? "something";

    /// <summary>
    /// It died. Its first death becomes a tale; a death at another bot's hand, out of a duel, makes that bot a personal enemy
    /// and a tale — in a war, the slayer is credited a kill and its war goes into its own story. Called from <c>BotMobile.OnDeath</c>.
    /// </summary>
    public static void Died(BotMobile victim, Mobile killer, Mobile slayer, Map map, Point3D at)
    {
        if (!On || BotLives.Of(victim) is not { } life)
        {
            return;
        }

        life.Deaths++;

        var place = Place(map, at);

        if (life.Deaths == 1)
        {
            Keep(life, "death", killer?.Name, $"The first time I died was {place}: {Who(killer)} killed me.", 50.0, 1);
        }

        if (slayer is not BotMobile other || ReferenceEquals(other, victim) || BotDuel.Between(victim, other))
        {
            return;
        }

        var ours = victim.Guild as Guild;
        var theirs = other.Guild as Guild;

        if (ours != null && theirs != null && ours != theirs && (BotWar.Of(ours.Name, theirs.Name) != null || BotRegard.AtWar(ours, theirs)))
        {
            BotTies.Move(victim, other, BotTies.WarKilled, $"{other.Name} of {theirs.Name} killed it in the war {place}");
            Keep(life, "wronged", other.Name, $"{other.Name} of {theirs.Name} killed me in the war {place}. I have not forgotten.", 55.0, 1);

            if (BotLives.Of(other) is { } slayerLife)
            {
                slayerLife.WarKills++;
                BotTraits.Credit(other, 1.0);
                KillsCredited += 1.0;

                Keep(
                    slayerLife,
                    "war",
                    ours.Name,
                    $"I fought for {theirs.Name} against {ours.Name}; {slayerLife.WarKills} of theirs fell to my hand, the last of them {victim.Name} {place}.",
                    30.0 + 2.0 * slayerLife.WarKills,
                    slayerLife.WarKills
                );
            }

            return;
        }

        life.Robbed++;
        BotTies.Move(victim, other, BotTies.Murdered, $"{other.Name} murdered it {place}");
        Keep(life, "wronged", other.Name, $"{other.Name} murdered me {place}, and went through my pack after.", 60.0, 1);
    }

    /// <summary>Saved from a creature by another bot that came to its cry. Called by <c>BotRescue</c> when the rescue ends well.</summary>
    public static void Rescued(Mobile rescuer, Mobile saved, Mobile foe, int lowest, int most)
    {
        if (!On || rescuer is not BotMobile || saved is not BotMobile || ReferenceEquals(rescuer, saved) || BotLives.Of(saved) is not { } life)
        {
            return;
        }

        life.SavedTimes++;

        var place = Place(saved.Map, saved.Location);
        var tie = BotTies.Move(saved, rescuer, BotTies.Saved, $"{rescuer.Name} saved it from {Who(foe)} {place} at {lowest} hits");

        if (tie != null)
        {
            tie.Saved++;
        }

        BotTies.Move(rescuer, saved, BotTies.Saver, $"it fought {Who(foe)} off {saved.Name} {place}");

        var share = most > 0 ? Math.Clamp(lowest / (double)most, 0.0, 1.0) : 1.0;

        Keep(
            life,
            "saved",
            rescuer.Name,
            $"{rescuer.Name} saved me {place} when {Who(foe)} had me at {lowest} hits.",
            40.0 + 20.0 * (1.0 - share),
            lowest
        );
    }

    /// <summary>Raised where it fell by another bot. Called by <c>BotRaise</c>.</summary>
    public static void Raised(Mobile raiser, BotMobile ghost, Point3D where)
    {
        if (!On || raiser is not BotMobile || ghost == null || ReferenceEquals(raiser, ghost) || BotLives.Of(ghost) is not { } life)
        {
            return;
        }

        life.RaisedTimes++;

        var place = Place(ghost.Map, where);
        var tie = BotTies.Move(ghost, raiser, BotTies.RaisedBy, $"{raiser.Name} raised it where it fell {place}");

        if (tie != null)
        {
            tie.Saved++;
        }

        BotTies.Move(raiser, ghost, BotTies.Raiser, $"it raised {ghost.Name} {place}");
        Keep(life, "raised", raiser.Name, $"{raiser.Name} raised me where I fell {place}. I owe that one.", 38.0, 1);
    }

    /// <summary>Mended by another bot. Called by <c>BotSalve</c> when the patient is whole.</summary>
    public static void Healed(Mobile healer, Mobile patient)
    {
        if (!On || healer is not BotMobile || patient is not BotMobile || ReferenceEquals(healer, patient))
        {
            return;
        }

        var tie = BotTies.Move(patient, healer, BotTies.Healed, $"{healer.Name} mended it");

        if (tie != null)
        {
            tie.Saved++;
        }
    }

    /// <summary>Made to pay under threat. Called by <c>BotRob</c> when a demand is paid.</summary>
    public static void Extorted(Mobile victim, Mobile robber, int amount)
    {
        if (!On || victim is not BotMobile || robber is not BotMobile || BotLives.Of(victim) is not { } life)
        {
            return;
        }

        life.Robbed++;

        var place = Place(victim.Map, victim.Location);
        var tie = BotTies.Move(victim, robber, BotTies.Extorted, $"{robber.Name} made it pay {amount}gp under threat {place}");

        if (tie != null)
        {
            tie.Wronged++;
        }

        Keep(life, "wronged", robber.Name, $"{robber.Name} made me pay {amount}gp {place}: my purse or my life, it said.", 40.0 + Math.Min(20.0, amount / 50.0), amount);
    }

    /// <summary>A sale hand to hand. Called by <c>BotHandover</c>; the best one becomes the seller's tale.</summary>
    public static void Sold(Mobile seller, Mobile buyer, int units, Type kind, int gold)
    {
        if (!On || seller is not BotMobile || buyer is not BotMobile)
        {
            return;
        }

        BotTies.Mutual(seller, buyer, BotTies.Traded, "a fair sale hand to hand");

        if (BotLives.Of(seller) is not { } life || gold <= life.BestSale)
        {
            return;
        }

        life.BestSale = gold;

        var item = BotShopkeep.Words(kind) ?? BotVoice.Words(kind?.Name) ?? "goods";

        Keep(life, "sale", buyer.Name, $"Best sale of my life: {units} {item} to {buyer.Name} for {gold}gp.", 15.0 + Math.Min(25.0, gold / 40.0), gold);
    }

    /// <summary>A creature felled by this bot alone. Called by <c>BotSlay</c> when its quarry goes down in the fight.</summary>
    public static void Slew(Mobile bot, Mobile quarry)
    {
        if (!On || bot is not BotMobile slayer || (slayer as IBotSquadMember)?.Squad != null)
        {
            return;
        }

        BotTraits.Credit(slayer, 1.0);
        KillsCredited += 1.0;
    }

    /// <summary>
    /// A company's kill: a share of one to each member standing, and a little regard between the leader and each of them for
    /// having fought side by side. Called by <c>BotSquad</c> when its focus is down.
    /// </summary>
    public static void CompanyKill(BotSquad squad)
    {
        if (!On || squad?.Members is not { Count: > 0 } members)
        {
            return;
        }

        var standing = 0;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is BotMobile { Deleted: false, Alive: true })
            {
                standing++;
            }
        }

        if (standing == 0)
        {
            return;
        }

        var share = 1.0 / standing;
        var leader = squad.Leader?.Self;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i]?.Self is not BotMobile { Deleted: false, Alive: true } member)
            {
                continue;
            }

            BotTraits.Credit(member, share);
            KillsCredited += share;

            if (leader is BotMobile && !ReferenceEquals(leader, member))
            {
                BotTies.Mutual(leader, member, BotTies.Beside, "a kill side by side in one company");

                if (BotLives.Of(member)?.Tie(leader.Name) is { } tie)
                {
                    tie.Beside++;
                }
            }
        }
    }

    /// <summary>A grudge fought out in a duel, for both of them. Called by <see cref="BotGrudge"/>.</summary>
    public static void Dueled(BotMobile winner, BotMobile loser, string how)
    {
        if (!On || winner == null || loser == null)
        {
            return;
        }

        if (BotLives.Of(winner) is { } won)
        {
            won.DuelsWon++;
            Keep(won, "duel", loser.Name, $"{loser.Name} and I had a score to settle, and I beat {loser.Name} in a duel ({how}).", 35.0, won.DuelsWon);
        }

        if (BotLives.Of(loser) is { } lost)
        {
            lost.DuelsLost++;
            Keep(lost, "duel", winner.Name, $"I called it out with {winner.Name} and lost the duel ({how}). We are square now.", 33.0, lost.DuelsLost);
        }
    }

    /// <summary>
    /// A tale, or a word about a friend or an enemy, for a speaker at a fire or on the road, one time in <see cref="TaleChance"/>:
    /// fills <c>{tale}</c>, <c>{friend}</c> or <c>{enemy}</c> and returns the occasion, or null to let the news be said.
    /// </summary>
    public static string Tale(Mobile speaker, Dictionary<string, string> fill)
    {
        if (!On || fill == null || Utility.RandomDouble() >= TaleChance || BotLives.Of(speaker) is not { } life)
        {
            return null;
        }

        // A word about a friend or an enemy one time in four, when there is one.
        if (Utility.Random(4) == 0)
        {
            var (friend, enemy) = BotTies.Strongest(life);

            if (enemy != null && (friend == null || Utility.Random(2) == 0))
            {
                fill["enemy"] = enemy.Name;
                EnemyTales++;
                ToldAtFire++;

                return "camp:enemy";
            }

            if (friend != null)
            {
                fill["friend"] = friend.Name;
                fill["why"] = friend.Saved > 0 ? $"Saved my skin {(friend.Saved == 1 ? "once" : $"{friend.Saved} times")}." : friend.Beside > 0 ? $"We've fought side by side {friend.Beside} times." : "";
                FriendTales++;
                ToldAtFire++;

                return "camp:friend";
            }
        }

        if (Next(life) is not { } tale)
        {
            return null;
        }

        fill["tale"] = tale.Text;
        ToldAtFire++;

        return "camp:tale";
    }

    /// <summary>The next tale to tell, round the three in turn so the same one is not told twice running.</summary>
    private static BotTale Next(BotLife life)
    {
        if (life.Tales.Count == 0)
        {
            return null;
        }

        life.ToldIndex = (life.ToldIndex + 1) % life.Tales.Count;

        return life.Tales[life.ToldIndex];
    }

    /// <summary>
    /// After a mood line somebody heard: one time in <see cref="VoiceChance"/>, a tale said in the local channel, or a tale of a
    /// sale in the trade room. Called by <c>BotVoice.Mood</c>.
    /// </summary>
    public static void AfterMood(BotMobile who)
    {
        if (!On || who == null || Utility.RandomDouble() >= VoiceChance || BotLives.Of(who) is not { Tales.Count: > 0 } life || Next(life) is not { } tale)
        {
            return;
        }

        var trade = tale.Kind == "sale" && Utility.RandomDouble() < TradeChance;
        var line = BotVoice.Phrase("character:tale", null, who, new Dictionary<string, string> { ["tale"] = tale.Text }) ?? tale.Text;

        if (!BotVoice.Say(who, trade ? "trade" : "local", line, false, "tale"))
        {
            return;
        }

        if (trade)
        {
            ToldTrade++;
        }
        else
        {
            ToldLocal++;
        }
    }

    public static string Describe()
    {
        var kinds = "";

        foreach (var (kind, n) in _byKind)
        {
            kinds += (kinds.Length == 0 ? "" : ", ") + $"{kind} {n}";
        }

        return !On
            ? "biographies are off"
            : $"{Written} events written into a life ({(kinds.Length == 0 ? "none" : kinds)}), {PushedOut} pushed a lighter one out, {TooLight} too light to keep; "
              + $"tales told: {ToldAtFire} at a fire or on the road ({FriendTales} of a friend, {EnemyTales} of an enemy), {ToldLocal} after a mood in the local channel, {ToldTrade} in the trade room; {KillsCredited:F0} kills credited";
    }

    public static void Forget()
    {
        Written = 0;
        PushedOut = 0;
        TooLight = 0;
        ToldAtFire = 0;
        ToldLocal = 0;
        ToldTrade = 0;
        FriendTales = 0;
        EnemyTales = 0;
        KillsCredited = 0;
        _byKind.Clear();
    }
}
