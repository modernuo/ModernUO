using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// What a kind costs a company, learned from companies' own fights against it, apart from what it costs the island as a whole
/// (<see cref="BotQuarry.Price"/>, which stays the lone bot's price).
///
/// <para>
/// <b>Patrick, 03.10.2026 ~12:40</b>, asked whether a company should price a kind apart from lone bots, since companies of ten
/// and more had put a Reaper down 14 times in 17 while the island's price (×4.8) kept every company off it: "if they kill it, let
/// them gather — what matters is that they level up, and the higher the level, the higher the chance to kill later." The level
/// is already in the scale a company is weighed by: <c>BotThreat.Power</c> grows with the weapon skill a bot lands its blows by
/// and the health its strength gives it, and a company is the sum of its members' (<c>BotSquad.Able</c>). So the record is kept
/// on that scale — every fight at the odds the company took it on at, its strength over the creature's — and a company that has
/// grown is weighed against companies as strong as it has become.
/// </para>
///
/// <para>
/// <b>Measured on build 396 (04:29–12:47, 210 bots, <c>agents/reaper13-tools/</c>):</b> companies took a Reaper on 153 times
/// and put it down 103 times — companies of ten and more 20 times in 23, of five to nine 56 in 79, of two to four 27 in 48. Of
/// the deaths of ours to Reapers, 106 were lone bots (no lone bot killed one) and 104 in companies; but of the 85 enlisted
/// members among them only 20 fell in the company's own fight against a Reaper (and a few more within two minutes of it, the
/// field's next one), while 56 fell as the company fought something else beside a Reaper it had not taken on and 9 as it fought
/// nothing. The island's record prices them all against the kind's kills, so a company of thirteen at twice a Reaper's strength
/// was reckoned at 47,000 and turned away from the one fight that would have ended the casting. The same split holds for every
/// killer of enlisted members: in the company's own fight, giant serpent 7 of 49, giant spider 1 of 57, troll 0 of 30.
/// </para>
///
/// <para>
/// <b>What is recorded:</b> a company's kill (<c>BotSquad.Judge</c>, "it is down") at the odds it took the creature on at; a
/// member's death to a wild creature of a kind the company was fighting, or had fought within <see cref="FightMs"/>, at the odds
/// it took that kind on at over the killer's strength (<c>BotSquad.Fell</c>, from <c>BotKept.Fell</c>). Companies of two or more
/// only. A death in a company to a kind it was not fighting is the island's record alone (<see cref="Elsewhere"/>): it says
/// something about walking beside the kind, which is the walk's business, and nothing about taking it on. Both are weighed by
/// the island's own half-life (<see cref="BotQuarry.RiskHalfMs"/>).
/// </para>
///
/// <para>
/// <b>How a kind is priced for a company</b> (<see cref="Price"/>): the island's price is the ceiling; the record can only lower
/// it, and only for a company at least as strong, relative to the kind, as companies that have beaten it. A company at odds
/// <c>r</c> asks of the record the kills by companies no stronger than it (<c>K</c>, at least <see cref="Vouch"/> of them) and
/// the deaths in the fights of companies up to <see cref="Margin"/> times stronger (<c>D</c>: a company somewhat stronger than
/// one that lost is not thereby safe), puts them through the island's own formula (<see cref="BotQuarry.Formula"/>: nothing
/// under <see cref="BotQuarry.RiskLeast"/> deaths, a quarter of a death per kill fair) and is let in when its odds carry that
/// price at the company's yardstick (<c>BotSquad.WithdrawOdds</c>). The lowest odds let in, times the yardstick, is the kind's
/// company price; a company below it is priced out exactly where companies like it have lost, or where none like it has won.
/// A kind no company has put down <see cref="Vouch"/> times keeps the island's price.
/// </para>
///
/// <para>
/// <b>Replayed on 396 after 05:19</b> (strengths from the members' powers on 395, the median otherwise; <c>replay_company_price.py</c>):
/// of 6219 company engagements the island's price would have kept companies off 582; the company record lets 178 of them in —
/// 101 Reapers, 29 gargoyles, 23 giant serpents, 13 wisps — of which 99 went down with 21 deaths of ours counted in them, and
/// keeps 404 off (151 down, 16 deaths). The Reaper's company price sat at ×1.0–1.5 (odds 0.67–1.0 let in) against the island's
/// ×3.4–5.7; the giant serpent's at ×2.5–4.2 against ×3.6–6.1; the air elemental stayed at the island's ×8, no company having
/// put enough of them down.
/// </para>
/// </summary>
public static class BotCompanyRecord
{
    /// <summary>Whether a company prices a kind by companies' own fights. Off, every company weighs every kind at the island's price, as before.</summary>
    public static bool Prices { get; set; } = true;

