using System;
using System.Collections.Generic;
using Server.Guilds;

namespace Server.BotAI.V2;

/// <summary>
/// What is said round a fire and on the road: a greeting and a welcome, then news of the roads, the worst ground near,
/// trade and the speaker's guild, and small talk, with an answer now and then.
///
/// <para>
/// The news is read where the shard keeps it: the roads the population has walked and how they read
/// (<see cref="BotRoadbook"/>), the worst square near by the quadrant map (<see cref="BotQuad"/>), what the speaker is
/// carrying that somebody wants, or after, or what the island's wants pay most for (<see cref="BotMarket"/>), how its guild's war stands
/// (<see cref="BotWar"/>) or what it is working towards (<c>BotGuilds.Aim</c>).
/// </para>
///
/// <para>
/// <b>Every line is a fact the shard already keeps, said in the voice's own channel.</b> Nothing here is decided for the
/// sake of talking: a road's band is <c>BotRoadbook.Safety</c>, a square's deaths are the quadrant's own count, a price is a
/// stall's or a want's. So a bot at a fire says what the population knows, which is the only news there is — and the
/// person watching learns it from the fire, in <c>logs/bot-speech.log</c> and on the event stream, like every other line
/// (<see cref="BotVoice.Say"/>).
/// </para>
///
/// <para>
/// <b>Spaced by the fire, not by the bot.</b> The voice rations each bot to a line every fifteen seconds; a fire of five
/// would then say five lines in a second and nothing for fifteen. Lines here are paced by the fire's own clock
/// (<see cref="LineMs"/>), said with the voice's <c>force</c> — the use it names, "a line that is the whole point" — and
/// each bot says at most <see cref="MostLinesEach"/>.
/// </para>
/// </summary>
public static class BotCampTalk
{
    /// <summary>Between one line and the next round a fire. Seven seconds, and up to four more.</summary>
    public static int LineMs { get; set; } = 7000;

    public static int LineSpreadMs { get; set; } = 4000;

    /// <summary>Between a line and its answer. Three seconds.</summary>
    public static int ReplyMs { get; set; } = 3000;

    /// <summary>The most lines one bot says at one fire. Five, a greeting among them: "a few lines each".</summary>
    public static int MostLinesEach { get; set; } = 5;

    /// <summary>How often a line of news is answered. A third.</summary>
    public static double ReplyChance { get; set; } = 0.35;

    /// <summary>How far round the fire the worst ground is looked for. Two hundred and fifty tiles, a day's walking.</summary>
    public static int DangerReach { get; set; } = 250;

    /// <summary>
    /// Boredom a line of company takes off each listener, in the takings' own units (<c>BotUrges.Paid</c>). Twelve: a fire of
    /// four talking for five minutes lifts about a fifth of a bot's boredom, what twenty gold of takings would.
    /// </summary>
    public static double CheerPerLine { get; set; } = 12.0;

    public static long Lines { get; private set; }

    public static long Greetings { get; private set; }

    public static long Roads { get; private set; }

    public static long Dangers { get; private set; }

    public static long Trades { get; private set; }

    public static long GuildTalk { get; private set; }

    public static long Small { get; private set; }

    public static long Replies { get; private set; }

    public static long Held { get; private set; }

