using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Server.BotAI.V2;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// The marshal of events: the fourth of Argus's watchers, who organises rather than diagnoses.
///
/// <para>
/// <b>Patrick's order of 16.09.2026, evening:</b> the guilds' masters are ordinary bots; the events — camps,
/// tournaments, errands, orders — are the business of one thinking bot in Argus's squad. Every
/// <see cref="EveryMs"/> it is shown the population's numbers, the board of errands, the treasury, the tournament
/// and the revels' ledger, and asked for one event or none: a revel (one trade worth ×3 for twelve minutes with a
/// prize), a camp (a revel with an orc camp raised where it says), a tournament, an errand on the board (kill,
/// gather or scout, paid from the treasury), a standing order, a bounty on a square, a price on a head, or a fair.
/// </para>
///
/// <para>
/// <b>It invents no mechanism.</b> Every one of these is a verb the door already had, so what the marshal does is
/// what a hand at the keyboard could do, on a clock, with a reason written to the watchers' log. While it stands the
/// revels are its business too, and Argus goes back to watching.
/// </para>
/// </summary>
public static class BotMarshal
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMarshal));

    /// <summary>The marshal's name. Hermes, the herald: the watchers are named for the far-sighted, and this one for the messenger.</summary>
    public static string Name { get; set; } = "Hermes";

    /// <summary>Whether a marshal stands at all. Without one, Argus declares the revels as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often the marshal is asked for an event. Ten minutes.</summary>
    public static int EveryMs { get; set; } = 600000;

    /// <summary>The marshal's robe.</summary>
    public static int Hue { get; set; } = 53;

    /// <summary>The watcher that is the marshal, while one stands.</summary>
    public static BotWatcher Standing { get; internal set; }

    private static long _thoughtTick;

    public static long Asked { get; private set; }

    /// <summary>Events set going.</summary>
    public static long Events { get; private set; }

    /// <summary>Times the marshal chose nothing.</summary>
    public static long Nothings { get; private set; }

    /// <summary>Answers that could not be read.</summary>
    public static long Unread { get; private set; }

    /// <summary>Events refused: an unknown event, or one its own rules turned away.</summary>
    public static long Refused { get; private set; }

    /// <summary>What came of the marshal's last answer, shown to it the next time it is asked. See <see cref="Sight"/>.</summary>
    private static string _last;

    /// <summary>
    /// How long an event the city refused is refused again without being tried, and said to be.
    ///
    /// <para>
    /// <b>Being shown the refusal was not enough.</b> On build 97 Hermes asked for a fair at 22:50, was told "the treasury
    /// holds 500gp of the 5000gp a fair wants", was shown exactly that at 23:00, and asked for a fair again. Twenty-five
    /// minutes: two more asks, and time for the mint to put a thousand gold back.
    /// </para>
    ///
    /// <para>
    /// <b>Only a refusal about the city's state rests an event; a refusal about the answer's own fields does not.</b> Build
    /// 98 rested whatever was refused, and at 23:55 Hermes asked for a revel of the kind "nothing": resting revels for that
    /// would have turned away the same revel with its kind put right. A treasury short of a fair's floor stays short for
    /// a while; a wrong word is put right on the next ask, and the answer shown to the marshal already names the words.
    /// </para>
    /// </summary>
    public static int RefusedRestMs { get; set; } = 1500000;

    private static string _refusedEvent;

    private static long _refusedTick;

    private static string _refusedWhy;

    /// <summary>Answers turned away because the same event had been refused within <see cref="RefusedRestMs"/>.</summary>
    public static long Repeated { get; private set; }

    public static bool Due(long now) => Running && Standing != null && now - _thoughtTick >= EveryMs;

    /// <summary>The clock restarted: at waking, and each time the marshal is asked.</summary>
    public static void Woke(long now) => _thoughtTick = now;

    /// <summary>Minutes until the marshal is next asked for an event. For the live window (02.10.2026).</summary>
    public static long NextInMinutes(long now) => Math.Max(0, EveryMs - (now - _thoughtTick)) / 60000;

    /// <summary>
    /// What the model may answer. The event's words are enforced in <see cref="Act"/>, as the revel's are.
    ///
    /// <para>
    /// <b>Every field required, from build 95.</b> With only the event and the reason required, the model left out what the
    /// event needed and the verb refused it: on build 94 Hermes's first two events were a revel with no kind (20:51) and a
    /// standing order with no amount and no price (21:11). A field that must be written is a field the model has to think
    /// about; one it may leave out is one it forgets.
    /// </para>
    ///
    /// <para>
    /// <b>The event and the kind chosen from lists, from build 101e.</b> A field that must be written can still be written
    /// wrong, and a wrong word is refused as surely as a missing one. On the night of 16–17.09.2026 Hermes asked for revels of
    /// the kinds "nothing" (23:55), "" (01:51, 02:53, 07:34) and orders for things called "herbs", "wood", "hunt" and "" — each
    /// refused, each shown to it, and the next ask as likely to repeat it. The event names and the kinds, which are closed
    /// lists, are enumerated (N2: narrow the enumeration instead of persuading); the thing an order or an errand names is not a
    /// closed list, and the sight names what the city knows instead (see <see cref="Sight"/>).
    /// </para>
    ///
    /// <para>
    /// <b>One shape for each event open this time, with only the fields it takes, from build 102.</b> Two lists in one flat form
    /// still let a right word land in the wrong event, and every number was a "number": from 03:13 to 10:22 on 17.09.2026
    /// Hermes was asked 42 times and 22 answers were refused — an errand of the kind "hunt" (09:39, a revel's word, which the
    /// shared list allowed), orders for things called "wood", "ore", "hunt" and "" twice, standing orders and errands with an
    /// amount or a reward of nought (six; and a "10.0" was read as nought too), fairs on a treasury short of its floor and the
    /// same fair again (seven). So the form is an <c>anyOf</c> of events: the kinds, the things, the creatures and the murderers
    /// each named from a list of their own, whole numbers between the bounds the verbs refuse outside, and an event the city
    /// would refuse on its state left out altogether — a fair while the treasury is under its floor, a price on a head while
    /// nobody is red. Measured against Ollama 0.34 before it was built: a demanded amount of 0 came back 1, "3.5 gold" came
    /// back 3, a fair of 2 minutes against a least of 5 came back 20, and "Bananas" came back as a thing on the list.
    /// </para>
    /// </summary>
    public static string Schema => Build();

    /// <summary>The events open when the sight was last written, so the form offers what the sight named. See <see cref="Open"/>.</summary>
    private static List<string> _open;

    private static long _openTick;

    /// <summary>The events <see cref="Act"/> carries out.</summary>
    private static readonly string[] EventWords = ["nothing", "revel", "camp", "tourney", "errand", "order", "bounty", "head", "fair"];

    /// <summary>The kinds any event reads: a revel's or a camp's trade, or an errand's kill, gather or scout.</summary>
    private static readonly string[] ErrandKinds = ["kill", "gather", "scout"];

    /// <summary>The raw goods a gathering errand may name besides what is on show at the shops, where the engine knows the type.</summary>
    private static readonly string[] Gathered =
    [
        "IronOre", "Log", "Hides", "Leather", "RawRibs", "Feather", "Wool", "Garlic", "Ginseng", "MandrakeRoot", "Nightshade",
        "Bloodmoss", "SpidersSilk", "BlackPearl", "SulfurousAsh"
    ];

    /// <summary>Largest place either way a shape offers, a little past the edge of Felucca.</summary>
    private const int MostX = 7167;

    private const int MostY = 4095;

    /// <summary>The form of this ask: one shape per open event. See <see cref="Schema"/>.</summary>
    private static string Build()
    {
        var open = _open != null && Core.TickCount - _openTick < 5000 ? _open : Open();
        var stalls = Stalls(30);
        var buffer = new MemoryStream();

        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteStartArray("anyOf");

            for (var i = 0; i < open.Count; i++)
            {
                switch (open[i])
                {
                    case "nothing":
                        new Shape(w, "nothing").Close(false);

                        break;

                    case "revel":
                        new Shape(w, "revel").Words("kind", BotRevel.Trades).Number("pay", 0, BotRevel.MostPrize).Close();

                        break;

                    case "camp":
                        new Shape(w, "camp").Number("pay", 0, BotRevel.MostPrize).Number("x", 0, MostX).Number("y", 0, MostY).Close();

                        break;

                    case "tourney":
                        new Shape(w, "tourney").Close();

                        break;

                    case "errand":
                        {
                            var pays = Math.Max(1, Math.Min(BotQuests.MostReward, BotCity.Purse));
                            var creatures = BotLairs.Kinds(BotPopulation.Home);

                            if (creatures.Count > 0)
                            {
                                // No place: a kill goes to the nearest lair that keeps the creature. The only coordinates
                                // the marshal is given are the population's home, and at 12:46 on 17.09.2026 it put five
                                // ogres there — refused, the nearest ogre spawner being 200 tiles off.
                                new Shape(w, "errand").Words("kind", ["kill"]).Words("name", creatures)
                                    .Number("number", 1, BotQuests.MostAmount).Number("pay", 1, pays).Close();
                            }

                            var goods = Goods(stalls);

                            if (goods.Count > 0)
                            {
                                new Shape(w, "errand").Words("kind", ["gather"]).Words("name", goods)
                                    .Number("number", 1, BotQuests.MostAmount).Number("pay", 1, pays)
                                    .Number("x", -1, MostX).Number("y", -1, MostY).Close();
                            }

                            new Shape(w, "errand").Words("kind", ["scout"]).Number("pay", 1, pays)
                                .Number("x", -1, MostX).Number("y", -1, MostY).Close();

                            break;
                        }

                    case "order":
                        if (stalls.Count > 0)
                        {
                            new Shape(w, "order").Words("name", stalls).Number("number", 1, BotQuests.MostAmount)
                                .Number("pay", 1, 1000).Close();
                        }

                        break;

                    case "bounty":
                        new Shape(w, "bounty").Number("pay", 1, Math.Max(1, Math.Min(BotCity.Cap, BotCity.Purse)))
                            .Number("x", 0, MostX).Number("y", 0, MostY).Close();

                        break;

                    case "head":
                        {
                            var reds = Reds();

                            if (reds.Count > 0)
                            {
                                new Shape(w, "head").Words("name", reds)
                                    .Number("pay", 1, Math.Max(1, Math.Min(BotCity.Cap, BotCity.Purse))).Close();
                            }
                        }

                        break;

                    case "fair":
                        new Shape(w, "fair").Number("number", 10, 120).Close();

                        break;
                }
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// One event's shape in the form, its fields written and required in alphabetical order.
    ///
    /// <para>
    /// <b>Alphabetical because that is the order the model is held to.</b> Asked with thinking on, Ollama 0.34 sorts a
    /// schema's properties by name before the answer is constrained (a Go map is written out sorted): deepseek-r1:14b,
    /// asked on 17.09.2026 for "a standing order for 10 Leather", answered <c>{"event": "errand", "gold": …, "kind":
    /// "scout", …}</c> and then a bounty — every shape whose first field sorted before "event" ("amount", in the kill, the
    /// gather and the order) was closed to an answer that began with "event", so build 102's form left Hermes the scout, the
    /// bounty and the events with no amount. Its answers at 11:25, 11:49 and 12:00 were scouts whose reasons were
    /// standing orders. The names are chosen so the sorted order is the answer's order — event, kind, name, number, pay,
    /// say, why, x, y — and a qwen3 asked without thinking, which keeps the written order, is given the same.
    /// </para>
    /// </summary>
    private sealed class Shape
    {
        private readonly Utf8JsonWriter _w;

        private readonly List<(string Field, Action<Utf8JsonWriter> Write)> _fields = [];

        public Shape(Utf8JsonWriter w, string what)
        {
            _w = w;

            // A list of one rather than a constant: both are read the same way, and a list is the older of the two.
            _fields.Add(("event", x => Enumerated(x, [what])));
        }

        public Shape Words(string field, IReadOnlyList<string> words)
        {
            _fields.Add((field, x => Enumerated(x, words)));

            return this;
        }

        public Shape Number(string field, int least, int most)
        {
            _fields.Add(
                (field, x =>
                    {
                        x.WriteString("type", "integer");
                        x.WriteNumber("minimum", least);
                        x.WriteNumber("maximum", Math.Max(least, most));
                    })
            );

            return this;
        }

        private static void Enumerated(Utf8JsonWriter x, IReadOnlyList<string> words)
        {
            x.WriteString("type", "string");
            x.WriteStartArray("enum");

            for (var i = 0; i < words.Count; i++)
            {
                x.WriteStringValue(words[i]);
            }

            x.WriteEndArray();
        }

        private static void Text(Utf8JsonWriter x) => x.WriteString("type", "string");

        /// <summary>The herald's line and the reasoning added, every field written in order, then what must be written.</summary>
        public void Close(bool says = true)
        {
            if (says)
            {
                _fields.Add(("say", Text));
            }

            _fields.Add(("why", Text));
            _fields.Sort((a, b) => string.CompareOrdinal(a.Field, b.Field));

            _w.WriteStartObject();
            _w.WriteString("type", "object");
            _w.WriteStartObject("properties");

            for (var i = 0; i < _fields.Count; i++)
            {
                _w.WriteStartObject(_fields[i].Field);
                _fields[i].Write(_w);
                _w.WriteEndObject();
            }

            _w.WriteEndObject();
            _w.WriteStartArray("required");

            for (var i = 0; i < _fields.Count; i++)
            {
                _w.WriteStringValue(_fields[i].Field);
            }

            _w.WriteEndArray();
            _w.WriteEndObject();
        }
    }

    /// <summary>
    /// The events that can be carried out now, in <see cref="EventWords"/>' order: nothing always; not an event refused on
    /// the city's state within <see cref="RefusedRestMs"/>; a revel or a camp only while no revel runs; a tournament only while none runs; an errand, a bounty or a head
    /// only on a treasury with gold in it; an order only while something is on show at the shops by the bank; a head only while somebody is red;
    /// a fair only on a treasury at its floor and while no fair runs. See <see cref="Schema"/>.
    /// </summary>
    private static List<string> Open()
    {
        List<string> open = [];
        var paid = BotCity.Running && BotCity.Purse >= 1;

        for (var i = 0; i < EventWords.Length; i++)
        {
            var what = EventWords[i];

            if (what != "nothing" && what == _refusedEvent && Core.TickCount - _refusedTick < RefusedRestMs)
            {
                continue;
            }

            var can = what switch
            {
                "revel" or "camp" => !BotRevel.Running,
                "tourney" => BotTourney.Ready,
                "errand" => paid && BotQuests.Running,
                "order" => BotCity.Running && Stalls(1).Count > 0,
                "bounty" => paid,
                "head" => paid && Reds().Count > 0,
                "fair" => BotCity.Running && BotCity.Purse >= BotCity.FairFloor && !BotCity.Fairing,
                _ => true
            };

            if (can)
            {
                open.Add(what);
            }
        }

        _open = open;
        _openTick = Core.TickCount;

        return open;
    }

    /// <summary>The names of the murderers at large now.</summary>
    private static List<string> Reds()
    {
        List<string> reds = [];
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && BotOutlaw.IsRed(bot) && !reds.Contains(bot.Name))
            {
                reds.Add(bot.Name);
            }
        }

        return reds;
    }

    /// <summary>What a gathering errand may name: the shops' things and the raw goods the engine knows, each once.</summary>
    private static List<string> Goods(List<string> stalls)
    {
        List<string> goods = [..stalls];

        for (var i = 0; i < Gathered.Length; i++)
        {
            var type = AssemblyHandler.FindTypeByName(Gathered[i]);

            if (type != null && typeof(Item).IsAssignableFrom(type) && !goods.Contains(type.Name))
            {
                goods.Add(type.Name);
            }
        }

        return goods;
    }

    /// <summary>The names of the things most on show at the bots' shops by the bank, most first, as a sentence.</summary>
    private static string Offered(int most)
    {
        var names = Stalls(most);

        return names.Count == 0 ? "nothing at the shops by the bank" : string.Join(", ", names);
    }

    /// <summary>
    /// The names of the things most on show at the bots' shops by the bank, most first: names the city's order knows. The stalls,
    /// until the auction went on 01.10.2026; the city's standing orders are served at the shops now (<c>BotShopkeep.CityBuy</c>).
    /// </summary>
    private static List<string> Stalls(int most)
    {
        var lots = BotShopkeep.Lots();
        Dictionary<string, int> amounts = [];

        for (var i = 0; i < lots.Count; i++)
        {
            var name = lots[i].Kind.Name;

            amounts.TryGetValue(name, out var had);
            amounts[name] = had + lots[i].Units;
        }

        List<(string Name, int Amount)> sorted = [];

        foreach (var (name, amount) in amounts)
        {
            sorted.Add((name, amount));
        }

        sorted.Sort((a, b) => b.Amount.CompareTo(a.Amount));

        List<string> names = [];

        for (var i = 0; i < sorted.Count && i < most; i++)
        {
            names.Add(sorted[i].Name);
        }

        return names;
    }

    /// <summary>What the marshal is told before it is asked.</summary>
    public static string System(string name)
    {
        var home = BotPopulation.Where;

        return $"You are {name}, the marshal of events on a shard of autonomous bots, one of {BotVigil.Name}'s watchers. "
               + "Every so often you may set one event going, or none. 'event' is one of the events open to you this time, "
               + "which the end of your sight names; the others are these, for when they open again. "
               + "revel: one kind of work worth three times as much for twelve minutes, with a prize of 'pay' gold to whoever "
               + $"does most of it; 'kind' is one of {string.Join(", ", BotRevel.Trades)}. "
               + "camp: the same, with an orc camp raised at 'x','y' in the countryside, out of town and near where bots are "
               + "working, for the population to fight over; use 'kind' hunt. A spot on dangerous ground, on ground no walk "
               + "reaches, with no bot near it, or close to one of the last few places an event was held is moved to a "
               + "fitting place, and a revel of a trade is called in a town square chosen the same way: you need not pick "
               + $"the population's home at ({home.X}, {home.Y}), and should not pick the same spot twice. "
               + "tourney: a championship, one against one, run by the watchers in a ring chosen among the towns: the ranks "
               + "fight for the right to challenge the champion, and the last one standing meets the champion for the title. "
               + "errand: an errand on the board, paid from the treasury when it is done: 'kind' is kill, gather or scout; "
               + "'name' is the creature's or the thing's type name (Ogre, Leather, RawRibs); 'number' how many; 'pay' the "
               + "reward in gold. A kill has no place: it is sought at the nearest place a spawner on the island keeps the "
               + "creature (one kept only in the dungeons is refused). A delivery's 'x','y' is where to bring the goods, or -1 "
               + "for home; a scout's is where to look, or -1 for the nearest ground outside the walls nobody has counted "
               + "lately — a scout is paid for standing at its place, so one inside the walls is refused. Any bot fit "
               + "for it may take it and sees it through; an errand "
               + "three takers give up with nothing done comes down and its gold goes back. A reward is weighed against what "
               + "the bots earn a minute at their other work, walk and work together. "
               + "order: a standing order: the city takes 'number' of 'name' at the bots' shops by the bank whenever it is on show at up "
               + "to 'pay' gold each. "
               + "bounty: 'pay' gold on the square at 'x','y'; the Baron marches there before anywhere else, and the company "
               + "that clears it is paid. "
               + "head: 'pay' gold on the head of the murderer named in 'name', paid to whoever catches it. "
               + "fair: for 'number' minutes the city takes what the bots' shops by the bank hold at 80 percent of the asking price, "
               + $"spending evenly over the minutes and keeping {BotCity.FairReserve}gp back; only when the treasury holds "
               + $"{BotCity.FairFloor}gp or more. "
               + $"Everything but a revel is paid from the city's treasury, which holds {BotCity.Purse}gp of "
               + $"{BotCity.Cap} and fills at {BotCity.MintPerHour}gp an hour; a revel's prize comes from the watchers' own "
               + $"purse of {BotRevel.Purse}gp. Choose what the numbers ask for: idle bots want an errand or a revel, a "
               + "market with goods sitting unsold wants a fair or an order, a square where bots die wants a bounty, a "
               + "murderer at large wants a price on its head, a quiet and healthy evening wants a tournament or nothing. "
               + "Answer nothing when the shard is busy and healthy; that is a good answer and you should give it often. "
               + "Do not repeat an event whose last outcome, in what you are shown, was that nobody came. "
               + "'say' is what you shout to the island, in a herald's voice, one sentence. 'why' is your reasoning, for "
               + "the log. Write the fields your event takes, numbers as whole numbers, and an errand's x and y as -1 when "
               + "it has no place. What came of your last answer is shown to you; if it was refused, the reason is given, "
               + "and the same answer will be refused again.";
    }

    /// <summary>The board, the treasury, the tournament, the revels and the murderers, appended to the watcher's report.</summary>
    public static string Sight()
    {
        using var say = ValueStringBuilder.Create(2048);

        say.Append("THE MARSHAL'S OWN SIGHT. The board of errands: ");
        say.Append(BotQuests.Tell());
        say.Append(". The city: ");
        say.Append(BotCity.Describe());
        say.Append(". The tournament: ");
        say.Append(BotTourney.Describe());
        say.Append(". Past revels: ");
        say.Append(BotRevel.Ledger());
        say.Append(". Murderers: ");
        say.Append(BotOutlaw.Describe());
        say.Append(". Idle: ");
        say.Append(BotVigil.Loitering());
        say.Append(". The island's squares: ");
        say.Append(BotQuad.Describe());
        say.Append(". An order's thing is named as the city knows it, as these on show at the shops by the bank now are: ");
        say.Append(Offered(15));
        say.Append(". The events open to you this time: ");
        say.Append(string.Join(", ", Open()));
        say.Append(". Your last answer: ");
        say.Append(_last ?? "none yet");
        say.Append('.');

        return say.ToString();
    }

    /// <summary>The plan carried out; the answer for the log, or null when nothing could be read.</summary>
    public static string Act(BotMarshalPlan plan, Map map)
    {
        Asked++;

        if (plan == null)
        {
            Unread++;

            return null;
        }

        var what = (plan.Event ?? "nothing").Trim().ToLowerInvariant();
        var by = Name;
        var stamp = DateTime.Now.ToString("HH:mm");
        var before = Signature();
        string answer;

        if (what != "nothing" && what == _refusedEvent && Core.TickCount - _refusedTick < RefusedRestMs)
        {
            Refused++;
            Repeated++;
            _last = $"at {stamp}, {what} again, turned away without trying: it was refused {(Core.TickCount - _refusedTick) / 60000} minutes ago ({_refusedWhy}); choose another event, or nothing";

            return $"{what} turned away: refused {(Core.TickCount - _refusedTick) / 60000} minutes ago ({plan.Why})";
        }

        switch (what)
        {
            case "nothing":
                Nothings++;
                _last = $"at {stamp}, nothing";

                return $"nothing this time: {plan.Why}";

            case "revel":
                if (!BotRevel.Known(plan.Kind))
                {
                    Refused++;
                    _last = $"at {stamp}, a revel, refused: \"{plan.Kind}\" is not work anybody takes; kind must be one of {string.Join(", ", BotRevel.Trades)}";

                    return $"no revel: \"{plan.Kind}\" is not work anybody takes ({plan.Why})";
                }

                answer = BotRevel.Declare(plan.Kind, plan.Gold, plan.Say, plan.Why, false, Point3D.Zero);

                break;

            case "camp":
                answer = BotRevel.Declare(BotRevel.Known(plan.Kind) ? plan.Kind : "hunt", plan.Gold, plan.Say, plan.Why, true, plan.Spot);

                break;

            case "tourney":
                answer = BotTourney.Start(by);

                break;

            case "errand":
                answer = BotQuests.PostByName(
                    plan.Kind,
                    plan.What,
                    plan.Amount,
                    plan.Gold,
                    map,
                    plan.Spot != Point3D.Zero ? plan.Spot.X : -1,
                    plan.Spot != Point3D.Zero ? plan.Spot.Y : -1,
                    by
                );

                break;

            case "order":
                answer = BotCity.Want(plan.What ?? "", plan.Amount, plan.Gold, by);

                break;

            case "bounty":
                if (plan.Spot == Point3D.Zero)
                {
                    Refused++;
                    _last = $"at {stamp}, a bounty, refused: it needs x and y";

                    return $"no bounty: it needs x and y ({plan.Why})";
                }

                answer = BotCity.Bounty(map, plan.Spot.X, plan.Spot.Y, plan.Gold, by);

                break;

            case "head":
                {
                    var red = Named(plan.What);

                    if (red == null)
                    {
                        Refused++;
                        _last = $"at {stamp}, a price on a head, refused: no bot of ours is called \"{plan.What}\"";

                        return $"no price on a head: no bot of ours is called {plan.What} ({plan.Why})";
                    }

                    answer = BotCity.Head(red, plan.Gold, by);

                    break;
                }

            case "fair":
                // The city's own rule for when a fair is worth holding, which its clock has always asked and the marshal did
                // not: the first fair Hermes called, at 21:22 on 16.09.2026, was on a purse of 3850gp.
                if (BotCity.Purse < BotCity.FairFloor)
                {
                    Refused++;
                    Refuse(what, $"the treasury held {BotCity.Purse}gp of the {BotCity.FairFloor}gp a fair wants");
                    _last = $"at {stamp}, a fair, refused: the treasury holds {BotCity.Purse}gp and a fair is held at {BotCity.FairFloor}gp or more";

                    return $"no fair: the treasury holds {BotCity.Purse}gp of the {BotCity.FairFloor}gp a fair wants ({plan.Why})";
                }

                answer = BotCity.Fair(plan.Amount > 0 ? plan.Amount : Math.Max(1, BotCity.FairMs / 60000), BotCity.FairShare, by);

                break;

            default:
                Refused++;
                _last = $"at {stamp}, \"{plan.Event}\", refused: no such event";

                return $"\"{plan.Event}\" is no event the marshal knows ({plan.Why})";
        }

        // <b>An event is something that changed, not a sentence that came back.</b> Every verb answers in words whether it
        // did anything or not, and on build 94 a standing order with no amount was logged "set an event going: order — a
        // standing order needs an amount and a price" and counted among the events. The mechanisms' own counters say
        // whether anything was set going.
        if (Signature() == before)
        {
            Refused++;
            _last = $"at {stamp}, {what}, refused: {answer}";

            logger.Information("{Name} asked for {Event} and the city refused it: {Answer} ({Why})", Name, what, answer, plan.Why);

            return $"{what} refused: {answer} ({plan.Why})";
        }

        Events++;
        _last = $"at {stamp}, {what}: {answer}";

        logger.Information("{Name} set an event going: {Event} — {Answer} ({Why})", Name, what, answer, plan.Why);

        return $"{what}: {answer} ({plan.Why})";
    }

    /// <summary>Remembers a refused event, so the same answer is not tried again straight away. See <see cref="RefusedRestMs"/>.</summary>
    private static void Refuse(string what, string why)
    {
        _refusedEvent = what;
        _refusedTick = Core.TickCount;
        _refusedWhy = why;
    }

    /// <summary>What the event verbs have set going, all told: a revel, a championship, an errand, anything of the city's.</summary>
    private static (long Revels, long Tourneys, long Errands, long City) Signature() =>
        (BotRevel.Declared, BotTourney.Called, BotQuests.Posted, BotCity.Changes);

    private static BotMobile Named(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is { Deleted: false } && string.Equals(bot.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return bot;
            }
        }

        return null;
    }

    public static string Describe() =>
        Standing == null
            ? "no marshal stands"
            : $"{Name} was asked {Asked} times: {Events} events set going, {Nothings} times nothing, {Refused} refused ({Repeated} of them the same event again too soon), {Unread} answers unreadable";

    public static void Forget()
    {
        Asked = 0;
        Events = 0;
        Nothings = 0;
        Unread = 0;
        Refused = 0;
    }
}