    /// <summary>Kills, as the hour weighs them, by companies no stronger than the one asking before the record may lower its price. Three.</summary>
    public static double Vouch { get; set; } = 3.0;

    /// <summary>How much stronger than a company that lost a company must be before that death stops counting against it. Half again.</summary>
    public static double Margin { get; set; } = 1.5;

    /// <summary>
    /// How long after a company's fight against a kind a member's death to that kind is still that fight's. Two minutes: on 396, 8 of
    /// the 12 enlisted deaths to a Reaper within ten minutes of the company's last Reaper fell inside two — the field's next one.
    /// </summary>
    public static int FightMs { get; set; } = 120000;

    /// <summary>How long a kind's company figure stands before it is worked out again from its record.</summary>
    public static int CacheMs { get; set; } = 5000;

    /// <summary>The most fights a kind's record holds; the oldest go first.</summary>
    public static int MostEvents { get; set; } = 256;

    private struct Fight
    {
        public double Odds;

        public bool Death;

        public long Tick;
    }

    private sealed class Book
    {
        public readonly List<Fight> Fights = [];

        /// <summary>The lowest odds the record lets a company in at; infinity while it vouches for none.</summary>
        public double Needed = double.PositiveInfinity;

        public double Kills;

        public double Deaths;

        public long Worked;

        public bool Stale = true;
    }

    private static readonly Dictionary<Type, Book> _books = [];

    /// <summary>Company kills and deaths in companies' own fights recorded; deaths in a company to a kind it was not fighting, left to the island.</summary>
    public static long Kills { get; private set; }

    public static long Deaths { get; private set; }

    public static long Elsewhere { get; private set; }

    /// <summary>
    /// Creatures a company chose, joined, kept or answered on the company record where the island's price would have put them
    /// beyond it (<c>BotSquad.Beyond</c>), once a company a creature; withdrawals the island's price would have made and the record
    /// did not (<c>BotSquad.Overmatched</c>), once a company a creature.
    /// </summary>
    public static long LetIn { get; private set; }

    public static long Stood { get; private set; }

    /// <summary>Creatures a look for a company's quarry, and zones a hunter's look for company ground, let in on the record.</summary>
    public static long LookLetIn { get; private set; }

    public static long ZoneLetIn { get; private set; }

    /// <summary>Let-ins and withdrawals not made, by kind, for the summary.</summary>
    private static readonly Dictionary<Type, (long LetIn, long Stood)> _byKind = [];

    internal static void Let(Mobile foe)
    {
        LetIn++;
        Tally(foe, true);
    }

    internal static void Stand(Mobile foe)
    {
        Stood++;
        Tally(foe, false);
    }

    internal static void Looked() => LookLetIn++;

    internal static void Zoned() => ZoneLetIn++;

    private static void Tally(Mobile foe, bool letIn)
    {
        if (foe == null)
        {
            return;
        }

        var kind = foe.GetType();

        if (_byKind.Count > 256 && !_byKind.ContainsKey(kind))
        {
            return;
        }

        _byKind.TryGetValue(kind, out var had);
        _byKind[kind] = letIn ? (had.LetIn + 1, had.Stood) : (had.LetIn, had.Stood + 1);
    }

    /// <summary>A company of two or more put one of these down, having taken it on at <paramref name="odds"/> (its strength over the creature's).</summary>
    public static void Killed(Type kind, double odds)
    {
        if (kind == null || !(odds > 0.0) || double.IsInfinity(odds))
        {
            return;
        }

        Kills++;
        Add(kind, odds, false);
    }

