using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The duke: what two guilds' envoys say before the witness, the facts the duke is given, the answers he may give, the rule
/// that answers when the model cannot, and what each answer does.
///
/// <para>
/// <b>Patrick's words of 29.09.2026: the envoys state their claims, and the one who decides is prompted "you are the duke of
/// this land; your vassals quarrel over land: one says the other attacks it too often, the other that the first crosses its
/// borders; the first takes the reward from the second's fighters but does it on its own land" — nothing grand, but so the
/// world comes alive.</b> The model is the duke and is asked through a hook the thinking assembly fills
/// (<see cref="BotParley.Judge"/>); what it is told is written here, from what the shard actually knows — each side's book of
/// grievances (<see cref="BotGrievances"/>), their opinions, their strength, their ground, their agreements and wars — and what
/// it may answer is an enumeration narrowed per topic (<see cref="Allowed"/>), so a duke cannot give an alliance to a pair that
/// came to declare war, nor a war to a pair that came to trade. DECISIONS N2: narrow the enumeration instead of persuading.
/// </para>
///
/// <para>
/// <b>The rule is not a lesser duke, it is the same judgement in arithmetic.</b> Ollama may be off, the one slot may be held
/// by a reflection for seven minutes, or the answer may be unreadable; the meeting must still end, and end the same way the
/// facts point. So <see cref="Rule"/> reads the same book the model is shown: blood, a claim on the hall's own ground or a
/// grievance both ways is war; a quarrel of trespass and borders between guilds that trade is trade terms; the rest is peace.
/// </para>
/// </summary>
public static class BotDuke
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDuke));

    public const string War = "war";

    public const string Peace = "peace";

    public const string Trade = "trade";

    public const string Orders = "orders";

    public const string Alliance = "alliance";

    public const string Nothing = "nothing";

    /// <summary>Every outcome a meeting between two guilds can have, in the order they are offered.</summary>
    public static readonly string[] Outcomes = [War, Peace, Trade, Orders, Alliance, Nothing];

    /// <summary>
    /// How many trades between the two (both books together) make a quarrel the rule settles with trade terms rather than a
    /// bare peace. Three: a pair that has bought off each other's stalls three times since the last settlement is trading.
    /// </summary>
    public static int TradesForTerms { get; set; } = 3;

    /// <summary>Verdicts, by who gave them.</summary>
    public static long ByModel { get; private set; }

    public static long ByRule { get; private set; }

    /// <summary>Verdicts by outcome.</summary>
    private static readonly Dictionary<string, long> _outcomes = new(StringComparer.Ordinal);

    public static string Word(BotTopic topic) =>
        topic switch
        {
            BotTopic.Grievance => "a grievance",
            BotTopic.Safety => "peace between them",
            BotTopic.Friendship => "an alliance",
            BotTopic.Commerce => "trade",
            _ => "the town's favour"
        };

    /// <summary>
    /// What the duke may answer on this topic, between these two, now. An alliance only when both have room for one
    /// (<c>BotRegard.MostAllies</c>); nothing on a grievance, because a pair at the war line that the duke sends home unsettled
    /// is back at his door at the next drift.
    /// </summary>
    public static string[] Allowed(BotTopic topic, Guild guest, Guild host)
    {
        var room = guest != null && host != null
            && (guest.Allies?.Count ?? 0) < BotRegard.MostAllies && (host.Allies?.Count ?? 0) < BotRegard.MostAllies;

        return topic switch
        {
            BotTopic.Grievance => [War, Peace, Trade, Orders],
            BotTopic.Safety => [Peace, Trade, Orders, Nothing],
            BotTopic.Friendship => room ? [Alliance, Trade, Orders, Nothing] : [Trade, Orders, Nothing],
            BotTopic.Commerce => [Trade, Orders, Nothing],
            _ => [Nothing]
        };
    }

    /// <summary>What an outcome does, in the duke's brief: the mechanism, in words, so the choice is made knowing its price.</summary>
    public static string Means(string outcome) =>
        outcome switch
        {
            War => $"war: declared now, fought until one side has killed {BotWar.Kills} of the other or taken {BotWar.Loot}gp off their dead, or judged after {BotWar.LongestMs / 60000} minutes; allies join it",
            Peace => $"peace: both grievances are wiped, and neither may declare war on the other for {BotPact.Hours:0.#} hours",
            Trade => $"trade terms: for {BotPact.Hours:0.#} hours each buys off the other's stalls at {BotPact.TradeDiscount:P0} off (a grievance is wiped by it)",
            Orders => $"standing orders: for {BotPact.Hours:0.#} hours each side's makers fill the other's orders before anybody else's (a grievance is wiped by it)",
            Alliance => "an alliance: each joins the other's wars, and their people count as allies",
            _ => $"nothing: no agreement; they may come before you again in {BotParley.RetryMs / 60000} minutes"
        };

    // ---- the hearing -------------------------------------------------------------------------------------------------

    /// <summary>
    /// The lines of the hearing, in order: the witness opens, the envoy states its case from its book, the host answers from
    /// its own (or the witness says the host sent nobody), and the witness says the duke will weigh it. Every line comes out
    /// of the phrase bank (<c>bot-voice.json</c>, keys <c>parley:*</c>), so the words can be changed without the code.
    /// </summary>
    public static void Hearing(BotHearing m)
    {
        m.Lines.Clear();

        var fill = Fill(m);

        m.Lines.Add((BotSpeaker.Witness, Phrase("parley:open", m.Witness, fill)));

        switch (m.Topic)
        {
            case BotTopic.Grievance:
                {
                    Causes(m, BotSpeaker.Envoy, m.Guest, m.Host, "parley:grieve", fill, false);

                    break;
                }
            case BotTopic.Safety:
                {
                    m.Lines.Add((BotSpeaker.Envoy, Phrase("parley:ask-peace", m.Envoy, fill)));

                    break;
                }
            case BotTopic.Friendship:
                {
                    Causes(m, BotSpeaker.Envoy, m.Guest, m.Host, "parley:ask-alliance", fill, true);

                    break;
                }
            default:
                {
                    Causes(m, BotSpeaker.Envoy, m.Guest, m.Host, "parley:ask-trade", fill, true);

                    break;
                }
        }

        if (m.HostAbsent || m.HostRep is not { Deleted: false })
        {
            m.Lines.Add((BotSpeaker.Witness, Phrase("parley:absent", m.Witness, fill)));
        }
        else if (m.Topic is BotTopic.Grievance or BotTopic.Safety)
        {
            // What the host holds against the guest, from its own book: the other half of the quarrel.
            Causes(m, BotSpeaker.Host, m.Host, m.Guest, "parley:deny", fill, false);
        }
        else
        {
            m.Lines.Add((BotSpeaker.Host, Phrase("parley:welcome", m.HostRep, fill)));
        }

        m.Lines.Add((BotSpeaker.Witness, Phrase("parley:await", m.Witness, fill)));
    }

    /// <summary>
    /// Up to two lines from one side's book: its worst causes (or its best, for a proposal), each in its own phrase; the
    /// fallback line when the book is empty.
    /// </summary>
    private static void Causes(BotHearing m, BotSpeaker who, string of, string about, string fallback, Dictionary<string, string> fill, bool best)
    {
        var book = BotGrievances.Of(of, about);
        var said = 0;

        if (best)
        {
            book.Reverse();
        }

        for (var i = 0; i < book.Count && said < 2; i++)
        {
            var (why, count, moved) = book[i];

            if (best ? moved <= 0.0 : moved >= 0.0)
            {
                continue;
            }

            var key = "parley:cause:" + why.Replace(' ', '-');

            if (!BotVoice.Has(key))
            {
                continue;
            }

            var mine = new Dictionary<string, string>(fill) { ["count"] = count.ToString(CultureInfo.InvariantCulture) };
            var line = Phrase(key, who == BotSpeaker.Host ? m.HostRep : m.Envoy, mine);

            if (line != null)
            {
                m.Lines.Add((who, line));
                said++;
            }
        }

        if (said == 0)
        {
            m.Lines.Add((who, Phrase(fallback, who == BotSpeaker.Host ? m.HostRep : m.Envoy, fill)));
        }
    }

    /// <summary>The placeholders every meeting line may use.</summary>
    public static Dictionary<string, string> Fill(BotHearing m) =>
        new()
        {
            ["guest"] = m.Guest,
            ["host"] = m.Host ?? m.Town?.Name,
            ["other"] = m.Host ?? m.Town?.Name,
            ["town"] = m.Town?.Name ?? "this town",
            ["witness"] = m.WitnessName ?? "the duke's witness",
            ["topic"] = Word(m.Topic),
            ["envoy"] = m.Envoy?.Name ?? "the envoy"
        };

    /// <summary>A line of the phrase bank, or the key's own fallback text when the bank has none.</summary>
    public static string Phrase(string key, Mobile who, Dictionary<string, string> fill) =>
        BotVoice.Phrase(key, null, who, fill) ?? key;

    // ---- the brief ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// What the duke is told, from the shard's own records, in plain lines. Bounded by what it holds: two guilds, two books,
    /// two opinions, their strength and ground, what stands between them, and what was said.
    /// </summary>
    public static string Brief(BotHearing m)
    {
        var guest = BotGuilds.Named(m.Guest);
        var host = BotGuilds.Named(m.Host);
        using var b = Server.Text.ValueStringBuilder.Create(1024);

        b.Append($"The matter: {m.Guest} has come to {m.Host} at {m.Town?.Name ?? "their seat"} about {Word(m.Topic)}. What called it: {m.Why}.\n");
        b.Append(Side(m.Guest, guest));
        b.Append(Side(m.Host, host));
        b.Append($"{m.Guest} thinks of {m.Host} {BotRegard.Of(m.Guest, m.Host):F0}; {m.Host} thinks of {m.Guest} {BotRegard.Of(m.Host, m.Guest):F0}. ");
        b.Append($"On this scale war is declared at {BotRegard.Enmity:F0} and alliances are made at {BotRegard.Alliance:F0}.\n");
        b.Append($"What {m.Guest} holds against {m.Host} since they last settled: {BotGrievances.Tell(m.Guest, m.Host)}.\n");
        b.Append($"What {m.Host} holds against {m.Guest}: {BotGrievances.Tell(m.Host, m.Guest)}.\n");
        b.Append("(Causes: \"working our land\" = the other side's people worked this side's land; \"refusing to move along\" = told to leave and stayed; ");
        b.Append("\"claiming our ground\" = laid claim to a square this side held; \"blood\" = killed this side's people; \"a shared border\" = their halls stand close; ");
        b.Append("\"trade\", \"a corpse shared\", \"help in a fight\" = good turns.)\n");

        var standing = BotPact.Any(m.Guest, m.Host) ? Standing(m.Guest, m.Host) : "none";

        b.Append($"Agreements already between them: {standing}.\n");
        b.Append($"Said before the witness — {m.Guest}'s envoy: \"{Said(m, BotSpeaker.Envoy)}\"; ");
        b.Append(m.HostAbsent || m.HostRep == null ? $"{m.Host} sent nobody to answer.\n" : $"{m.Host}'s answer: \"{Said(m, BotSpeaker.Host)}\".\n");
        b.Append("You may answer only one of these:\n");

        for (var i = 0; i < m.Allowed.Length; i++)
        {
            b.Append($"- {Means(m.Allowed[i])}\n");
        }

        return b.ToString();
    }

    /// <summary>One guild as the duke is told of it: its size, its might, its town, its land, its hall, its wars and its ally.</summary>
    private static string Side(string name, Guild guild)
    {
        var members = 0;

        if (guild?.Members != null)
        {
            for (var i = 0; i < guild.Members.Count; i++)
            {
                if (guild.Members[i] is BotMobile { Deleted: false })
                {
                    members++;
                }
            }
        }

        BotParley.Seat(guild, out var town, out _);

        string against = null;

        foreach (var war in BotWar.Standing)
        {
            against = war.Against(name) ?? against;
        }

        var hall = BotEstate.Hall(guild) is { Deleted: false } ? "has a hall" : "has no hall";
        var wars = against != null ? $", at war with {against}" : ", at peace";
        var ally = guild?.Allies is { Count: > 0 } allies && allies[0] is Guild first ? $", allied with {first.Name}" : "";

        return $"{name}: {members} members, might {BotWar.Might(name):F0}, lives by {town?.Name ?? "no town"}, holds {BotClaim.Holds(name)} squares of land, {hall}{wars}{ally}.\n";
    }

    private static string Standing(string a, string b)
    {
        List<string> said = [];

        foreach (var (x, y, kind, left) in BotPact.Standing())
        {
            if (x == a && y == b || x == b && y == a)
            {
                said.Add($"{BotPact.Word(kind)} for {left / 60000} more minutes");
            }
        }

        return said.Count == 0 ? "none" : string.Join(", ", said);
    }

    /// <summary>What one speaker said at the hearing, joined.</summary>
    public static string Said(BotHearing m, BotSpeaker who)
    {
        List<string> said = [];

        for (var i = 0; i < m.Lines.Count; i++)
        {
            if (m.Lines[i].Who == who)
            {
                said.Add(m.Lines[i].Line);
            }
        }

        return said.Count == 0 ? "nothing" : string.Join(" ", said);
    }

    // ---- the rule ------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The verdict the facts point to, when the model cannot give one. See the class: the same book, read as arithmetic.
    /// </summary>
    public static BotVerdict Rule(BotHearing m, string because)
    {
        var fill = Fill(m);
        string outcome;
        string reason;

        var blood = BotGrievances.Count(m.Guest, m.Host, "blood");
        var hall = BotGrievances.Count(m.Guest, m.Host, "claiming the ground by our hall");
        var theirBlood = BotGrievances.Count(m.Host, m.Guest, "blood");
        var trades = BotGrievances.Count(m.Guest, m.Host, "trade") + BotGrievances.Count(m.Host, m.Guest, "trade");
        var both = BotRegard.Of(m.Host, m.Guest) <= BotRegard.Enmity;

        switch (m.Topic)
        {
            case BotTopic.Grievance:
                {
                    if (m.HostAbsent)
                    {
                        outcome = War;
                        reason = $"{m.Host} would not come to answer";
                    }
                    else if (blood > 0 || hall > 0 || both)
                    {
                        outcome = War;
                        reason = blood > 0 ? $"{m.Host} has {blood} of {m.Guest}'s dead on its hands"
                            : hall > 0 ? $"{m.Host} laid claim to the ground by {m.Guest}'s hall"
                            : "the grievance runs both ways";
                    }
                    else if (trades >= TradesForTerms)
                    {
                        outcome = Trade;
                        reason = $"a quarrel of land between guilds that have traded {trades} times";
                    }
                    else
                    {
                        outcome = Peace;
                        reason = "a quarrel of land and borders, with no blood in it";
                    }

                    break;
                }
            case BotTopic.Safety:
                {
                    if (m.HostAbsent || theirBlood > 0)
                    {
                        outcome = Nothing;
                        reason = m.HostAbsent ? $"{m.Host} would not come to answer" : $"{m.Guest} has {theirBlood} of {m.Host}'s dead on its hands";
                    }
                    else
                    {
                        outcome = Peace;
                        reason = "the asker has shed no blood";
                    }

                    break;
                }
            case BotTopic.Friendship:
                {
                    outcome = Array.IndexOf(m.Allowed, Alliance) >= 0 ? Alliance : Trade;
                    reason = outcome == Alliance ? "both came, and both have room for an ally" : "one of them is already allied";

                    break;
                }
            default:
                {
                    outcome = Trade;
                    reason = "they came to trade";

                    break;
                }
        }

        if (Array.IndexOf(m.Allowed, outcome) < 0)
        {
            outcome = m.Allowed[^1];
        }

        fill["reason"] = reason;

        return new BotVerdict
        {
            Outcome = outcome,
            Speech = Phrase($"parley:rule:{outcome}", m.Witness, fill),
            Reason = $"by rule ({because}): {reason}"
        };
    }

    // ---- the word carried out -------------------------------------------------------------------------------------------

    /// <summary>
    /// Carries the verdict out, and says in <see cref="BotHearing.Ending"/> what came of it. War is asked of the war's own rules
    /// once more: they may have changed since the grievance called the meeting, and a duke's word does not overrule them.
    /// </summary>
    public static void Apply(BotHearing m)
    {
        var v = m.Verdict;
        var guest = BotGuilds.Named(m.Guest);
        var host = BotGuilds.Named(m.Host);
        var outcome = v?.Outcome ?? Nothing;
        var pact = (long)(BotPact.Hours * 3600000);

        if (guest == null || host == null)
        {
            m.Ending = "a guild was gone by the verdict";
            m.Applied = Nothing;

            return;
        }

        if (outcome == Alliance && ((guest.Allies?.Count ?? 0) >= BotRegard.MostAllies || (host.Allies?.Count ?? 0) >= BotRegard.MostAllies))
        {
            outcome = Trade;
        }

        _outcomes[outcome] = _outcomes.GetValueOrDefault(outcome) + 1;

        switch (outcome)
        {
            case War:
                {
                    if (!BotWar.MayDeclare(m.Guest, m.Host, out var refused))
                    {
                        BotParley.Forbidden++;
                        m.Ending = $"the duke's word was war and the war's rules refused it: {refused}";
                        m.Applied = Nothing;

                        return;
                    }

                    // <b>An ultimatum before the war (07.10.2026).</b> A guest twice the host's might or more demands tribute instead,
                    // and a host whose chest can pay it pays: the meeting ends in tribute, not war. See BotTribute.Demand.
                    if (BotTribute.Demand(guest, host, m.Why, out var terms))
                    {
                        m.Ending = terms;
                        m.Applied = BotTribute.Word;

                        return;
                    }

                    if (guest.IsAlly(host))
                    {
                        guest.RemoveAlly(host);
                    }

                    BotWar.Declare(guest, host, $"{m.Why}, heard before {m.WitnessName ?? "no witness"} at {m.Town?.Name ?? "their seat"}");
                    BotParley.Wars++;
                    m.Ending = "war";
                    m.Applied = War;

                    return;
                }
            case Peace:
                {
                    BotRegard.Settle(m.Guest, m.Host);
                    BotPact.Swear(m.Guest, m.Host, BotPact.Kind.Peace, pact);
                    m.Ending = $"peace for {BotPact.Hours:0.#} hours";
                    m.Applied = Peace;

                    return;
                }
            case Trade:
            case Orders:
                {
                    if (m.Topic is BotTopic.Grievance or BotTopic.Safety)
                    {
                        BotRegard.Settle(m.Guest, m.Host);
                    }

                    BotPact.Swear(m.Guest, m.Host, outcome == Trade ? BotPact.Kind.Trade : BotPact.Kind.Orders, pact);
                    m.Ending = $"{(outcome == Trade ? "trade terms" : "standing orders")} for {BotPact.Hours:0.#} hours";
                    m.Applied = outcome;

                    return;
                }
            case Alliance:
                {
                    guest.AddAlly(host);
                    BotParley.Alliances++;
                    m.Ending = "an alliance";
                    m.Applied = Alliance;

                    logger.Warning("{Guest} and {Host} are allies, sworn before {Witness} at {Town}", m.Guest, m.Host, m.WitnessName ?? "no witness", m.Town?.Name);

                    return;
                }
            default:
                {
                    m.Ending = "no agreement";
                    m.Applied = Nothing;

                    return;
                }
        }
    }

    /// <summary>Counts a verdict by who gave it.</summary>
    public static void Given(bool byModel)
    {
        if (byModel)
        {
            ByModel++;
        }
        else
        {
            ByRule++;
        }
    }

    public static string Describe()
    {
        using var say = Server.Text.ValueStringBuilder.Create(128);

        for (var i = 0; i < Outcomes.Length; i++)
        {
            say.Append(i > 0 ? ", " : "");
            say.Append($"{Outcomes[i]} {_outcomes.GetValueOrDefault(Outcomes[i])}");
        }

        return $"verdicts: {say.ToString()} ({ByModel} by the duke's model, {ByRule} by rule)";
    }

    public static void Forget()
    {
        ByModel = 0;
        ByRule = 0;
        _outcomes.Clear();
    }

    /// <summary>
    /// The lines of the meetings, written into the phrase bank for every key the file does not already carry — so a word
    /// changed in <c>bot-voice.json</c> wins, and a key it lacks still speaks.
    /// </summary>
    public static void Phrases()
    {
        Dictionary<string, string[]> bank = new(StringComparer.OrdinalIgnoreCase)
        {
            ["parley:open"] = ["I am {witness}, witness for the duke. {guest} has come to {host} about {topic}. Speak.", "{witness} hears this for the duke. {guest}, say why you have come."],
            ["parley:grieve"] = ["We have borne enough from {other}.", "{other} does us wrong, and it must stop."],
            ["parley:ask-peace"] = ["{guest} wants no war with {other}. We ask for peace.", "We have not come to fight. Let there be peace between us."],
            ["parley:ask-alliance"] = ["We have stood beside {other}. Let us stand together.", "{guest} would call {other} ally."],
            ["parley:ask-trade"] = ["We trade well with {other}. Let it be on better terms.", "{guest} would buy and sell with {other} as friends do."],
            ["parley:deny"] = ["We have done {guest} no wrong.", "{guest} complains of what it brought on itself."],
            ["parley:welcome"] = ["{host} would welcome it.", "We are glad of it."],
            ["parley:absent"] = ["{host} has sent nobody to answer.", "Nobody of {host} has come. The duke hears one side."],
            ["parley:await"] = ["The duke will weigh it.", "It is heard. The duke will give his word."],
            ["parley:cause:working-our-land"] = ["Your people work our land. {count} times now.", "You take what our land gives. {count} times."],
            ["parley:cause:refusing-to-move-along"] = ["We told your people to move along. {count} times they would not.", "Your people stand on our ground and will not leave."],
            ["parley:cause:claiming-our-ground"] = ["You laid claim to our ground.", "You set your name on land that is ours."],
            ["parley:cause:claiming-the-ground-by-our-hall"] = ["You laid claim to the very ground by our hall."],
            ["parley:cause:blood"] = ["Your people have killed {count} of ours.", "There is blood between us: {count} of ours."],
            ["parley:cause:a-shared-border"] = ["Your hall sits on our border.", "You live too close, and you press on us."],
            ["parley:cause:trade"] = ["We have traded fairly with you, {count} times.", "Our stalls and yours know each other."],
            ["parley:cause:a-corpse-shared"] = ["We have hunted side by side and shared what fell.", "Our people have shared the kill."],
            ["parley:cause:help-in-a-fight"] = ["You came to our aid when we were set upon."],
            ["parley:cause:help-given"] = ["We came to your aid when you were set upon."],
            ["parley:rule:war"] = ["The duke's word: {reason}. Let it be settled by arms.", "The duke finds no peace here: {reason}. It is war."],
            ["parley:rule:peace"] = ["The duke's word: {reason}. Keep to your own ground, and keep the peace.", "Peace, by the duke's word. Neither of you draws a blade on the other."],
            ["parley:rule:trade"] = ["The duke's word: trade with each other on fair terms, and let the quarrel go.", "Buy and sell with each other. That is the duke's word."],
            ["parley:rule:orders"] = ["The duke's word: serve each other's orders first.", "Let your makers serve each other first."],
            ["parley:rule:alliance"] = ["The duke's word: stand together as allies.", "Allies, by the duke's word."],
            ["parley:rule:nothing"] = ["The duke gives no word today: {reason}.", "Nothing is agreed. Go home."],
            ["parley:end:war"] = ["Then it is war.", "So be it. To arms."],
            ["parley:end:peace"] = ["Peace, then.", "We will keep it."],
            ["parley:end:trade"] = ["We will trade.", "Fair terms, then."],
            ["parley:end:orders"] = ["Your orders come first with us.", "It is agreed."],
            ["parley:end:alliance"] = ["Together, then.", "Ally."],
            ["parley:end:nothing"] = ["We will go home.", "Another day, then."],
            ["parley:relay"] = ["The duke's word: {speech}"],
            ["parley:relay-town"] = ["{town} says: {speech}"],
            ["parley:town-open"] = ["I speak for {town}. What does {guest} ask of us?", "{town} hears you, {guest}."],
            ["parley:town-ask"] = ["{guest} would serve {town}, and trade here on better terms.", "Set us a task, and let {town} remember who did it."],
            ["parley:town-await"] = ["Let me see how the town stands.", "Wait, and hear what the town needs."],
            ["parley:town-accept"] = ["It will be done.", "{guest} will see to it."],
            ["parley:town-done"] = ["{town} thanks {guild}: our prices are lower there now.", "The task for {town} is done."],
            ["parley:town-failed"] = ["We did not do what {town} asked. It will cost us there.", "{town} is not pleased with {guild}."],
            ["parley:town-exiled"] = ["{town} has put {guild} out. We must live elsewhere.", "We are not welcome in {town} any more."]
        };

        Dictionary<string, string[]> missing = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, lines) in bank)
        {
            if (!BotVoice.Has(key))
            {
                missing[key] = lines;
            }
        }

        if (missing.Count > 0)
        {
            BotVoice.Phrases(missing);
        }
    }
}
