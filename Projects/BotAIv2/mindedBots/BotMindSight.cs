using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Engines.Craft;
using Server.Text;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.Mind;

/// <summary>
/// The world as one bot can see it, written out for the model.
///
/// <para>
/// <b>Every line here is a defect surface, and that is not a figure of speech.</b> The first version of this
/// wrote "Carrying 39 of 215 stones" — the engine's own unit for weight — and the model read it as cargo:
/// the first plan that bot ever made in its life was to walk to the market and sell its stones. Nothing was
/// wrong with the code, the data or the schema. The sentence was wrong. So each fact below is written the
/// way a person would say it, units named, and nothing is included that the bot could not actually be said
/// to know.
/// </para>
///
/// <para>
/// <b>A weight is not a cargo, and the very first live answer proved it again.</b> Told only that its pack
/// was "12% full by weight", the model chose to go and sell what it was carrying — of an empty pack, on a
/// bot that had just been born. The percentage was true and it was not the fact being asked about. So the
/// count of things is said beside it: how heavy the pack is answers whether there is room, and how many
/// things are in it answers whether there is anything to sell.
/// </para>
///
/// <para>
/// <b>And it is given what the shard already knows.</b> A mind told only "you are at 1440, 1470" will decide
/// to go and look for a forge while standing next to one — the second lesson of the first run. What the
/// population has already surveyed is a fact about the bot's world, so it goes in the state rather than
/// being left for the model to rediscover by walking.
/// </para>
/// </summary>
public static class BotMindSight
{
    /// <summary>How far round itself the bot is said to see.</summary>
    public static int Notice { get; set; } = 14;

    /// <summary>How many past outcomes are recited before the model chooses again.</summary>
    public static int Recall { get; set; } = 6;

    /// <summary>
    /// The standing instruction. Short on purpose: everything situational belongs in the state, where it can
    /// be measured against what happened, and a system prompt that carries facts is a system prompt that
    /// goes stale without anybody noticing.
    /// </summary>
    public static string System(BotMind mind) =>
        $"""
         You are the mind of {mind.Name}, a {mind.Trade} living on an Ultima Online shard among {Others()} other
         bots who work, trade and fight for a living. {Thinkers()} of the others think as you do and you can
         hear them; the rest work by instinct. You are not narrating a story and you are not talking to a
         person: you choose this bot's next piece of work and nothing else.

         The shard runs the work itself — walking, swinging, digging, buying. Your one decision is which trade
         to take up next, and how much you expect it to be worth, in gold-equivalent per minute. Skill and
         goods count towards that as well as coin.

         Every trade on the list has real work in it right now — that is why it is on the list — and which
         one is taken up is settled by the shard weighing the work itself. Your number changes nothing about
         that: it cannot win you the work and it cannot lose you the work. It is a forecast and only a
         forecast, checked afterwards against what the work actually paid, and you are shown the comparison.
         There is nothing to be gained by predicting low or high, so predict what you believe.

         Fighting and staying alive are not yours to decide. Those are reflexes and they happen without you.
         {Calling(mind)}
         {Charter()}
         """;

    /// <summary>
    /// What a maker is, over and above being a crafter — added on Patrick's order of 11.09.2026.
    ///
    /// <para>
    /// <b>Every mind on this shard is a crafter, and every crafter is the maker of a guild.</b> That was
    /// true before this paragraph and no mind knew it: they chose work for one body while being the only
    /// bots on the island able to point fifteen at one thing. What is added here is the knowledge and the
    /// three powers, and the powers are deliberately described as prices rather than orders, because that is
    /// what they are — a member with better work in front of it goes on doing that.
    /// </para>
    ///
    /// <para>
    /// The strategy is stated as questions rather than as a plan, because a plan in a system prompt is a
    /// plan that is right on the day it is written. What a guild needs — a hall, ground, a full purse, and
    /// neighbours who are not at war with it — is read off the state on every ask.
    /// </para>
    /// </summary>
    private static string Charter() =>
        """
        You are also the maker of your guild, which means you founded it and it cannot exist without you. A
        guild is worth something to its members when it has a hall, ground of its own, and money in the
        common purse; a member of a guild that has none of those may walk out, and does. So the guild is
        yours to build, and the questions are: what does it lack, what can it get next, and who among the
        other guilds is worth trading with or worth minding.

        With every choice you may also charge your whole band, in three ways, all optional:
        'gather' names a trade you want the band collecting at, 'make' names one you want them making at, and
        'marchx'/'marchy' name a spot on the map you want them all near. Each is a price, not an order: it
        makes that work worth half again to your members, and one with better work in front of it will
        ignore you. An order stands for a quarter of an hour and then lapses. Say 'none' for gather and make,
        and nought for the coordinates, when the band should carry on as it is — which is most of the time. A
        band charged with something new every few minutes finishes nothing.

        You also choose who is in the band. When 'expel' and 'recruit' appear in your answer, you may name
        one member to put out and one bot outside every band to take on; they are the only two lists you may
        name from, and each name comes with how far along its trade that bot is. A band that is short of
        members cannot raise a hall, because a hall is paid for by a levy on the members; a band carrying
        somebody who is learning nothing is carrying it. Say 'none' for either when you are content. Using
        the power rests it for half an hour, so spend it on the clearest case rather than the first one.

        The crafters share one market and one board, and you talk to each other through 'say' and read what
        the others said. Use 'say' for the market: what you will make, what you are short of, what you will
        leave to somebody else. When 'take' appears in your answer you may name one row of the board that you
        will fill; the others are told it is yours for a quarter of an hour, and a row somebody else has taken
        is theirs — leave it, unless they have gone quiet. Say 'none' when you are taking nothing.

        Your guild has a board of its own. With 'want' and 'wantamount' you may ask your band for a material —
        ore, logs, herbs, hides, feathers, meat or wool — and how much; the request stands for half an hour.
        Gatherers of your band will be worth half again for bringing it in, and where it comes off a creature
        the hunters of your band will go for the creatures that carry it. Ask for what the orders on the board
        actually eat, in the amount they eat; a board full of requests is a board nobody reads. Say 'none' and
        nought when the band should carry on.
        """;

    /// <summary>
    /// How many other bots there are, counted rather than written down.
    ///
    /// <para>
    /// It said "fourteen" for as long as this file has existed and the population has been thirty-three since
    /// the 19th. Nothing broke, which is the point: a fact hard-written into a system prompt goes stale
    /// silently, and the only symptom is a mind reasoning about a world half the size of the one it is in.
    /// </para>
    /// </summary>
    private static int Others() => Math.Max(0, BotPopulation.Bots.Count - 1);

    /// <summary>How many other minds are running. Same reasoning as <see cref="Others"/>.</summary>
    private static int Thinkers() => Math.Max(0, BotMinds.All.Count - 1);