    /// <summary>One of ours fell to one of these in its company's fight against the kind, the company having taken it on at <paramref name="odds"/>.</summary>
    public static void Fell(Type kind, double odds)
    {
        if (kind == null || !(odds > 0.0) || double.IsInfinity(odds))
        {
            return;
        }

        Deaths++;
        Add(kind, odds, true);
    }

    /// <summary>One of ours fell in a company to a kind the company was not fighting: the island's record only. See the class note.</summary>
    public static void FellElsewhere() => Elsewhere++;

    private static void Add(Type kind, double odds, bool death)
    {
        if (!_books.TryGetValue(kind, out var book))
        {
            if (_books.Count >= 1024)
            {
                _books.Clear();
            }

            book = new Book();
            _books[kind] = book;
        }

        if (book.Fights.Count >= Math.Max(8, MostEvents))
        {
            book.Fights.RemoveAt(0);
        }

        book.Fights.Add(new Fight { Odds = odds, Death = death, Tick = Core.TickCount });
        book.Stale = true;
    }

    /// <summary>The company's yardstick: what a company withdraws past and chooses within (<c>BotSquad.WithdrawOdds</c>).</summary>
    private static double Yardstick => Math.Max(0.1, BotSquad.WithdrawOdds);

    /// <summary>
    /// What a kind's strength is multiplied by when a company weighs it: the island's price (<see cref="BotQuarry.Price"/>), lowered
    /// to the company yardstick times the lowest odds the companies' record lets a company in at, never under one. See the class
    /// note. The island's price wherever the record is off, empty or vouches for no odds.
    /// </summary>
    public static double Price(Type kind)
    {
        var island = BotQuarry.Price(kind);

        if (!Prices || kind == null || island <= 1.0 || !_books.TryGetValue(kind, out var book))
        {
            return island;
        }

        var needed = Needed(book);

        return double.IsInfinity(needed) ? island : Math.Clamp(Yardstick * needed, 1.0, island);
    }

    /// <summary>The strength a company must bring to choose this creature: its own, times its kind's company price.</summary>
    public static double Priced(BaseCreature creature) =>
        creature == null ? 0.0 : BotThreat.Power(creature) * Price(creature.GetType());

    private static readonly List<double> _points = [];

    private static readonly List<(double Odds, bool Death, double Weight)> _weighed = [];

    /// <summary>
    /// The lowest odds the record lets a company in at, worked out at most every <see cref="CacheMs"/>: over the breakpoints of the
    /// record (every kill's odds, every death's odds times <see cref="Margin"/>), the first span where at least <see cref="Vouch"/>
    /// kills by companies no stronger stand and the odds carry the island's formula of what is left.
    /// </summary>
    private static double Needed(Book book)
    {
        var now = Core.TickCount;

        if (!book.Stale && now - book.Worked < CacheMs)
        {
            return book.Needed;
        }

        book.Stale = false;
        book.Worked = now;
        book.Needed = double.PositiveInfinity;
        book.Kills = 0.0;
        book.Deaths = 0.0;

        _weighed.Clear();
        _points.Clear();

        var half = Math.Max(1, BotQuarry.RiskHalfMs);
        var margin = Math.Max(1.0, Margin);

        // Forgotten past a hundredth of their weight (six and two-thirds half-lives), so the record does not grow without end.
        // Compacted in place: this is asked from every company's beat, and a predicate capturing the clock is a new delegate a call.
        var kept = 0;

        for (var i = 0; i < book.Fights.Count; i++)
        {
            if (now - book.Fights[i].Tick <= half * 6.65)
            {
                book.Fights[kept++] = book.Fights[i];
            }
        }

        if (kept < book.Fights.Count)
        {
            book.Fights.RemoveRange(kept, book.Fights.Count - kept);
        }

        foreach (var fight in book.Fights)
        {
            var weight = Math.Pow(0.5, Math.Max(0L, now - fight.Tick) / (double)half);

            _weighed.Add((fight.Odds, fight.Death, weight));

            if (fight.Death)
            {
                book.Deaths += weight;
                _points.Add(fight.Odds * margin);
            }
            else
            {
                book.Kills += weight;
                _points.Add(fight.Odds);
            }
        }

        if (book.Kills < Vouch)
        {
            return book.Needed;
        }

        _points.Add(0.0);
        _points.Sort();

        for (var i = 0; i < _points.Count; i++)
        {
            var at = _points[i];

            if (i + 1 < _points.Count && _points[i + 1] <= at)
            {
                continue;
            }

            var kills = 0.0;
            var deaths = 0.0;

            for (var j = 0; j < _weighed.Count; j++)
            {
                var (odds, death, weight) = _weighed[j];

                if (death ? odds * margin > at : odds <= at)
                {
                    if (death)
                    {
                        deaths += weight;
                    }
                    else
                    {
                        kills += weight;
                    }
                }
            }

            if (kills < Vouch)
            {
                continue;
            }

            var let = Math.Max(at, BotQuarry.Formula(deaths, kills) / Yardstick);
            var next = double.PositiveInfinity;

            for (var k = i + 1; k < _points.Count; k++)
            {
                if (_points[k] > at)
                {
                    next = _points[k];

                    break;
                }
            }

            if (let < next)
            {
                book.Needed = let;

                break;
            }
        }

        return book.Needed;
    }

