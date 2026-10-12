using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// One meeting: two guilds, or a guild and a town, a place, an envoy, a host, a witness, what was said and what came of it.
///
/// <para>
/// <b>A meeting is a thing with a place and a clock, not a function call.</b> Until 29.09.2026 a war was declared the instant
/// an opinion crossed a number; this object is the time between the crossing and the war — the walk to the other guild's
/// town, the wait for somebody to answer, the hearing and the word — and every stage of it is a field somebody can read on the
/// page and in the log. It is kept in memory only: a restart ends every meeting in flight, and the grievance that called it
/// calls it again at its next drift (<c>BotRegard.Drift</c>).
/// </para>
/// </summary>
public sealed class BotHearing
{
    public int Id { get; init; }

    public BotTopic Topic { get; init; }

    /// <summary>The guild that sends the envoy: the aggrieved, the asker, the proposer.</summary>
    public string Guest { get; init; }

    /// <summary>The guild on whose ground it is held, or null for a town.</summary>
    public string Host { get; init; }

    /// <summary>The town it is held in.</summary>
    public BotTowns.Town Town { get; init; }

    public Map Map { get; init; }

    /// <summary>Where: the town's main building, or the host's seat where the town has none. See <see cref="BotParley.Seat"/>.</summary>
    public Point3D Place { get; init; }

    /// <summary>What called it, in words.</summary>
    public string Why { get; init; }

    public BotHearingStage Stage { get; set; }

    public long CalledTick { get; init; }

    public long StageTick { get; set; }

    public long ArrivedTick { get; set; }

    public long HearingTick { get; set; }

    /// <summary>The guest guild's envoy, and when its errand last said it was still on it.</summary>
    public BotMobile Envoy { get; set; }

    public long EnvoyTick { get; set; }

    /// <summary>Whether the host has been asked to send somebody yet: once the envoy is near, so nobody stands for half an hour.</summary>
    public bool HostWanted { get; set; }

    /// <summary>The host guild's answerer, and when its errand last said it was still on it.</summary>
    public BotMobile HostRep { get; set; }

    public long HostTick { get; set; }

    /// <summary>Whether the host was waited for and sent nobody.</summary>
    public bool HostAbsent { get; set; }

    /// <summary>Bots that set out for this meeting and could not get there: not offered it again.</summary>
    public HashSet<Serial> Turned { get; } = [];

    /// <summary>The witness's body and name, while one stands there.</summary>
    public Mobile Witness { get; set; }

    public string WitnessName { get; set; }

    /// <summary>Whether no witness came in time and the meeting went on without one.</summary>
    public bool Unwitnessed { get; set; }

    /// <summary>What is to be said, who says it, in order; and how far the saying has got.</summary>
    public List<(BotSpeaker Who, string Line)> Lines { get; } = [];

    public int Said { get; set; }

    public long SaidTick { get; set; }

    /// <summary>The outcomes the duke may give, for this topic. See <see cref="BotDuke.Allowed"/>.</summary>
    public string[] Allowed { get; set; }

    /// <summary>The facts put in front of the duke, as they were when the hearing opened.</summary>
    public string Brief { get; set; }

    /// <summary>For a town: the tasks it may set, one of which it will.</summary>
    public List<BotTownTask> Tasks { get; set; }

    /// <summary>For a town: its state in numbers and in words, as the witness reports it.</summary>
    public BotTownReport Report { get; set; }

    /// <summary>Whether the model was asked, and whether it answered in time, late, or not at all.</summary>
    public bool Asked { get; set; }

    public bool ModelFailed { get; set; }

    public BotVerdict Verdict { get; set; }

    /// <summary>"the model", or the rule and why the rule was used.</summary>
    public string JudgedBy { get; set; }

    /// <summary>Whether a word was given and carried out, which is what makes the envoy's errand finished rather than failed.</summary>
    public bool Heard { get; set; }

    /// <summary>What came of it, in words: the outcome, or why it lapsed.</summary>
    public string Ending { get; set; }

    /// <summary>
    /// The outcome actually carried out, which is not always the one given: a war the war's rules refused is nothing, an
    /// alliance with no room left is trade. Null until the word is carried out.
    /// </summary>
    public string Applied { get; set; }

    public long OverTick { get; set; }

    /// <summary>Whether this is between two guilds rather than a guild and a town.</summary>
    public bool Between => Topic != BotTopic.Town;

    /// <summary>One clause, for Argus and the summary.</summary>
    public string Tell() =>
        $"#{Id} {Guest} {(Between ? $"to {Host}" : "to the town")} at {Town?.Name ?? "the wild"} ({Place.X},{Place.Y}) over {BotDuke.Word(Topic)}: {Stage.ToString().ToLowerInvariant()}"
        + (Envoy != null ? $", envoy {Envoy.Name}" : "")
        + (HostRep != null ? $", host {HostRep.Name}" : "")
        + (WitnessName != null ? $", witness {WitnessName}" : Unwitnessed ? ", unwitnessed" : "")
        + (Ending != null ? $" — {Ending}" : "");
}

/// <summary>What a meeting is about, which decides who walks, who waits, and what the duke may answer.</summary>
public enum BotTopic
{
    /// <summary>One guild's opinion of another has fallen to the war line: it comes to declare, and may be talked out of it.</summary>
    Grievance,

    /// <summary>A guild that another has come to think ill of asks it for peace before the grievance becomes a war.</summary>
    Safety,

    /// <summary>Two guilds that think well enough of each other to be allies come to swear it.</summary>
    Friendship,

    /// <summary>Two guilds on good terms come to agree trade terms or standing orders.</summary>
    Commerce,

    /// <summary>A guild's envoy goes to the town it lives in, hears how the town stands and is set a task by it.</summary>
    Town
}

/// <summary>Where a meeting has got to.</summary>
public enum BotHearingStage
{
    /// <summary>Called, and nobody of the guest guild has set out yet.</summary>
    Called,

    /// <summary>The envoy is on the road.</summary>
    Walking,

    /// <summary>The envoy is at the place; the witness and the host are being waited for.</summary>
    Gathered,

    /// <summary>The sides are speaking.</summary>
    Hearing,

    /// <summary>Everything is said and the duke's word is awaited.</summary>
    Awaiting,

    /// <summary>The word is being given and carried out.</summary>
    Verdict,

    /// <summary>Done with, heard or not.</summary>
    Over
}

/// <summary>What the duke — or the town — answered: one outcome out of those allowed, and the words said for it.</summary>
public sealed class BotVerdict
{
    /// <summary>For two guilds, one of <see cref="BotDuke.Outcomes"/>; for a town, the id of one task on offer.</summary>
    public string Outcome { get; init; }

    /// <summary>What the witness says aloud for it. One or two sentences.</summary>
    public string Speech { get; init; }

    /// <summary>Why, for the log. Never said aloud.</summary>
    public string Reason { get; init; }

    /// <summary>For a town: how the town stands, in the town's words, said before the task.</summary>
    public string Report { get; init; }
}

/// <summary>Who says a line of a meeting.</summary>
public enum BotSpeaker
{
    Witness,
    Envoy,
    Host
}
