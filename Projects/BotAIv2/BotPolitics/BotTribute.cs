using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Ultimatums and tribute: a strong guild about to make war on a weak one — at the duke's word, or by seizing its ground —
/// demands so much a day instead, and the weak pays from its chest while it can.
///
/// <para>
/// <b>Patrick's order of 07.10.2026, night: "ultimatums and tribute: a strong guild demands tribute from a neighbour instead of
/// war, the weak pays or fights"</b>, and his standing rule that the weak do not declare on the strong and capitulate
/// (<c>BotWar.DauntedAt</c>, <c>BotWar.YieldOdds</c>). Tribute is the peaceful form of that capitulation: the war that would
/// have cost the weak its dead and its ground costs it coin instead, and the strong gets the coin without the blood.
/// </para>
///
/// <para>
/// <b>When.</b> Only where a war was about to begin: the duke's word was war between two guilds and the war's rules allowed it
/// (<c>BotDuke.Apply</c>), or a guild weighed a held square and would seize it (<c>BotSeizure.Judge</c>). Only when the would-be
/// declarer's might (<c>BotWar.Might</c>) is <see cref="Odds"/> times the other's or more, and not within
/// <see cref="RefuseMs"/> of the same guild refusing the same demand.
/// </para>
///
/// <para>
/// <b>The terms.</b> <see cref="Share"/> of the weak chest a day, between <see cref="Least"/> and <see cref="Most"/>gp, paid an
/// instalment every <see cref="EveryMs"/> from chest to chest (<c>BotChest.Pay</c>: nothing made, nothing lost). The weak pays
/// when its chest holds the first instalment, and the first is paid at once; otherwise it refuses and the war follows by the
/// existing rules. While the tribute stands, neither side may declare on the other (<c>BotWar.MayDeclare</c>) and the strong
/// does not seize the weak's ground. It ends after <see cref="Hours"/>, when the strong falls under <see cref="EndOdds"/> times
/// the weak's might, or when an instalment cannot be paid — and then the strong thinks <see cref="LapseRegard"/> worse of the
/// weak, and the grievance takes its course.
/// </para>
/// </summary>
public static class BotTribute
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTribute));

    /// <summary>The word a meeting ends with when the host pays tribute. See <c>BotDuke.Apply</c>.</summary>
    public const string Word = "tribute";

    /// <summary>Whether the strong demand tribute before a war on the weak.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How many times the other's might a guild must be to send an ultimatum. Two: Patrick's "several times".</summary>
    public static double Odds { get; set; } = 2.0;

    /// <summary>The tribute ends when the payee falls under this many times the payer's might: the balance has turned.</summary>
    public static double EndOdds { get; set; } = 1.5;

    /// <summary>How long a tribute stands, in hours of running time.</summary>
    public static double Hours { get; set; } = 24.0;

    /// <summary>How often an instalment is paid: an hour.</summary>
    public static int EveryMs { get; set; } = 3600000;

    /// <summary>The share of the weak chest demanded a day.</summary>
    public static double Share { get; set; } = 0.25;

    /// <summary>The least and the most demanded a day.</summary>
    public static int Least { get; set; } = 240;

    public static int Most { get; set; } = 4800;

    /// <summary>How long after a refusal no new ultimatum goes between the same pair: six hours, so the war may follow.</summary>
    public static int RefuseMs { get; set; } = 21600000;

    /// <summary>What a tribute left unpaid costs of the payee's regard of the payer.</summary>
    public static double LapseRegard { get; set; } = -30.0;

    /// <summary>One tribute standing.</summary>
    private sealed class Pact
    {
        public string Payer;

        public string Payee;

        public int Daily;

        public int Paid;

        public int Instalments;

        public long Began;

        public long Last;

        public string Over;
    }

    private static readonly List<Pact> _pacts = [];

    private static readonly Dictionary<(string Strong, string Weak), long> _refused = [];

    /// <summary>Ultimatums sent, paid and refused; wars or seizures spared by a tribute already standing.</summary>
    public static long Sent { get; private set; }

    public static long Accepted { get; private set; }

    public static long Refused { get; private set; }

    public static long Spared { get; private set; }

    /// <summary>Instalments paid and their gold, the first ones included.</summary>
    public static long Instalments { get; private set; }

    public static long Gold { get; private set; }

    /// <summary>Tributes ended, by how.</summary>
    public static long Served { get; private set; }

    public static long Turned { get; private set; }

    public static long Lapsed { get; private set; }

    public static long Gone { get; private set; }

    /// <summary>Declarations refused because a tribute stood between the two. See <see cref="Forbids"/>.</summary>
    public static long Forbade { get; private set; }

    private static int Instalment(int daily) => Math.Max(1, (int)Math.Ceiling(daily * (Math.Max(60000, EveryMs) / 86400000.0)));

    private static Pact Find(string payer, string payee)
    {
        for (var i = 0; i < _pacts.Count; i++)
        {
            if (_pacts[i].Payer == payer && _pacts[i].Payee == payee)
            {
                return _pacts[i];
            }
        }

        return null;
    }

    /// <summary>Whether this guild is paying tribute to anybody. Asked by <c>BotPurpose</c> for the war chest.</summary>
    public static bool Pays(string guild)
    {
        for (var i = 0; i < _pacts.Count; i++)
        {
            if (_pacts[i].Payer == guild)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a tribute standing between the two forbids <paramref name="mine"/> declaring on <paramref name="theirs"/>. Asked
    /// by <c>BotWar.MayDeclare</c>; counted when it refuses.
    /// </summary>
    public static bool Forbids(string mine, string theirs, out string why)
    {
        why = null;

        if (_pacts.Count == 0 || mine == null || theirs == null)
        {
            return false;
        }

        var p = Find(theirs, mine) ?? Find(mine, theirs);

        if (p == null)
        {
            return false;
        }

        Forbade++;
        why = $"{p.Payer} pays {p.Payee} a tribute of {p.Daily}gp a day, {Left(p, Core.TickCount)} hours left";

        return true;
    }

    private static int Left(Pact p, long now) => (int)Math.Max(0L, ((long)(Hours * 3600000) - (now - p.Began)) / 3600000);

    /// <summary>
    /// The ultimatum: <paramref name="strong"/> is about to make war on <paramref name="weak"/> over <paramref name="over"/>. True
    /// when a tribute stands between them now — already, or because the weak has just agreed and paid the first instalment — and
    /// the war or the seizure does not happen; <paramref name="terms"/> says so. False, and the caller goes on as before, when
    /// the strong is not <see cref="Odds"/> times the weak's might, or the weak refused (or refused lately).
    /// </summary>
    public static bool Demand(Guild strong, Guild weak, string over, out string terms)
    {
        terms = null;

        if (!Running || strong?.Name == null || weak?.Name == null || strong == weak || BotUnderworld.Band(strong) || BotUnderworld.Band(weak))
        {
            return false;
        }

        var now = Core.TickCount;

        if (Find(weak.Name, strong.Name) is { } standing)
        {
            Spared++;
            terms = $"{weak.Name} pays {strong.Name} a tribute of {standing.Daily}gp a day already, {Left(standing, now)} hours left";

            return true;
        }

        var ours = BotWar.Might(strong.Name);
        var theirs = BotWar.Might(weak.Name);

        if (Odds <= 0.0 || theirs <= 0.0 || ours < theirs * Odds)
        {
            return false;
        }

        var pair = (strong.Name, weak.Name);

        if (_refused.TryGetValue(pair, out var at) && now - at < RefuseMs)
        {
            return false;
        }

        Sent++;

        var chest = BotChest.Holds(weak.Name);
        var daily = Math.Clamp((int)(chest * Math.Max(0.0, Share)), Math.Max(1, Least), Math.Max(Least, Most));
        var first = Instalment(daily);
        var fill = new Dictionary<string, string> { ["coin"] = daily.ToString() };

        if (chest < first)
        {
            Refused++;
            _refused[pair] = now;

            logger.Warning(
                "{Weak} refused {Strong}'s ultimatum over {Over} — {Daily}gp a day or war: its chest holds {Chest}gp, under the first {First}gp; might {Ours:F0} against {Theirs:F0} ({Ratio:F1} times), and the war goes on by its rules",
                weak.Name,
                strong.Name,
                over,
                daily,
                chest,
                first,
                ours,
                theirs,
                ours / theirs
            );

            fill["enemy"] = strong.Name;
            BotVoice.ToGuild(weak.Name, "politics:refused", fill);
            fill["enemy"] = weak.Name;
            BotVoice.ToGuild(strong.Name, "politics:defied", fill);
            BotEvents.Post("politics", BotGuilds.Head(weak), $"{weak.Name} cannot pay {strong.Name}'s tribute of {daily}gp a day: war");

            return false;
        }

        var paid = BotChest.Pay(weak.Name, strong.Name, first);

        _pacts.Add(
            new Pact
            {
                Payer = weak.Name,
                Payee = strong.Name,
                Daily = daily,
                Paid = paid,
                Instalments = 1,
                Began = now,
                Last = now,
                Over = over
            }
        );

        Accepted++;
        Instalments++;
        Gold += paid;

        // The tribute settles the quarrel: both opinions back to nought, as a war's end does. See BotRegard.Settle.
        BotRegard.Settle(strong.Name, weak.Name);

        logger.Warning(
            "{Strong} sent {Weak} an ultimatum over {Over} — {Daily}gp a day or war — and {Weak2} pays: might {Ours:F0} against {Theirs:F0} ({Ratio:F1} times), {Paid}gp at once out of a chest of {Chest}gp, for {Hours:0.#}h unless the balance turns under {End:0.#} times",
            strong.Name,
            weak.Name,
            over,
            daily,
            weak.Name,
            ours,
            theirs,
            ours / theirs,
            paid,
            chest,
            Hours,
            EndOdds
        );

        fill["enemy"] = weak.Name;
        BotVoice.ToGuild(strong.Name, "politics:ultimatum", fill);
        fill["enemy"] = strong.Name;
        BotVoice.ToGuild(weak.Name, "politics:tribute", fill);
        BotEvents.Post("politics", BotGuilds.Head(strong), $"{weak.Name} pays {strong.Name} a tribute of {daily}gp a day instead of war ({ours / theirs:F1} times its might)");

        terms = $"tribute: {weak.Name} pays {strong.Name} {daily}gp a day for {Hours:0.#}h, {ours / theirs:F1} times its might";

        return true;
    }

    /// <summary>Once a minute: instalments due, and tributes that have run out, turned or gone unpaid.</summary>
    public static void Look(long now)
    {
        for (var i = _pacts.Count - 1; i >= 0; i--)
        {
            var p = _pacts[i];

            if (BotGuilds.Named(p.Payer) is not { Disbanded: false } || BotGuilds.Named(p.Payee) is not { Disbanded: false })
            {
                End(i, "a guild is gone");
                Gone++;

                continue;
            }

            if (now - p.Began >= (long)(Hours * 3600000))
            {
                End(i, $"it served its {Hours:0.#} hours");
                Served++;

                continue;
            }

            var payee = BotWar.Might(p.Payee);
            var payer = BotWar.Might(p.Payer);

            if (payer > 0.0 && payee < payer * EndOdds)
            {
                End(i, $"the balance turned: {p.Payee}'s might is {payee / payer:F1} times {p.Payer}'s");
                Turned++;

                continue;
            }

            if (now - p.Last < EveryMs)
            {
                continue;
            }

            var due = Instalment(p.Daily);

            if (BotChest.Holds(p.Payer) < due)
            {
                Lapsed++;
                _refused[(p.Payee, p.Payer)] = now;
                BotRegard.Move(p.Payee, p.Payer, LapseRegard, "tribute unpaid");
                End(i, $"{p.Payer}'s chest could not pay the {due}gp due");

                continue;
            }

            var paid = BotChest.Pay(p.Payer, p.Payee, due);

            p.Paid += paid;
            p.Instalments++;
            p.Last = now;
            Instalments++;
            Gold += paid;

            logger.Information(
                "{Payer} paid {Payee} {Paid}gp of tribute, {Total}gp in {Count} instalments; {Left}h left",
                p.Payer,
                p.Payee,
                paid,
                p.Paid,
                p.Instalments,
                Left(p, now)
            );
        }
    }

    private static void End(int index, string why)
    {
        var p = _pacts[index];

        _pacts.RemoveAt(index);

        logger.Information(
            "{Payer}'s tribute to {Payee} has ended: {Why}; {Paid}gp paid in {Count} instalments over {Minutes} minutes",
            p.Payer,
            p.Payee,
            why,
            p.Paid,
            p.Instalments,
            (Core.TickCount - p.Began) / 60000
        );

        var fill = new Dictionary<string, string> { ["enemy"] = p.Payee, ["reason"] = why };

        BotVoice.ToGuild(p.Payer, "politics:tribute-end", fill);
    }

    // ---- telling -----------------------------------------------------------------------------------------------------------

    public static void Write(Utf8JsonWriter w, string guild)
    {
        w.WritePropertyName("tribute");
        w.WriteStartArray();

        var now = Core.TickCount;

        for (var i = 0; i < _pacts.Count; i++)
        {
            var p = _pacts[i];

            if (p.Payer != guild && p.Payee != guild)
            {
                continue;
            }

            w.WriteStartObject();
            w.WriteString("payer", p.Payer);
            w.WriteString("payee", p.Payee);
            w.WriteNumber("daily", p.Daily);
            w.WriteNumber("paid", p.Paid);
            w.WriteNumber("instalments", p.Instalments);
            w.WriteNumber("hoursLeft", Left(p, now));
            BotJson.StringOrNull(w, "over", p.Over);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    /// <summary>The <c>Diplomacy:</c> line's clause.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "ultimatums are OFF";
        }

        using var say = ValueStringBuilder.Create(384);

        say.Append(
            $"ultimatums at {Odds:0.#} times the might, {Share:P0} of the weak chest a day ({Least}–{Most}gp) paid every {EveryMs / 60000}m for {Hours:0.#}h, ended under {EndOdds:0.#} times: {Sent} sent, {Accepted} paid, {Refused} refused for a chest that could not pay, {Spared} wars or seizures spared by a tribute standing; {Instalments} instalments of {Gold}gp chest to chest ({BotChest.Transfers} transfers of {BotChest.TransferGold}gp on the chests' books); ended: {Served} served, {Turned} turned, {Lapsed} unpaid, {Gone} a guild gone; {Forbade} declarations refused for a tribute standing; standing: "
        );

        if (_pacts.Count == 0)
        {
            say.Append("none");
        }

        var now = Core.TickCount;

        for (var i = 0; i < _pacts.Count; i++)
        {
            var p = _pacts[i];

            say.Append(i == 0 ? "" : ", ");
            say.Append($"{p.Payer} pays {p.Payee} {p.Daily}gp/day ({p.Paid}gp paid, {Left(p, now)}h left)");
        }

        return say.ToString();
    }

    // ---- the store ---------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(_pacts.Count);

        for (var i = 0; i < _pacts.Count; i++)
        {
            var p = _pacts[i];

            writer.Write(p.Payer);
            writer.Write(p.Payee);
            writer.WriteEncodedInt(p.Daily);
            writer.WriteEncodedInt(p.Paid);
            writer.WriteEncodedInt(p.Instalments);
            writer.Write(Math.Max(0L, now - p.Began));
            writer.Write(Math.Max(0L, now - p.Last));
            writer.Write(p.Over ?? "");
        }

        var kept = 0;

        foreach (var (_, at) in _refused)
        {
            if (now - at < RefuseMs)
            {
                kept++;
            }
        }

        writer.WriteEncodedInt(kept);

        foreach (var (pair, at) in _refused)
        {
            if (now - at < RefuseMs)
            {
                writer.Write(pair.Strong);
                writer.Write(pair.Weak);
                writer.Write(Math.Max(0L, now - at));
            }
        }
    }

    internal static int Load(IGenericReader reader)
    {
        var now = Core.TickCount;
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var payer = reader.ReadString();
            var payee = reader.ReadString();
            var daily = reader.ReadEncodedInt();
            var paid = reader.ReadEncodedInt();
            var instalments = reader.ReadEncodedInt();
            var began = reader.ReadLong();
            var last = reader.ReadLong();
            var over = reader.ReadString();

            if (string.IsNullOrEmpty(payer) || string.IsNullOrEmpty(payee))
            {
                continue;
            }

            _pacts.Add(
                new Pact
                {
                    Payer = payer,
                    Payee = payee,
                    Daily = daily,
                    Paid = paid,
                    Instalments = instalments,
                    Began = now - Math.Clamp(began, 0L, (long)int.MaxValue),
                    Last = now - Math.Clamp(last, 0L, (long)int.MaxValue),
                    Over = string.IsNullOrEmpty(over) ? null : over
                }
            );
        }

        var refusals = reader.ReadEncodedInt();

        for (var i = 0; i < refusals; i++)
        {
            var strong = reader.ReadString();
            var weak = reader.ReadString();
            var ago = reader.ReadLong();

            if (!string.IsNullOrEmpty(strong) && !string.IsNullOrEmpty(weak))
            {
                _refused[(strong, weak)] = now - Math.Clamp(ago, 0L, (long)int.MaxValue);
            }
        }

        return count;
    }

    public static void Forget()
    {
        Sent = 0;
        Accepted = 0;
        Refused = 0;
        Spared = 0;
        Instalments = 0;
        Gold = 0;
        Served = 0;
        Turned = 0;
        Lapsed = 0;
        Gone = 0;
        Forbade = 0;
    }

    /// <summary>
    /// Every tribute and every remembered refusal forgotten, for Patrick's reset (07.10.2026, reset26): exactly what
    /// <see cref="Save"/> writes. A tribute is paid instalment by instalment chest to chest (<c>BotChest.Pay</c>), nothing held
    /// between them, so ending it moves no gold. See <see cref="BotPolitics.Wipe"/>. Returns the tributes ended.
    /// </summary>
    public static int Wipe()
    {
        var pacts = _pacts.Count;

        _pacts.Clear();
        _refused.Clear();
        Forget();

        return pacts;
    }
}