    /// <summary>Everything forgotten with the world. See <c>BotQuarry.Forget</c>.</summary>
    public static void Forget()
    {
        _books.Clear();
        _byKind.Clear();
        Kills = 0;
        Deaths = 0;
        Elsewhere = 0;
        LetIn = 0;
        Stood = 0;
        LookLetIn = 0;
        ZoneLetIn = 0;
    }

    /// <summary>The company record's clause of the Companies line.</summary>
    public static string Describe()
    {
        if (!Prices)
        {
            return "the company record: off (every company weighs every kind at the island's price)";
        }

        List<(string Name, double Company, double Island, double Odds, double Kills, double Deaths)> lower = [];
        var island = 0;

        foreach (var (kind, book) in _books)
        {
            var needed = Needed(book);
            var price = BotQuarry.Price(kind);

            if (price <= 1.0)
            {
                continue;
            }

            var company = Price(kind);

            if (company < price)
            {
                lower.Add((kind.Name, company, price, needed, book.Kills, book.Deaths));
            }
            else
            {
                island++;
            }
        }

        lower.Sort(static (a, b) => (b.Island - b.Company).CompareTo(a.Island - a.Company));

        using var sb = ValueStringBuilder.Create(512);

        for (var i = 0; i < lower.Count && i < 8; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            var (name, company, price, odds, kills, deaths) = lower[i];
            sb.Append($"{name} x{company:F1} against x{price:F1} (companies at {odds:F2} of its strength let in; {kills:F0} of them put down and {deaths:F1} of ours lost in companies' fights)");
        }

        var stood = ValueStringBuilder.Create(128);

        try
        {
            foreach (var (kind, n) in _byKind)
            {
                if (stood.Length > 0)
                {
                    stood.Append(", ");
                }

                stood.Append($"{kind.Name} {n.LetIn}/{n.Stood}");
            }

            return $"the company record (a kind priced by companies' own fights, never above the island's price): {Kills} company kills and {Deaths} deaths of ours in companies' fights recorded, {Elsewhere} deaths in a company to a kind it was not fighting left to the island's record; kinds priced below the island for companies: {(lower.Count > 0 ? sb.ToString() : "none")}; {island} priced kinds still at the island's price for companies (no {Vouch:F0} company kills vouching, or the record no better); {LetIn} creatures a company chose, joined or answered and {Stood} withdrawals not made on the company record that the island's price would have refused (by kind, let in/stood: {(stood.Length > 0 ? stood.ToString() : "none")}), {LookLetIn} creatures a look for a company's quarry and {ZoneLetIn} zones a look for company ground let in on it";
        }
        finally
        {
            stood.Dispose();
        }
    }
}