    /// <summary>
    /// The paragraph that says what this particular bot is for, or nothing for the three whose trade is
    /// already the ordinary business of the shard.
    ///
    /// <para>
    /// <b>It exists for one mind, and writing it for all four would have been the mistake.</b> A warrior, a
    /// smith and a caster all live by the same rule the prompt above states — work is worth what it pays and
    /// the number you give is a forecast of that — so anything added for them would be flavour, and flavour
    /// in a system prompt is a fact that goes stale where nobody can see it. The Baron does not live by that
    /// rule at all: he is paid nothing, he hands away everything his company takes, and the thing he is
    /// actually trying to change is the state of the island. A mind told only the general rule would sit
    /// there predicting its own income, which is nought, for ever.
    /// </para>
    /// </summary>
    private static string Calling(BotMind mind) =>
        mind.Trade switch
        {
            "baron" =>
                """

                One more thing, and it is the whole of what makes you different from the others.

                You are a Baron. You are not making a living and you do not need money: you have a stipend,
                you take no share of anything your company kills, and every coin and every item off every
                corpse is divided among the five bots who came with you. That division is the only reason
                anybody follows you, so when you are asked what a harrowing is worth, the number you give is
                what you expect THEM to carry away, not what you will.

                What you are for is ground that has killed people. Squares where two or more have died stand
                on the board until somebody empties them, and nothing else on this island will: the others
                hunt where hunting pays, which is never the places that have proved they kill. You are the
                only bot who will walk into one on purpose, and you go with five behind you or you do not go.
                A harrowing ends when twenty things in it are dead or after forty minutes, and then that
                square comes off the board for good.

                When no ground is standing, walk your town. It pays nothing and it is meant to: it is where
                you are between harrowings, not a way of filling time you should feel bad about. Your only
                sorrow is the dead and the squares nobody has dealt with — never an empty purse, and never an
                idle afternoon.
                """,
            "crafter" =>
                """

                One more thing, and it is the whole of what makes you different from the others.

                YOU ARE ONE OF THE FOUR CRAFTERS, AND THERE ARE NO OTHERS. Nobody else on this island makes
                anything. Every weapon, every piece of armour, every arrow, every potion, every scroll and
                every meal is made by the four of you or it does not exist here. The others fight, gather and
                carry; you are the only ones who turn what they bring back into something worth having.

                READ YOUR STATE IN THIS ORDER, AND STOP AT THE FIRST THING YOU CAN DO.

                First, the board. A want there is a bot that has already paid for something it cannot make:
                the gold is lodged and waiting, so you are not gambling on being paid, you are collecting.
                Each row tells you which craft makes the thing, what skill it takes and what you have. If a
                row says you can make it now, that is your work and nothing on this list beats it. A want
                whose price has been raised again and again with nothing delivered is the loudest sentence
                this island can say: it has been trying to buy that for a long time and nobody could.

                Second, what the island is short of. Bare armour slots, shooters with nothing to shoot, bots
                that have not eaten — those numbers are counted at the moment you read them, and every one of
                them is a thing only you can fix. A bot with an empty slot is a bot that dies sooner; an
                archer with no arrows is a bow being carried for nothing. Nobody has ordered these. Make them
                anyway, sell them at the shop by Britain's bank, and the island pays you for them.

                Third, your skill, if it is what is stopping you. Skill counts as earnings on this shard, so
                an hour spent getting better is paid work, not a detour. Make the dearest thing you are
                allowed to attempt — your bench lists them — until the number moves. Do not practise a skill
                that is already at its target; it earns you nothing more.

                Fourth, material. You carry a pickaxe, an axe and the skill to use both, and the shops by the
                bank list what everything costs. Buy it there if you can afford it, or ask for it aloud and a
                carrier will bring it; dig or cut it if nobody has it, or if buying costs more than the
                finished thing is worth. Ore, wood, leather and cloth all reach you either way.

                Fifth, selling. A stack of ingots in your pack is worth nothing to anybody. The same ingots
                sold at the shop by the bank, carried to whoever asked, or turned into what somebody asked
                for, are the island getting richer, and that is what you are judged on. Everything is sold
                hand to hand.

                THREE RULES THAT OVERRIDE ALL OF THAT.

                Do not make what has already failed to sell. Your own goods tell you what you still hold that
                nobody is asking for. That is the island answering you in money, and it is worth more than any
                forecast you can make.

                Do not stand at a bench another crafter is already at. You are shown what the other three are
                holding right now. Four of you on one trade is three of you wasted, and the wants nobody can
                fill will still be sitting there afterwards.

                Do not take work for a thing nothing on this island can produce. The board hides those from
                you already; if you find yourself reasoning about one anyway, you have invented it.

                Hunt, dig or cut when it is the shortest road to what you are making, or when the board is
                empty and the island is short of nothing. Not as a way of filling an afternoon.
                """,
            _ => ""
        };