    /// <summary>The words, written into the voice's bank for every occasion <c>bot-voice.json</c> does not already name.</summary>
    public static Dictionary<string, string[]> Defaults() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["took:camp"] = ["A fire, I think, and a sit down.", "My feet have had enough. Time for a fire.", "I'll get a fire going; anybody is welcome at it."],
            ["finished:camp"] = ["That's the fire out. Safe roads, all.", "Good fire. Good talk.", "Embers. Time to move on."],
            ["failed:camp"] = ["The kindling won't take.", "No fire tonight, then."],
            ["took:fireside"] = ["Is that a fire? I could use a sit.", "Smoke through the trees. Company, maybe.", "A fire in the woods. I'll warm my hands."],
            ["finished:fireside"] = ["Thanks for the fire.", "Good company. Back to it.", "Safe roads, all of you."],
            ["finished:chat"] = ["Safe roads.", "Mind how you go.", "Until next time, then."],
            ["camp:lit"] = ["There. Sit, if you like.", "Fire's going. Pull up a log, anyone.", "Nothing like a fire in the woods."],
            ["camp:greet"] = ["Evening, {other}. Room for one more?", "Mind if I sit, {other}?", "{other}! Saw your smoke from the path."],
            ["camp:welcome"] = ["Sit, sit. Plenty of fire for two.", "Welcome, {other}.", "Come in out of the dark, {other}."],
            ["camp:road:safe"] =
            [
                "Walked from {from} to {to}; the road reads {band}.",
                "The road to {to} is fine. Walked {walked} times now.",
                "Nothing on the road from {from} to {to} worth fearing."
            ],
            ["camp:road:unsafe"] =
            [
                "Stay off the road from {from} to {to}; it reads {band}.",
                "The {to} road is bad. Go in company.",
                "I would not walk to {to} alone. The road reads {band}."
            ],
            ["camp:danger"] =
            [
                "Keep clear of ({x}, {y}) near {town}; it reads {band}.",
                "Something bad lives by ({x}, {y}).",
                "The ground at ({x}, {y}) is {band}. Mind it."
            ],
            ["camp:danger:dead"] =
            [
                "{deaths} of ours have died at ({x}, {y}) near {town}. Keep away.",
                "Don't go near ({x}, {y}). {deaths} dead there already."
            ],
            ["camp:quiet"] = ["Quiet around here, anyway.", "Nobody has died near here that I've heard.", "The woods are calm tonight."],
            ["camp:selling"] = ["I'm carrying {amount} {item}, and somebody is paying {price} apiece for them.", "Anybody need {item}? I have {amount}, {price} each, hand to hand."],
            ["camp:buying"] = ["I'm after {item}; paying {price} a piece.", "If you come across {item}, I'll give {price} for each."],
            ["camp:market"] = ["Everybody wants {item} these days; {price} a piece, they're paying.", "There's money in {item}. {price} on the board."],
            ["camp:war"] =
            [
                "{guild} and {enemy} are at it: {kills} of theirs to {lost} of ours.",
                "The war with {enemy} goes on. Watch yourselves.",
                "{enemy} again. {kills} to {lost}, last I heard."
            ],
            ["camp:guild"] = ["{guild} is after {aim}.", "In {guild} we're working towards {aim}.", "{guild} keeps me busy. {aim}, next."],
            ["camp:noguild"] = ["No guild for me. Nobody tells me where to dig.", "I keep my own counsel. No guild.", "A guild? Maybe one day."],
            ["camp:small"] =
            [
                "Nice night for it.",
                "Smells like rain.",
                "My feet are killing me.",
                "Good fire, this.",
                "Anyone got anything to eat?",
                "I could sleep for a week."
            ],
            ["camp:reply"] = ["Is that so.", "Good to know.", "Hm.", "Aye.", "I'll keep that in mind.", "Huh. Thanks."],
            ["camp:tale-witness"] = ["Aye, {about}. I know them well.", "I have a tale of {about} myself, another evening.", "So that's what became of {about}."],
            ["camp:tale-heard"] = ["I've heard this one — told well all the same.", "Aye, {about}'s name travels.", "This one's reached my ears before."],
            ["camp:tale-retold"] = ["A story I picked up in {place}, from {teller}: {tale}", "{teller} told it in {place}, and I tell it again: {tale}", "Word travels. In {place} I heard {teller} tell it: {tale}"],
            ["abode:furnished"] = ["A bed of my own, at last.", "There. A real bed, in my own house.", "Look at that. A bed, and it's mine."],
            ["meet:greet"] = ["Well met, {other}.", "{other}! Where are you off to?", "Hail, {other}. Keeping well?"],
            ["meet:answer"] = ["Well met yourself.", "About my work. You?", "Can't stop long, {other}.", "Keeping well enough."],
            ["meet:small"] = ["Warm day.", "Long road, this.", "Busy, busy.", "Have you seen the market lately?"]
        };

    /// <summary>
    /// Puts the defaults into the voice's bank, each only where the bank has nothing for that occasion: the file's words win
    /// whichever module starts first, and <c>BotVoice</c>'s own files are not touched.
    /// </summary>
    public static int Phrases()
    {
        var added = 0;

        foreach (var (key, lines) in Defaults())
        {
            if (!BotVoice.Has(key))
            {
                BotVoice.Phrases(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { [key] = lines });
                added++;
            }
        }

        return added;
    }

    /// <summary>The keeper has just got the fire going.</summary>
    public static void Lit(BotCamp.Fire fire, Mobile keeper)
    {
        if (Say(keeper, "camp:lit", null, null, null))
        {
            fire.Lines++;
            fire.LastSpeaker = keeper;
        }
    }

    /// <summary>
    /// One line round a fire, if one is due: an answer owed, else a newcomer's greeting, else news from somebody who has not
    /// said their share. From the camp's clock; nothing is said with fewer than two seated.
    /// </summary>
    public static void Speak(BotCamp.Fire fire, long now)
    {
        if (!fire.Lit || now - fire.NextLineTick < 0)
        {
            return;
        }

        var places = fire.Places;
        Span<int> seated = stackalloc int[places.Length];
        var n = 0;

        for (var i = 0; i < places.Length; i++)
        {
            if (places[i] is { Seated: true, Bot: { Deleted: false, Alive: true } })
            {
                seated[n++] = i;
            }
        }

        if (n < 2)
        {
            return;
        }

        fire.NextLineTick = now + Math.Max(1000, LineMs) + Utility.Random(Math.Max(1, LineSpreadMs));

        // An answer owed comes first, and soon.
        if (fire.Owed.Who is { } owing)
        {
            var (_, to, occasion, about) = fire.Owed;

            fire.Owed = default;

            Dictionary<string, string> owedFill = null;

            if (about != null)
            {
                owedFill = new Dictionary<string, string> { ["about"] = about };
            }

            if (fire.PlaceOf(owing) is { Seated: true } answerer && Said(fire, answerer, to, occasion, null, owedFill))
            {
                if (occasion == "camp:reply")
                {
                    Replies++;
                }

                return;
            }
        }

        // A newcomer greets whoever keeps the fire (or anybody there), and is welcomed back.
        for (var k = 0; k < n; k++)
        {
            var place = places[seated[k]];

            if (place.Greeted)
            {
                continue;
            }

            place.Greeted = true;

            if (ReferenceEquals(place.Bot, fire.Keeper) && fire.Guests == 0)
            {
                continue;
            }

            var host = Other(fire, place.Bot, seated, n, preferKeeper: true);

            if (host != null && Said(fire, place, host, "camp:greet", null, null))
            {
                Greetings++;
                fire.Owed = (host, place.Bot, "camp:welcome", null);
                fire.NextLineTick = now + ReplyMs;

                return;
            }
        }

        // News, from somebody who has not said their share and did not speak last.
        BotCamp.Place speaker = null;
        var tries = 0;

        while (speaker == null && tries++ < n * 2)
        {
            var pick = places[seated[Utility.Random(n)]];

            if (pick.Lines < MostLinesEach && !ReferenceEquals(pick.Bot, fire.LastSpeaker))
            {
                speaker = pick;
            }
        }

        if (speaker == null)
        {
            return;
        }

        var listener = Other(fire, speaker.Bot, seated, n, preferKeeper: false);
        var fill = new Dictionary<string, string>();

        // Whoever is seated to hear it: a story at least half of them already carry is not told again (A6).
        var audience = new List<Mobile>(n - 1);

        for (var k = 0; k < n; k++)
        {
            var hearer = places[seated[k]].Bot;

            if (!ReferenceEquals(hearer, speaker.Bot))
            {
                audience.Add(hearer);
            }
        }

        var topic = Topic(speaker.Bot, fire.Map, fire.Centre, fill, "camp:small", out var told, audience);

        if (!Said(fire, speaker, listener, topic, "camp:small", fill))
        {
            return;
        }

        // A tale told aloud is remembered by everybody seated for it (PLAN-life A6). The answer is chosen before the
        // recording, else the answerer would always "have heard" the tale it is just now hearing.
        if (told != null)
        {
            if (listener != null)
            {
                var answer = BotBiography.Answer(listener, told);

                if (answer != null)
                {
                    fire.Owed = (listener, speaker.Bot, answer, told.Other);
                    fire.NextLineTick = now + ReplyMs;
                }
                else if (Utility.RandomDouble() < ReplyChance)
                {
                    fire.Owed = (listener, speaker.Bot, "camp:reply", null);
                    fire.NextLineTick = now + ReplyMs;
                }
            }

            var place = BotBiography.Place(fire.Map, fire.Centre);

            for (var i = 0; i < fire.Places.Length; i++)
            {
                if (fire.Places[i] is { Seated: true, Bot: { } hearer })
                {
                    BotBiography.Heard(hearer, speaker.Bot, told, place);
                }
            }

            return;
        }

        if (listener != null && Utility.RandomDouble() < ReplyChance)
        {
            fire.Owed = (listener, speaker.Bot, "camp:reply", null);
            fire.NextLineTick = now + ReplyMs;
        }
    }

    /// <summary>
    /// A line of news for a speaker at a place: a road, a danger, a trade, its guild, or small talk — one of the five at
    /// random, falling through to the next when the shard has nothing to say about it. Fills <paramref name="fill"/> and
    /// returns the occasion. When the occasion is <c>camp:tale</c>, <paramref name="told"/> is the tale itself, so the
    /// listeners can remember it and answer to it; otherwise null.
    /// </summary>
    public static string Topic(Mobile speaker, Map map, Point3D at, Dictionary<string, string> fill, string small, out BotTale told, IEnumerable<Mobile> audience = null)
    {
        // Now and then a tale of the speaker's own life, or a word about a friend or an enemy, instead of the news (07.10.2026,
        // traits17). See BotBiography.Tale.
        if (BotBiography.Tale(speaker, fill, out told, audience) is { } tale)
        {
            return tale;
        }

        var first = Utility.Random(4);

        for (var k = 0; k < 4; k++)
        {
            var occasion = ((first + k) % 4) switch
            {
                0 => Road(fill),
                1 => Danger(map, at, fill),
                2 => Trade(speaker, fill),
                _ => GuildNews(speaker, fill)
            };

            if (occasion != null)
            {
                return occasion;
            }
        }

        Small++;

        return small;
    }

    /// <summary>A road somebody has walked, at random, and how it reads on the danger map.</summary>
    private static string Road(Dictionary<string, string> fill)
    {
        BotRoadbook.Road pick = null;
        var seen = 0;
        var roads = BotRoadbook.All;

        for (var i = 0; i < roads.Count; i++)
        {
            if (roads[i] is { Walked: > 0, Drawn: true } road && Utility.Random(++seen) == 0)
            {
                pick = road;
            }
        }

        if (pick == null)
        {
            return null;
        }

        var safety = BotRoadbook.Safety(pick);

        fill["from"] = pick.From;
        fill["to"] = pick.To;
        fill["band"] = BotQuad.Band(safety);
        fill["walked"] = pick.Walked.ToString();
        Roads++;

        return safety <= BotQuad.Unsafe ? "camp:road:unsafe" : "camp:road:safe";
    }

    /// <summary>The worst square the population knows of near the fire, with its dead; or the quiet, when there is none.</summary>
    private static string Danger(Map map, Point3D at, Dictionary<string, string> fill)
    {
        var worst = BotQuad.WorstNear(map, at, DangerReach);

        // Nothing the population fears within reach is news too.
        if (worst == Point2D.Zero)
        {
            Dangers++;

            return "camp:quiet";
        }

        var where = new Point3D(worst.X, worst.Y, 0);
        var deaths = BotQuad.Known(map, where)?.Deaths ?? 0;

        fill["x"] = worst.X.ToString();
        fill["y"] = worst.Y.ToString();
        fill["band"] = BotQuad.Band(BotQuad.Safety(map, where));
        fill["deaths"] = deaths.ToString();
        fill["town"] = BotTowns.Nearest(where)?.Name ?? "nowhere much";
        Dangers++;

        return deaths > 0 ? "camp:danger:dead" : "camp:danger";
    }

    /// <summary>
    /// Something the speaker carries that somebody has said it wants, at that want's price; else its own want, else the best-paid
    /// want said on the island. (The speaker's own stall came first until the auction went on 01.10.2026: goods ride in a pack now,
    /// and what is worth saying at a fire is that one has what another is asking for.)
    /// </summary>
    private static string Trade(Mobile speaker, Dictionary<string, string> fill)
    {
        var wants = BotMarket.Wants;
        BotWant carried = null;
        var held = 0;
        var seen = 0;

        if (speaker is IBotWilful own)
        {
            for (var i = 0; i < wants.Count; i++)
            {
                if (wants[i] is not { IsOpen: true } want || ReferenceEquals(want.Buyer?.Self, speaker))
                {
                    continue;
                }

                var have = BotHandover.Carried(own, want.Kind);

                if (have > 0 && Utility.Random(++seen) == 0)
                {
                    carried = want;
                    held = have;
                }
            }
        }

        if (carried != null)
        {
            fill["item"] = carried.Label;
            fill["amount"] = held.ToString();
            fill["price"] = carried.Offer.ToString();
            Trades++;

            return "camp:selling";
        }

        BotWant mine = null;
        BotWant best = null;

        for (var i = 0; i < wants.Count; i++)
        {
            if (wants[i] is not { IsOpen: true } want)
            {
                continue;
            }

            if (ReferenceEquals(want.Buyer?.Self, speaker))
            {
                mine ??= want;
            }

            if (best == null || want.Offer > best.Offer)
            {
                best = want;
            }
        }

        var said = mine ?? best;

        if (said == null)
        {
            return null;
        }

        fill["item"] = said.Label;
        fill["price"] = said.Offer.ToString();
        Trades++;

        return mine != null ? "camp:buying" : "camp:market";
    }

    /// <summary>The speaker's guild's war, else what it is working towards; or that it has no guild.</summary>
    private static string GuildNews(Mobile speaker, Dictionary<string, string> fill)
    {
        if (speaker?.Guild is not Guild guild)
        {
            GuildTalk++;

            return "camp:noguild";
        }

        foreach (var war in BotWar.Standing)
        {
            var enemy = war.Against(guild.Name);

            if (enemy == null)
            {
                continue;
            }

            fill["enemy"] = enemy;
            fill["kills"] = war.Score(guild.Name).Kills.ToString();
            fill["lost"] = war.Score(enemy).Kills.ToString();
            GuildTalk++;

            return "camp:war";
        }

        fill["aim"] = BotGuilds.Aim(guild);
        GuildTalk++;

        return "camp:guild";
    }

    /// <summary>Somebody else seated: the keeper when asked for and there, else anybody.</summary>
    private static Mobile Other(BotCamp.Fire fire, Mobile not, Span<int> seated, int n, bool preferKeeper)
    {
        if (preferKeeper && fire.Keeper is { } keeper && !ReferenceEquals(keeper, not) && fire.PlaceOf(keeper) is { Seated: true })
        {
            return keeper;
        }

        var start = Utility.Random(n);

        for (var k = 0; k < n; k++)
        {
            var bot = fire.Places[seated[(start + k) % n]].Bot;

            if (!ReferenceEquals(bot, not))
            {
                return bot;
            }
        }

        return null;
    }

    /// <summary>A line said at a fire, counted against the speaker's share, and a little company for everybody listening.</summary>
    private static bool Said(BotCamp.Fire fire, BotCamp.Place place, Mobile to, string occasion, string fallback, Dictionary<string, string> fill)
    {
        if (!Say(place.Bot, occasion, fallback, to, fill))
        {
            return false;
        }

        place.Lines++;
        fire.Lines++;
        fire.LastSpeaker = place.Bot;

        if (CheerPerLine > 0.0)
        {
            for (var i = 0; i < fire.Places.Length; i++)
            {
                if (fire.Places[i] is { Seated: true, Bot: { } listener } && !ReferenceEquals(listener, place.Bot))
                {
                    listener.Resolve?.Urges?.Paid(CheerPerLine);
                }
            }
        }

        return true;
    }

    /// <summary>A line said aloud, in the local channel, with the listener's name for <c>{other}</c>. Counted either way.</summary>
    public static bool Say(Mobile speaker, string occasion, string fallback, Mobile to, Dictionary<string, string> fill)
    {
        if (speaker == null || occasion == null)
        {
            return false;
        }

        fill ??= [];
        fill["other"] = to?.Name ?? "friend";

        var line = BotVoice.Phrase(occasion, fallback, speaker, fill);

        if (line == null || !BotVoice.Say(speaker, "local", line, force: true))
        {
            Held++;

            return false;
        }

        Lines++;

        return true;
    }

    public static string Describe() =>
        $"{Lines} lines said by a fire or on the road ({Greetings} greetings, {Roads} of a road, {Dangers} of danger, {Trades} of trade, {GuildTalk} of a guild, "
        + $"{Small} small talk, {Replies} answers), {Held} not said (the voice was off or had no words)";

    public static void Forget()
    {
        Lines = 0;
        Greetings = 0;
        Roads = 0;
        Dangers = 0;
        Trades = 0;
        GuildTalk = 0;
        Small = 0;
        Replies = 0;
        Held = 0;
    }
}