/// <summary>What the marshal answered. A record of its own, for the same reason <see cref="BotRevelPlan"/> is one.</summary>
public sealed class BotMarshalPlan
{
    public string Event { get; init; }

    public string Kind { get; init; }

    public string What { get; init; }

    public int Amount { get; init; }

    public int Gold { get; init; }

    public Point3D Spot { get; init; }

    public string Say { get; init; }

    public string Why { get; init; }

    public static BotMarshalPlan Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new BotMarshalPlan
            {
                Event = Word(root, "event"),
                Kind = Word(root, "kind"),
                What = Word(root, "name") ?? Word(root, "what"),
                Amount = root.TryGetProperty("number", out _) ? Whole(root, "number") : Whole(root, "amount"),
                Gold = root.TryGetProperty("pay", out _) ? Whole(root, "pay") : Whole(root, "gold"),
                Say = Word(root, "say"),
                Why = Word(root, "why"),
                Spot = Tile(root, "x", out var ax) && Tile(root, "y", out var ay) && ax >= 0 && ay >= 0
                    ? new Point3D(ax, ay, 0)
                    : Point3D.Zero
            };
        }
        catch
        {
            return null;
        }
    }

    private static string Word(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>A number of the answer as a whole number, "10.0" included: read as nought before build 102.</summary>
    /// <summary>
    /// A coordinate, whole or written with a point: a model writes "1442.0" as readily as "1442", and <c>TryGetInt32</c>
    /// refuses the first. False when it is absent or not a number. See <see cref="BotRevel.Ring"/> for what a camp without
    /// one used to do.
    /// </summary>
    private static bool Tile(JsonElement root, string name, out int value)
    {
        value = 0;

        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        if (element.TryGetInt32(out value))
        {
            return true;
        }

        if (!element.TryGetDouble(out var real) || real is <= int.MinValue or >= int.MaxValue)
        {
            return false;
        }

        value = (int)Math.Round(real);

        return true;
    }

    private static int Whole(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }

        if (value.TryGetInt32(out var got))
        {
            return got;
        }

        return value.TryGetDouble(out var real) && real is > int.MinValue and < int.MaxValue ? (int)Math.Round(real) : 0;
    }
}