    /// <summary>
    /// The guild this bot leads: what it has, what it lacks, what it has been charged with, and how it
    /// stands with the others.
    ///
    /// <para>
    /// <b>Added 11.09.2026, with the powers that go with it.</b> A mind cannot build a guild it is never
    /// told the state of, and the three facts that decide everything about one — hall, ground, purse — were
    /// nowhere in the state. The neighbours are here too, worst opinion first, because the question "who
    /// should we mind" has an answer on this shard and it is a number.
    /// </para>
    ///
    /// <para>
    /// Kept short on purpose. Every block here costs prompt budget, and the debugger's own prompt strangled
    /// its model by growing quietly — see <see cref="LastChars"/>.
    /// </para>
    /// </summary>
    private static void Band(ref ValueStringBuilder sb, BotMobile body)
    {
        if (body?.Guild is not Server.Guilds.Guild guild)
        {
            sb.Append("\n\nYour band: you are in none.");

            return;
        }

        var hall = BotEstate.Hall(guild);
        var fund = BotEstate.Fund(guild);
        var ground = BotClaim.Holds(guild.Name);

        sb.Append("\n\nYour band, ");
        sb.Append(guild.Name);
        sb.Append(": ");
        sb.Append(guild.Members?.Count ?? 0);
        sb.Append(" of you, ");
        sb.Append(fund);
        sb.Append("gp in the common purse of the ");
        sb.Append(BotEstate.Price);
        sb.Append(" a hall costs, ");

        if (hall is { Deleted: false })
        {
            sb.Append("a hall at ");
            sb.Append(hall.X);
            sb.Append(',');
            sb.Append(hall.Y);
        }
        else
        {
            sb.Append("no hall");
        }

        sb.Append(", ");
        sb.Append(ground);
        sb.Append(" squares of ground held (");
        sb.Append(BotClaim.Free);
        sb.Append(" are free to take, then ");
        sb.Append(BotClaim.Price);
        sb.Append("gp each). Standing orders: ");
        sb.Append(BotCharter.Says(guild));
        sb.Append(". The guild's board asks for: ");
        sb.Append(BotCharter.BoardSays(guild));
        sb.Append('.');

        // What the whole population has found pays, so that a `want` on the board is grounded in something: the four
        // minds never used it in the hour after build 68, and this is the first thing they were never shown.
        var best = BotCommons.Best(5);

        if (best.Count > 0)
        {
            sb.Append(" What pays on this island lately, by everybody's record: ");

            for (var i = 0; i < best.Count; i++)
            {
                var (kind, _, where, perMinute, settled, _) = best[i];

                if (i > 0)
                {
                    sb.Append("; ");
                }

                sb.Append(kind);
                sb.Append(' ');
                sb.Append((int)perMinute);
                sb.Append("/min at ");
                sb.Append(where.X);
                sb.Append(',');
                sb.Append(where.Y);
                sb.Append(" on ");
                sb.Append(settled);
            }

            sb.Append('.');
        }

        // Who minds whom. The worst opinion of this guild is what decides whether anybody is about to
        // declare war on it, and it is the one relation a maker can do anything about.
        var worst = 0.0;
        string sourest = null;

        foreach (var other in BotGuilds.Standing)
        {
            if (other == null || other == guild)
            {
                continue;
            }

            var held = BotRegard.Of(other.Name, guild.Name);

            if (held < worst)
            {
                worst = held;
                sourest = other.Name;
            }
        }

        if (sourest != null)
        {
            sb.Append(" Of the other bands, ");
            sb.Append(sourest);
            sb.Append(" thinks worst of you at ");
            sb.Append(worst.ToString("F0", global::System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(" (war is declared below ");
            sb.Append(BotRegard.Enmity.ToString("F0", global::System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(").");
        }

        if (guild.Enemies is { Count: > 0 })
        {
            sb.Append(" You are at war with ");
            sb.Append(guild.Enemies.Count);
            sb.Append(" of them.");
        }

        Roll(ref sb, guild);
    }

    /// <summary>
    /// Who is in the band, who could be, and who you would be best rid of.
    ///
    /// <para>
    /// <b>Patrick's order of 11.09.2026: a leader may put members out and take others on.</b> The power is
    /// worth nothing without the two lists, and the lists are worth nothing without the number they were
    /// ranked by — so each name carries its own, and the sentence says what the number means. A model shown
    /// five names and no reason to prefer any of them picks the first one every time.
    /// </para>
    ///
    /// <para>
    /// Silent while the clock is still running, except for one clause saying how long: the alternative is a
    /// leader that names somebody every couple of minutes for twenty minutes and has every answer thrown
    /// away.
    /// </para>
    /// </summary>
    private static void Roll(ref ValueStringBuilder sb, Server.Guilds.Guild guild)
    {
        var waits = BotRoster.Waits(guild);

        sb.Append(" Your roll: ");
        sb.Append(guild.Members?.Count ?? 0);
        sb.Append(" of the ");
        sb.Append(BotGuilds.Most);
        sb.Append(" you may hold");

        if (waits > 0 || !BotRoster.Free(guild))
        {
            sb.Append("; you may change it again in ");
            sb.Append(waits);
            sb.Append(" min.");

            return;
        }

        sb.Append(". You may put one member out and take one bot on, and doing either rests the power for ");
        sb.Append(BotRoster.EveryMs / 60000);
        sb.Append(" min. Judge them by how far along their trade they are, 0 to 1.");

        Names(ref sb, " Weakest of yours: ", BotRoster.Weakest(guild, BotRoster.Shortlist));
        Names(ref sb, " In no band: ", BotRoster.Candidates(guild, BotRoster.Shortlist));
    }

    private static void Names(ref ValueStringBuilder sb, string lead, List<BotMobile> who)
    {
        if (who.Count == 0)
        {
            return;
        }

        sb.Append(lead);

        for (var i = 0; i < who.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(who[i].Name);
            sb.Append(" the ");
            sb.Append(who[i].Class?.Name ?? "nothing");
            sb.Append(" at ");
            sb.Append(who[i].Progress.ToString("F2", global::System.Globalization.CultureInfo.InvariantCulture));
        }

        sb.Append('.');
    }

    /// <summary>Everything about right now, as the question.</summary>
    /// <summary>
    /// How long the last state written was, in characters.
    ///
    /// <para>
    /// Watched rather than assumed. The debugger's prompt grew from 14780 to 19981 characters over a few
    /// sittings and quietly strangled the model it was written for — nothing failed, the answers simply got
    /// worse. Every block added here costs some of the same budget, so the number is printed in the minds'
    /// own summary where it can be seen growing.
    /// </para>
    /// </summary>
    public static int LastChars { get; private set; }

    /// <summary>
    /// Set true and the next state built is written out in full, once, to the minds' own log; it puts
    /// itself back to false afterwards.
    ///
    /// <para>
    /// <b>Because everything else about a prompt is inferred from what the model said back.</b> Four
    /// crafters in a row talked about sewing ringmail, which is forged, and the whole of an evening's
    /// diagnosis was guesswork about whether the state said something misleading, said nothing, or said the
    /// right thing to a model that ignored it. Those are three different faults with three different fixes
    /// and no way to tell them apart from the outside.
    /// </para>
    ///
    /// <para>
    /// It is a dial, so it is turned on through the debugger's door on a living shard —
    /// <c>dial BotMindSight.Dump true</c> — and one state appears in <c>bot-minds.log</c> a few seconds
    /// later. One, not a stream: a switch that stays on fills a log with megabytes of duplicated prompt and
    /// is exactly the kind of instrument that gets left on by accident.
    /// </para>
    /// </summary>
    public static bool Dump { get; set; }

    public static string State(BotMind mind, BotMobile body, IReadOnlyList<string> trades, IReadOnlyList<string> band = null)
    {
        var sb = ValueStringBuilder.Create(2048);

        try
        {
            Body(ref sb, body);
            Around(ref sb, body);
            Ground(ref sb, mind, body);
            Bench(ref sb, mind, body);
            Board(ref sb, mind, body);
            Fellows(ref sb, mind, body);
            Materials(ref sb, mind);
            Island(ref sb, mind, body);
            Shelf(ref sb, mind, body);
            Past(ref sb, mind);
            Heard(ref sb, mind);
            Lessons(ref sb, mind);
            Band(ref sb, body);
            Offers(ref sb, trades, band);

            sb.Append("\nChoose one trade from the list, say what you expect it to be worth per minute, how many minutes you expect to spend on it, and why in one sentence.");
            sb.Append(" You may also put one short line in `say` for the other thinking bots to read — something you have found, somewhere worth coming to, something you have given up on. Leave it empty unless it is worth their attention.");
            sb.Append(" And if your band should be doing something different, set `gather`, `make` or `marchx`/`marchy`; otherwise leave them at none and nought.");

            var state = sb.ToString();

            LastChars = state.Length;

            if (Dump)
            {
                Dump = false;

                BotMindLog.Write(mind.Name, $"the whole of what it reads, {state.Length} characters", state);
            }

            return state;
        }
        finally
        {
            sb.Dispose();
        }
    }

    private static void Body(ref ValueStringBuilder sb, BotMobile body)
    {
        var hits = body.HitsMax > 0 ? body.Hits * 100 / body.HitsMax : 0;
        var mana = body.ManaMax > 0 ? body.Mana * 100 / body.ManaMax : 0;
        var stamina = body.StamMax > 0 ? body.Stam * 100 / body.StamMax : 0;

        sb.AppendLine("YOURSELF");
        sb.Append("Health ");
        sb.Append(hits);
        sb.Append("%, stamina ");
        sb.Append(stamina);
        sb.Append("%, mana ");
        sb.Append(mana);
        sb.AppendLine("%.");

        var gold = body.Backpack?.TotalGold ?? 0;
        var banked = Banker.GetBalance(body);

        sb.Append("Money: ");
        sb.Append(gold);
        sb.Append(" gold in your pack and ");
        sb.Append(banked);
        sb.AppendLine(" gold in the bank.");

        // Named as room left rather than as a number of stones. See the note at the top of this file: the
        // engine's unit for weight was read as a cargo of rocks and cost a whole first run.
        var load = BotLadder.Load(body);
        var ceiling = Math.Max(1, BotLadder.Ceiling(body));
        var room = Math.Max(0, 100 - load * 100 / ceiling);

        sb.Append("Your pack is ");
        sb.Append(100 - room);
        sb.Append("% full by weight and holds ");
        sb.Append(body.Backpack?.Items.Count ?? 0);
        sb.AppendLine(" things.");

        var weapon = body.Weapon as Item;

        sb.Append("In hand: ");
        sb.AppendLine(weapon?.GetType().Name ?? "nothing but your fists");
    }

    private static void Around(ref ValueStringBuilder sb, BotMobile body)
    {
        sb.AppendLine("\nWHERE YOU ARE");

        var region = body.Region?.Name;

        sb.Append("Standing at ");
        sb.Append(body.Location.X);
        sb.Append(", ");
        sb.Append(body.Location.Y);

        if (!string.IsNullOrEmpty(region))
        {
            sb.Append(" in ");
            sb.Append(region);
        }

        sb.AppendLine(".");

        // Where this bot lives (29.09.2026), not the population's home. See BotPopulation.HomeOf.
        var home = BotPopulation.HomeOf(body);
        var away = (int)body.GetDistanceToSqrt(home);

        sb.Append("Your camp is ");
        sb.Append(away);
        sb.AppendLine(" tiles away.");

        // Towns forbid fighting outright, and a mind that does not know that will pick hunting inside one all
        // day and wonder why nothing ever comes of it.
        if (body.Region is Server.Regions.GuardedRegion)
        {
            sb.AppendLine("This is guarded ground: nothing can be fought here, but shops and a bank are at hand.");
        }

        var worst = BotThreat.Strongest(body, Notice);

        if (worst != null)
        {
            sb.Append("The most dangerous thing in sight is ");
            sb.Append(worst.Name);
            sb.Append(", about ");
            sb.Append((int)body.GetDistanceToSqrt(worst.Location));
            sb.AppendLine(" tiles off.");
        }
        else
        {
            sb.AppendLine("Nothing hostile is in sight.");
        }

        var friends = 0;
        var map = body.Map;

        if (map != null && map != Map.Internal)
        {
            foreach (var other in map.GetMobilesInRange<BotMobile>(body.Location, Notice))
            {
                if (other != body && other.Alive)
                {
                    friends++;
                }
            }
        }

        sb.Append(friends);
        sb.AppendLine(friends == 1 ? " of your own people is nearby." : " of your own people are nearby.");
    }

    /// <summary>
    /// The ground that has killed people and has not been dealt with — for the one mind whose whole subject
    /// it is.
    ///
    /// <para>
    /// <b>Written for the Baron and nobody else, because for everybody else it would be a distraction with
    /// coordinates in it.</b> A miner shown a list of deadly squares will reason about them; it has no
    /// errand that can touch one, so every word of that reasoning is spent. The Baron has exactly one errand
    /// and this is the whole of what it is about, and without it his state would say "the Baron trade has
    /// work in it" and nothing about why or where.
    /// </para>
    ///
    /// <para>
    /// Said as squares and the dead in them, never as the peril reading. The reading is a decaying frequency
    /// and it answers a different question — see <c>BotPeril</c> — and a mind given both numbers would be a
    /// mind asked to reconcile two rankings that are allowed to disagree.
    /// </para>
    /// </summary>
    private static void Ground(ref ValueStringBuilder sb, BotMind mind, BotMobile body)
    {
        if (mind.Trade != "baron")
        {
            return;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        sb.AppendLine("\nGROUND THAT HAS KILLED PEOPLE");

        var listed = 0;

        foreach (var (where, _, blows, deaths) in BotPeril.Worst(Squares))
        {
            if (deaths < BotPeril.Deadly)
            {
                continue;
            }

            listed++;

            sb.Append("- (");
            sb.Append(where.X);
            sb.Append(", ");
            sb.Append(where.Y);
            sb.Append("): ");
            sb.Append(deaths);
            sb.Append(" have died there and ");
            sb.Append(blows);
            sb.Append(" blows have landed, about ");
            sb.Append((int)body.GetDistanceToSqrt(where));
            sb.AppendLine(" tiles off.");
        }

        if (listed == 0)
        {
            sb.AppendLine("Nowhere. Nobody has died anywhere that is still standing on the board.");
        }
    }

    /// <summary>How many squares of dangerous ground are recited. A list, not a map.</summary>
    public static int Squares { get; set; } = 6;

    /// <summary>
    /// The facts a decision was made from, in one line, for the minds' log.
    ///
    /// <para>
    /// <b>Because a reason without its evidence cannot be checked.</b> The log records what a mind chose and
    /// the sentence it gave for choosing - and that sentence is the model's account of itself, which is
    /// exactly the thing least worth trusting. When a crafter says "the board is empty so I will sew", the
    /// question a person asks the next morning is whether the board really was empty, and until now the only
    /// way to answer it was to have been watching at the time.
    /// </para>
    ///
    /// <para>
    /// Kept to one line and to numbers the shard measured itself. The full state is available on demand
    /// through <see cref="Dump"/>; this is what is worth having on every decision, for ever.
    /// </para>
    /// </summary>
    public static string Brief(BotMind mind, BotMobile body)
    {
        if (body == null)
        {
            return "no body";
        }

        using var line = ValueStringBuilder.Create(320);

        var pack = body.Backpack?.TotalGold ?? 0;
        var load = BotLadder.Load(body);
        var ceiling = Math.Max(1, BotLadder.Ceiling(body));

        line.Append($"saw: {pack}gp in pack, {Banker.GetBalance(body)}gp banked, {load * 100 / ceiling}% loaded");

        if (mind.Trade == "crafter")
        {
            var canMake = 0;
            var cannot = 0;
            string best = null;
            var bestOffer = 0;

            var wants = BotMarket.Wants;

            for (var i = 0; i < wants.Count; i++)
            {
                var want = wants[i];

                if (want == null || want.Amount <= 0 || !BotShopper.Makeable(want.Kind))
                {
                    continue;
                }

                var recipe = BotShopper.MadeBy(want.Kind);
                var have = recipe == null ? 0.0 : body.Skills[recipe.Value.Skill]?.Base ?? 0.0;

                if (recipe != null && have >= recipe.Value.MinSkill)
                {
                    canMake++;

                    if (want.Offer > bestOffer)
                    {
                        bestOffer = want.Offer;
                        best = want.Label;
                    }
                }
                else
                {
                    cannot++;
                }
            }

            line.Append($"; board: {canMake} wants it could make now, {cannot} beyond its skill");

            if (best != null)
            {
                line.Append($", dearest reachable {best} at {bestOffer}gp");
            }

            // The shops by the bank since the stalls went (01.10.2026).
            var lots = BotShopkeep.Lots();
            var kinds = lots.Count;
            var cheapest = int.MaxValue;

            for (var i = 0; i < lots.Count; i++)
            {
                cheapest = Math.Min(cheapest, lots[i].Ask);
            }

            line.Append($"; shops by the bank: {kinds} lots from {(kinds == 0 ? 0 : cheapest)}gp");

            var minds = BotMinds.All;
            var others = 0;

            for (var i = 0; i < minds.Count; i++)
            {
                if (minds[i] != mind && minds[i]?.Body is { Deleted: false })
                {
                    if (others == 0)
                    {
                        line.Append("; others: ");
                    }
                    else
                    {
                        line.Append(", ");
                    }

                    others++;
                    line.Append($"{minds[i].Name} {minds[i].Body.Resolve?.Deed?.Kind ?? "-"}");
                }
            }
        }

        return line.ToString();
    }

    /// <summary>
    /// How many wants and how many kinds of stall a crafter is shown. Enough to choose between, few enough
    /// to leave room to think: the debugger's prompt grew to 19981 characters and choked the model it was
    /// written for, and that lesson is this project's, not a general one.
    /// </summary>
    public static int Rows { get; set; } = 8;

    /// <summary>
    /// What this bot can make, and how good it is at each of them.
    ///
    /// <para>
    /// <b>The skills are read off the body and the targets off the class, rather than either being written
    /// here.</b> A prompt that lists a trade's skills is wrong the day somebody adds one — Fletching was
    /// added to the crafter on 04.09.2026 for the arrows nobody could make, and a hand-written list would
    /// have gone on describing a crafter who could not fletch.
    /// </para>
    ///
    /// <para>
    /// Both numbers are given because they answer different questions: what it is at says whether the thing
    /// on the board can be made now, and how far it is from its target says whether an hour of practice is
    /// worth anything. A skill already at its target earns nothing further, and a mind that cannot see that
    /// keeps taking work it has stopped being paid for.
    /// </para>
    /// </summary>
    private static void Bench(ref ValueStringBuilder sb, BotMind mind, BotMobile body)
    {
        if (mind.Trade != "crafter")
        {
            return;
        }

        var klass = body.Class;

        if (klass?.Skills == null || klass.Skills.Count == 0)
        {
            return;
        }

        sb.AppendLine("\nWHAT YOU CAN MAKE, AND HOW GOOD YOU ARE AT IT");

        for (var i = 0; i < klass.Skills.Count; i++)
        {
            var (skill, target) = klass.Skills[i];
            var have = body.Skills[skill]?.Base ?? 0.0;

            sb.Append("- ");
            sb.Append(skill.ToString());

            // <b>The trade beside the skill, because the two are not the same word.</b> Told it had
            // Blacksmith 20 and shown a menu containing Smith and Tailor, a mind announced it would buy
            // cloth and make Ringmail Gloves as a tailor - ringmail is forged out of iron ingots, and the
            // engine would have refused every attempt. Same class of mistake as guessing a recipe: the fact
            // exists in the shard and simply was not in front of it.
            var bench = Bench(skill);

            if (bench != null)
            {
                sb.Append(" (the trade called ");
                sb.Append(bench);
                sb.Append(')');
            }

            sb.Append(": ");
            sb.Append((int)have);
            sb.Append(" of ");
            sb.Append((int)target);

            if (have >= target)
            {
                sb.AppendLine(" - as good as this trade asks. Practice pays you nothing more here.");
            }
            else if (have < 30.0)
            {
                sb.AppendLine(" - a beginner. Only the cheapest recipes come out, and half the material is lost on a failure.");
            }
            else
            {
                sb.AppendLine(" - getting better still pays.");
            }

            Recipes(ref sb, skill, have);
        }
    }

    /// <summary>
    /// Two or three things this bot could actually make with that skill right now, and how many there are
    /// in all.
    ///
    /// <para>
    /// <b>Because a mind with no list invents one.</b> Given a board, a bench and prices but never a recipe,
    /// four crafters in a row announced they would sew <i>bandages</i> — an item no craft system on this
    /// shard produces, and the one the order board had to be filtered for on 05.09.2026 for exactly the same
    /// reason. Their actual work was fine: the shard picks the recipe, not the mind. But an explanation that
    /// names something impossible is an explanation nobody can check, and a mind reasoning from an invented
    /// recipe will misjudge every forecast it makes about that trade.
    /// </para>
    ///
    /// <para>
    /// The examples are the ones at the top of what it can reach — the hardest it is allowed to attempt
    /// rather than the easiest, because the easiest is what it will be given anyway and says nothing about
    /// what the skill has bought.
    /// </para>
    /// </summary>
    /// <summary>
    /// What a thing of this name costs at the cheapest bot's shop by the bank holding one, or 0 if nobody is selling it (the
    /// cheapest stall until the auction went on 01.10.2026). Matched on the type's name, which is what the mind is shown.
    /// </summary>
    private static int Cheapest(string material)
    {
        if (string.IsNullOrEmpty(material))
        {
            return 0;
        }

        var lots = BotShopkeep.Lots();
        var best = 0;

        for (var i = 0; i < lots.Count; i++)
        {
            if (lots[i].Kind?.Name != material)
            {
                continue;
            }

            if (best == 0 || lots[i].Ask < best)
            {
                best = lots[i].Ask;
            }
        }

        return best;
    }

    /// <summary>
    /// Which trade on the work menu is made with this skill, or null for the skills that make nothing —
    /// Mining and Lumberjacking fetch material, and no bench belongs to them.
    ///
    /// Answered by asking the craft systems what they make with it rather than out of a table written here,
    /// so it cannot drift from <see cref="BotShopper.MadeBy"/>, which the board rows use.
    /// </summary>
    private static string Bench(SkillName skill)
    {
        CraftSystem[] systems = [BotAnvil.System, BotThread.System, BotFlask.System, BotFletching.System, BotQuill.System];

        for (var i = 0; i < systems.Length; i++)
        {
            var recipes = systems[i]?.CraftItems;

            if (recipes == null)
            {
                continue;
            }

            for (var r = 0; r < recipes.Count; r++)
            {
                var recipe = recipes[r];
                var needs = recipe?.Skills?.Count > 0 ? recipe.Skills[0] : null;

                if (needs?.SkillToMake != skill)
                {
                    continue;
                }

                return BotShopper.MadeBy(recipe.ItemType)?.Trade;
            }
        }

        return null;
    }

    private static void Recipes(ref ValueStringBuilder sb, SkillName skill, double have)
    {
        CraftSystem[] systems = [BotAnvil.System, BotThread.System, BotFlask.System, BotFletching.System, BotQuill.System];

        var can = 0;
        var shown = 0;

        for (var i = 0; i < systems.Length; i++)
        {
            var recipes = systems[i]?.CraftItems;

            if (recipes == null)
            {
                continue;
            }

            for (var r = 0; r < recipes.Count; r++)
            {
                var recipe = recipes[r];
                var needs = recipe?.Skills?.Count > 0 ? recipe.Skills[0] : null;

                if (needs == null || needs.SkillToMake != skill || needs.MinSkill > have)
                {
                    continue;
                }

                can++;

                if (shown >= 3)
                {
                    continue;
                }

                shown++;

                sb.Append(shown == 1 ? "    you can make: " : ", ");
                sb.Append(recipe.NameString ?? recipe.ItemType?.Name ?? "something");

                // <b>The first one carries its material and what that costs, and nothing else does.</b>
                // "Practise on the cheap things" is not an instruction a mind can act on until it knows what
                // the cheap thing is made of and whether that is on a stall - the crafters spent an evening
                // announcing they would buy cloth for recipes that eat leather. One recipe's worth, because
                // this line is read on every decision and a full bill of materials would crowd out the board.
                if (shown == 1)
                {
                    var eats = BotShopper.MadeBy(recipe.ItemType);

                    if (eats?.Material != null && eats.Value.Needs > 0)
                    {
                        sb.Append(" (eats ");
                        sb.Append(eats.Value.Needs);
                        sb.Append(' ');
                        sb.Append(eats.Value.Material);

                        var price = Cheapest(eats.Value.Material);

                        if (price > 0)
                        {
                            sb.Append(", on the stalls from ");
                            sb.Append(price);
                            sb.Append("gp");
                        }
                        else
                        {
                            sb.Append(", none on any stall");
                        }

                        sb.Append(')');
                    }
                }
            }
        }

        if (shown == 0)
        {
            // <b>Two different silences.</b> Mining at 50 and Tinkering at 0 both make nothing, and only one
            // of those is about the number: mining fetches material and has no bench at all. Printing the
            // same sentence for both told a crafter its best skill was useless.
            sb.AppendLine(
                Bench(skill) == null
                    ? "    nothing is made with this skill - it fetches material."
                    : "    you cannot make anything with this skill yet."
            );

            return;
        }

        if (can > shown)
        {
            sb.Append(" and ");
            sb.Append(can - shown);
            sb.Append(" others");
        }

        sb.AppendLine(".");
    }

    /// <summary>
    /// The wants on the board: what the island has paid for and cannot get.
    ///
    /// <para>
    /// <b>Filtered to what the shard is able to make at all.</b> The board once carried bandages, which no
    /// craft system here produces, and 3982 attempts were spent walking towards them before
    /// <c>BotShopper.Makeable</c> was written. Showing a mind an order nothing can fill is inviting it to
    /// spend its afternoons on the one thing guaranteed to fail.
    /// </para>
    ///
    /// <para>
    /// <b>The raises are the point of the row, not the price.</b> A want whose offer has been lifted five
    /// times with nothing delivered is the island saying it has been trying to buy this for half an hour.
    /// That is a stronger signal than any number the auction produces, and nothing else in the state
    /// carries it.
    /// </para>
    /// </summary>
    private static void Board(ref ValueStringBuilder sb, BotMind mind, BotMobile body)
    {
        if (mind.Trade != "crafter")
        {
            return;
        }

        sb.AppendLine("\nTHE BOARD: WHAT THE ISLAND HAS PAID FOR AND CANNOT GET");

        var wants = BotMarket.Wants;
        var shown = 0;
        var unmakeable = 0;

        // The rows this mind may claim, rebuilt with the board: the schema names them from here.
        mind.Orders.Clear();

        // <b>Wants for the same thing at the same price are one row.</b> Eight bots each wanting one leather
        // bustier arm posted eight orders, and the eight identical lines filled the whole board a mind is
        // shown - so a scroll nobody could write and a chest worth twice as much were pushed off the end of
        // a list that was, read literally, one item long.
        var seen = new Dictionary<(string Label, int Offer), (int Wanted, int Orders, int Coming, int Filled, int Raises, Type Kind)>();

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want == null || want.Amount <= 0)
            {
                continue;
            }

            if (!BotShopper.Makeable(want.Kind))
            {
                unmakeable++;

                continue;
            }

            var key = (want.Label, want.Offer);

            if (seen.TryGetValue(key, out var had))
            {
                seen[key] = (had.Wanted + want.Amount, had.Orders + 1, had.Coming + want.Coming, had.Filled + want.Filled, Math.Max(had.Raises, want.Raises), had.Kind);
            }
            else
            {
                seen[key] = (want.Amount, 1, want.Coming, want.Filled, want.Raises, want.Kind);
            }
        }

        foreach (var ((label, offer), row) in seen)
        {
            if (shown >= Rows)
            {
                break;
            }

            shown++;

            if (!mind.Orders.Contains(label))
            {
                mind.Orders.Add(label);
            }

            sb.Append("- ");
            sb.Append(row.Wanted);
            sb.Append(" x ");
            sb.Append(label);

            // Whose it is, if anybody has said. See BotMindClaims: a note the others read, not a lock.
            var (holder, ago) = BotMindClaims.Holder(label);

            if (holder != null)
            {
                sb.Append(string.Equals(holder, mind.Name, global::System.StringComparison.OrdinalIgnoreCase)
                    ? $" [yours, taken {ago / 60} min ago]"
                    : $" [taken by {holder} {ago / 60} min ago — theirs]");
            }

            sb.Append(" at ");
            sb.Append(offer);
            sb.Append("gp each, paid by hand when it is brought, ");
            sb.Append(row.Coming);
            sb.Append(" on their way already");

            if (row.Orders > 1)
            {
                sb.Append(", across ");
                sb.Append(row.Orders);
                sb.Append(" separate orders");
            }

            if (row.Filled > 0)
            {
                sb.Append(", ");
                sb.Append(row.Filled);
                sb.Append(" delivered so far");
            }

            if (row.Raises > 0)
            {
                sb.Append(". The price has been raised ");
                sb.Append(row.Raises);
                sb.Append(row.Raises == 1 ? " time" : " times");
                sb.Append(row.Filled > 0 ? " and it is still short" : " and nothing has ever been delivered");
            }

            // <b>Which craft, and what it takes.</b> Without this line a mind reasons about the recipe from
            // the name of the item and the name of its own class: the first thinking crafter this shard ever
            // had announced it would forge a Mind Blast Scroll. The recipe is read out of the engine's own
            // craft systems, so it cannot disagree with what the bot will actually be allowed to attempt.
            var recipe = BotShopper.MadeBy(row.Kind);

            if (recipe != null)
            {
                var fact = recipe.Value;
                var have = body.Skills[fact.Skill]?.Base ?? 0.0;

                sb.Append(". Made by ");
                sb.Append(fact.Craft);
                sb.Append(" - take the trade called ");
                sb.Append(fact.Trade);
                sb.Append(" - needs ");
                sb.Append(fact.Skill.ToString());
                sb.Append(' ');
                sb.Append((int)fact.MinSkill);
                sb.Append("; you have ");
                sb.Append((int)have);
                sb.Append(have >= fact.MinSkill ? " and can make it now" : " and cannot make it yet");

                // What one costs in material, so the offer can be judged against the price of the stuff it
                // is made of rather than admired on its own. Without it a crafter cannot tell an order worth
                // taking from one that pays less than the ingots.
                if (fact.Material != null && fact.Needs > 0)
                {
                    sb.Append(". Each one eats ");
                    sb.Append(fact.Needs);
                    sb.Append(' ');
                    sb.Append(fact.Material);
                }
            }

            sb.AppendLine(".");
        }

        if (shown == 0)
        {
            sb.AppendLine("Nothing. Nobody has paid for anything you could make - so make what sells, or go and get material.");
        }
        else
        {
            sb.AppendLine("Name one row in 'take' to tell the other crafters it is yours; a row marked theirs is spoken for.");
        }

        if (unmakeable > 0)
        {
            sb.Append("(");
            sb.Append(unmakeable);
            sb.AppendLine(" more wants are for things no craft on this island can produce. They are not yours; leave them.)");
        }
    }

    /// <summary>
    /// What the other crafters are doing at this moment.
    ///
    /// <para>
    /// <b>Because four minds given the same state make the same choice.</b> In the first minutes of the four
    /// thinking crafters, all four read the same stalls, the same empty board and the same beginner's skill
    /// list, and all four chose to buy cloth and sew shirts — one island, four bots, one trade, and the
    /// wants nobody could fill still sitting there. Nothing was wrong with any single decision. The state
    /// simply contained no fact about the other three.
    /// </para>
    ///
    /// <para>
    /// It says what they hold rather than what they intend. An intention is a forecast and can be
    /// abandoned in the next second; the deed a bot is holding is what is actually happening on the island.
    /// </para>
    /// </summary>
    private static void Fellows(ref ValueStringBuilder sb, BotMind mind, BotMobile body)
    {
        if (mind.Trade != "crafter")
        {
            return;
        }

        var minds = BotMinds.All;
        var said = 0;

        sb.AppendLine("\nTHE OTHER CRAFTERS, AND WHAT THEY ARE DOING NOW");

        for (var i = 0; i < minds.Count; i++)
        {
            var other = minds[i];

            if (other == null || other == mind || other.Trade != "crafter")
            {
                continue;
            }

            var fellow = other.Body;

            if (fellow is not { Deleted: false })
            {
                continue;
            }

            said++;

            sb.Append("- ");
            sb.Append(other.Name);
            sb.Append(": ");
            sb.Append(fellow.Resolve?.Deed?.Kind ?? "nothing yet");

            var (row, ago) = BotMindClaims.Of(other.Name);

            if (row != null)
            {
                sb.Append(", and has taken the order for ");
                sb.Append(row);
                sb.Append(" (");
                sb.Append(ago / 60);
                sb.Append(" min ago)");
            }

            sb.AppendLine("");
        }

        if (said == 0)
        {
            sb.AppendLine("Nobody. You are the only crafter working on this island right now.");
        }
        else
        {
            sb.AppendLine("Four of you share one island. Two at the same bench is one bench wasted.");
        }
    }

    /// <summary>
    /// What is on the stalls, and what it costs - the answer to "where would I get one".
    ///
    /// <para>
    /// A crafter that can only dig is a crafter that stops when the seam does. Buying is the other half of
    /// getting hold of a material, and the market is the one place on this shard where the price of a thing
    /// is a fact rather than a guess. The cheapest of each kind, because that is what the bot would actually
    /// pay, and the count beside it because one ingot and two hundred ingots are different worlds.
    /// </para>
    /// </summary>
    private static void Materials(ref ValueStringBuilder sb, BotMind mind)
    {
        if (mind.Trade != "crafter")
        {
            return;
        }

        sb.AppendLine("\nWHAT IS FOR SALE AT THE SHOPS BY BRITAIN'S BANK, AND WHAT IT COSTS");
        // <b>Written the other way round three hours earlier, and it was false.</b> The line said "gold in the
        // bank buys nothing until you have been to fetch it", which is a true fact about an older version of
        // this shard and about nothing else: both a bot's shop's till and BotShops draw the shortfall straight out
        // of the account (Banker.Withdraw) precisely so that a rich bot is never unable to buy a bandage. A
        // stale fact in a prompt is worse than a missing one - the mind reasons carefully from it, and there
        // is nothing in its own experience that can contradict it.
        sb.AppendLine("You pay from your pack, and whatever it is short of is taken from your bank account automatically. Both purses spend.");

        var lots = BotShopkeep.Lots();
        var seen = new Dictionary<string, (int Count, int Cheapest)>();

        for (var i = 0; i < lots.Count; i++)
        {
            var label = BotMarket.Spaced(lots[i].Kind.Name);

            seen[label] = seen.TryGetValue(label, out var had)
                ? (had.Count + 1, Math.Min(had.Cheapest, lots[i].Ask))
                : (1, lots[i].Ask);
        }

        if (seen.Count == 0)
        {
            sb.AppendLine("Nothing at all is on sale at the shops. Whatever you need, you will have to get yourself or ask for aloud.");

            return;
        }

        var listed = 0;

        foreach (var (label, what) in seen)
        {
            if (listed >= Rows)
            {
                break;
            }

            listed++;

            sb.Append("- ");
            sb.Append(label);
            sb.Append(": ");
            sb.Append(what.Count);
            sb.Append(what.Count == 1 ? " shop, from " : " shops, from ");
            sb.Append(what.Cheapest);
            sb.AppendLine("gp.");
        }

        if (seen.Count > listed)
        {
            sb.Append("(and ");
            sb.Append(seen.Count - listed);
            sb.AppendLine(" other kinds of thing at other shops.)");
        }
    }

    /// <summary>
    /// What the population is short of at this moment: bare armour slots, shooters with nothing to shoot,
    /// bots that have not eaten.
    ///
    /// <para>
    /// <b>Counted now, never accumulated.</b> Every subsystem here keeps a running total — <c>BotArms.Dry</c>,
    /// <c>BotArmourer.Standing</c>, <c>BotMeal.Empty</c> — and every one of those is the wrong number to show
    /// a mind. A total that has been growing since boot reads as the state of this minute and is not: the
    /// clearest case this shard has produced was "848 could not afford armour", entirely true, entirely about
    /// the first five minutes of a session hours old. So this walks the population and counts what is true
    /// right now, which costs one pass over forty-nine bots four times a minute.
    /// </para>
    ///
    /// <para>
    /// <b>This is the block that makes "work for the good of the island" a fact rather than an exhortation.</b>
    /// A crafter told only to be useful will decide what useful means; a crafter shown that eleven of thirteen
    /// shooters have no arrows knows what to make before lunch. Every test used here is the same call the
    /// shard itself makes — <see cref="BotArms.Stocked"/>, <see cref="BotMeal.IsFed"/>,
    /// <see cref="BotHarness.Layers"/> — rather than a second opinion written for the prompt.
    /// </para>
    /// </summary>
    private static void Island(ref ValueStringBuilder sb, BotMind mind, BotMobile body)
    {
        if (mind.Trade != "crafter")
        {
            return;
        }

        var bots = BotPopulation.Bots;
        var layers = BotHarness.Layers;

        var alive = 0;
        var bare = 0;
        var slots = 0;
        var shooters = 0;
        var dry = 0;
        var hungry = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var other = bots[i];

            if (other is not { Deleted: false, Alive: true })
            {
                continue;
            }

            alive++;

            var open = 0;

            for (var l = 0; l < layers.Count; l++)
            {
                if (other.FindItemOnLayer(layers[l]) == null)
                {
                    open++;
                }
            }

            if (open > 0)
            {
                bare++;
                slots += open;
            }

            if (other.Class?.Kit.Ranged is { Count: > 0 })
            {
                shooters++;

                if (!BotArms.Stocked(other, other.Class))
                {
                    dry++;
                }
            }

            if (!BotMeal.IsFed(other))
            {
                hungry++;
            }
        }

        sb.AppendLine("\nWHAT THE ISLAND IS SHORT OF, COUNTED THIS MOMENT");

        sb.Append("- Armour: ");
        sb.Append(bare);
        sb.Append(" of ");
        sb.Append(alive);
        sb.Append(" bots have a bare slot, ");
        sb.Append(slots);
        sb.AppendLine(" empty slots between them.");
        sb.AppendLine("  Metal armour is forged (the trade called Smith) out of ingots; leather and cloth armour is sewn (the trade called Tailor). Nobody else can make either.");

        // <b>Said as a state, never as a count of a negative.</b> "0 of 12 shooters have nothing to shoot"
        // was read by the model as an emergency about arrows, and it is the sentence for everything being
        // fine. A number whose good value is zero has to be phrased so that zero reads as good.
        sb.Append("- Arrows: ");

        if (shooters == 0)
        {
            sb.AppendLine("nobody on this island shoots.");
        }
        else if (dry == 0)
        {
            sb.Append("every one of the ");
            sb.Append(shooters);
            sb.AppendLine(" shooters has something to shoot. Nothing wanted here.");
        }
        else
        {
            sb.Append(dry);
            sb.Append(" of ");
            sb.Append(shooters);
            sb.AppendLine(" shooters are out of ammunition. Arrows are made by fletching (the trade called Fletcher) out of boards and feathers, and nobody but a crafter can make one.");
        }

        // <b>Named as a comfort rather than a famine, and with the craft that answers it.</b> Told "49 of 49
        // have not eaten recently", the model decided the island was starving and set about sewing hats for
        // the hungry. Nobody starves on this shard - a meal quickens recovery and lifts mood, and that is the
        // whole of it - and hats are not food. Both halves of that confusion were in the sentence.
        sb.Append("- Food: ");
        sb.Append(hungry);
        sb.Append(" of ");
        sb.Append(alive);
        sb.AppendLine(" have not eaten in the last while. Nobody starves here - a meal only quickens recovery and lifts the mood - and food is cooked (the trade called Cook) out of raw meat and flour.");
    }

    /// <summary>
    /// This bot's own goods to sell: what it holds over its keep, in pack and bank box, and how much of it the island is asking
    /// for. Its own stalls until the auction went on 01.10.2026; the goods stay with their maker now, and are sold at its shift by
    /// the bank or carried to a want.
    /// </summary>
    private static void Shelf(ref ValueStringBuilder sb, BotMind mind, BotMobile body)
    {
        if (mind.Trade != "crafter")
        {
            return;
        }

        var stock = BotShopkeep.Look(body);

        sb.AppendLine("\nYOUR OWN GOODS TO SELL");

        if (stock.Units == 0)
        {
            sb.AppendLine("You hold nothing to sell. Nothing you have made is waiting for a buyer.");

            return;
        }

        sb.Append("You hold ");
        sb.Append(stock.Units);
        sb.Append(" things of ");
        sb.Append(stock.Kinds);
        sb.Append(stock.Kinds == 1 ? " kind" : " kinds");
        sb.Append(" worth ");
        sb.Append(stock.Worth);
        sb.Append("gp at your prices, ");
        sb.Append(stock.Demanded);
        sb.AppendLine("gp of it what the island is short of or somebody has asked for. Sell it at the shop by Britain's bank or carry it to whoever asked; make what is asked for, not more of what is not.");
    }

    private static void Past(ref ValueStringBuilder sb, BotMind mind)
    {
        var past = mind.Past;

        if (past.Count == 0)
        {
            sb.AppendLine("\nWHAT YOUR CHOICES HAVE COME TO\nNothing yet: this is your first decision.");

            return;
        }

        sb.AppendLine("\nWHAT YOUR CHOICES HAVE COME TO");

        var from = Math.Max(0, past.Count - Recall);

        for (var i = from; i < past.Count; i++)
        {
            var done = past[i];

            sb.Append("- ");
            sb.Append(done.Trade);

            // A rate is only quoted where there is one. Told "you expected 250 a minute and got 0 a minute"
            // about six seconds of walking, a mind has been handed a false comparison and will reason from
            // it — so a short piece of work is reported as what it was, in seconds and in gold, and left
            // uncompared.
            if (done.Long)
            {
                sb.Append(": you expected ");
                sb.Append(done.Expected, "F0");
                sb.Append(" a minute, it came to ");
                sb.Append(done.Measured, "F0");
                sb.Append(" a minute over ");
                sb.Append(done.Minutes, "F1");
                sb.Append(" minutes (");
                sb.Append(done.Ending);
                sb.AppendLine(").");
            }
            else
            {
                sb.Append(": ");
                sb.Append(done.Ending);
                sb.Append(" after only ");
                sb.Append(done.Minutes * 60, "F0");
                sb.Append(" seconds with ");
                sb.Append(done.Gained);
                sb.AppendLine(" gold — too short to be worth anything a minute either way.");
            }
        }
    }

    // Where a section about empty trades used to be, and the note is worth more than the section was.
    //
    // The menu lists what exists, not what is available, and this tried to bridge the gap in words: "nothing
    // came of it 12 seconds ago; choosing it again now will most likely come to nothing again." The model
    // read it back to front — "the peddler trade was only attempted seconds ago, making it the highest
    // probability for quick coin conversion" — and chose the empty trade seven times running, citing the
    // warning each time as its reason. The gap is now closed where it cannot be reinterpreted: an empty
    // trade is left off the list the sampler is constrained to. See BotMind.Menu.
    //
    // And the gap itself is gone as well, which is the better half of the fix. The list is no longer "the
    // trades that exist, less the ones lately found empty" but "the trades that answered with real work when
    // asked a moment ago" — see BotMinds.Working. The barren memory stays, because a trade can still empty
    // out in the seconds between being asked and being taken up, but it is now a fence and not the wall.

    /// <summary>
    /// What the other thinking bots have said lately.
    ///
    /// <para>
    /// Their words, unedited, with who said them and how long ago — and nothing else. No instruction to obey
    /// them, agree with them or answer them: a remark from somebody else is a fact about the world, on the
    /// same footing as a wraith being visible or a pack being full, and what to do about it is the decision
    /// being asked for. A mind told to <em>respond</em> would spend its one answer a minute on conversation.
    /// </para>
    /// </summary>
    private static void Heard(ref ValueStringBuilder sb, BotMind mind)
    {
        var said = false;

        foreach (var (who, what, ago) in BotMindTalk.Heard(mind.Name))
        {
            if (!said)
            {
                said = true;

                sb.AppendLine("\nWHAT THE OTHERS HAVE SAID");
            }

            sb.Append("- ");
            sb.Append(who);
            sb.Append(", ");
            sb.Append(ago);
            sb.Append("s ago: ");
            sb.AppendLine(what);
        }
    }

    private static void Lessons(ref ValueStringBuilder sb, BotMind mind)
    {
        var lessons = mind.Lessons;

        if (lessons.Count == 0)
        {
            return;
        }

        sb.AppendLine("\nWHAT YOU HAVE LEARNED");

        for (var i = 0; i < lessons.Count; i++)
        {
            sb.Append("- ");
            sb.AppendLine(lessons[i]);
        }
    }

    private static void Offers(ref ValueStringBuilder sb, IReadOnlyList<string> trades, IReadOnlyList<string> band)
    {
        // Named as what they are: not the trades that exist, but the ones with something in them this
        // minute. Every proposer on this list was asked a moment ago and answered with a real piece of work.
        sb.AppendLine("\nTRADES WITH WORK IN THEM RIGHT NOW");

        for (var i = 0; i < trades.Count; i++)
        {
            sb.Append("- ");
            sb.AppendLine(Explain(trades[i]));
        }

        // A trade resting from this mind's own choice is still one its band may be set to: see BotMind.Rest. Named in one
        // line and given no reason, because a reason is something a model argues with.
        if (band == null)
        {
            return;
        }

        var first = true;

        for (var i = 0; i < band.Count; i++)
        {
            var listed = false;

            for (var j = 0; j < trades.Count && !listed; j++)
            {
                listed = string.Equals(trades[j], band[i], StringComparison.OrdinalIgnoreCase);
            }

            if (listed)
            {
                continue;
            }

            sb.Append(first ? "Your band's gather and make may also name: " : ", ");
            sb.Append(band[i]);
            first = false;
        }

        if (!first)
        {
            sb.AppendLine(".");
        }
    }

    /// <summary>
    /// A trade's name with a plain sentence about it.
    ///
    /// <para>
    /// The names are the shard's own — a proposer is called <c>Miner</c> and that is the word the schema
    /// constrains the answer to — but a bare list of nouns leaves the model guessing what half of them mean,
    /// and a guess about <c>Gleaner</c> or <c>Muster</c> is a decision made for the wrong reason. Anything
    /// unrecognised is named without a gloss rather than being hidden: a trade this file has not been taught
    /// about is still a trade the bot may take.
    /// </para>
    /// </summary>
    private static string Explain(string trade) =>
        trade switch
        {
            "Hunter" => "Hunter — go and kill something on your own, and take what it carries.",
            "Muster" => "Muster — call a company of nearby bots against something too big for one of you.",
            "Gleaner" => "Gleaner — pick up spent arrows and bolts from the ground.",
            "Miner" => "Miner — dig ore, smelt it into ingots, and put them on the market or in the bank.",
            "Shopper" => "Shopper — buy back the supplies you are short of from a shopkeeper.",
            "Peddler" => "Peddler — sell what you are carrying to a shopkeeper for coin.",
            "Seeker" => "Seeker — fill a standing order another bot has posted on the market.",
            "Tailor" => "Tailor — buy cloth and sew goods to sell.",
            "Scribe" => "Scribe — buy blank scrolls and write spells to sell.",
            "Surgeon" => "Surgeon — go and heal one of your own people who is hurt.",
            "Smith" => "Smith — take metal to an anvil and forge goods, filling the board's orders first.",
            "Bullion" => "Bullion — buy metal from a shopkeeper instead of spending eight minutes digging it.",
            "Upkeep" => "Upkeep — post an order on the market for a replacement of something you are wearing out.",
            "Armoury" => "Armoury — buy a few attack scrolls to open a fight with or to break away from one.",
            "Lifeline" => "Lifeline — carry a resurrection scroll to raise one of your company where it falls.",
            "Rescuer" => "Rescuer — go to the aid of one of your own people who is being set upon.",
            "Undertaker" => "Undertaker — go back for your own corpse and recover what you were carrying.",
            "Porter" => "Porter — carry goods to where they were asked for.",
            "Baron" => "Baron — take five bots to the ground that has killed the most people and empty it. Twenty dead or forty minutes, and the square comes off the board. Everything it drops goes to them.",
            "Stroll" => "Stroll — walk your town. It pays nothing; it is where you are when no ground is standing.",
            _ => trade
        };
}
