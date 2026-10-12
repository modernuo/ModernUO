using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// The debugger's ear, and the one door in the world through which a person can reach it.
///
/// <para>
/// Say <c>Hey Argus, the smith is making nothing</c> anywhere on the shard and the line is written into the
/// debugger's own log, answered on the spot, and put in front of the model on its next report — where it is
/// told that this came from the person who runs the shard and is the first thing to account for.
/// </para>
///
/// <para>
/// <b>The wake phrase is built from the debugger's name rather than written down here.</b> The name is a
/// configuration value; a trigger hard-written as "hey argus" would go on working after somebody renamed the
/// bot in <c>bot-debugger.json</c>, and then stop working the first time they typed the new name — with no
/// error anywhere, because a sentence nobody is listening for looks exactly like a sentence nobody said.
/// </para>
///
/// <para>
/// <b>Staff only, and that is not about trust.</b> The debugger is invisible to players by design; a
/// population that could talk to it would be a population that knows it exists, and every answer it gave
/// would be a message from nowhere. It also means a note in the log is always from somebody whose word is
/// worth acting on, which is what makes the notes usable as instructions rather than as data.
/// </para>
/// </summary>
public static class BotHail
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHail));

    /// <summary>What a person said to the debugger, and whether the model has been shown it yet.</summary>
    public sealed class Note
    {
        public string Who { get; init; }

        public string What { get; init; }

        public DateTime When { get; init; }

        /// <summary>Whether this has been put in front of the model at least once.</summary>
        public bool Read { get; set; }
    }

    /// <summary>
    /// How many notes are kept and recited.
    ///
    /// Every one of them stays in the log for ever; this is only how many are carried into the next prompt,
    /// and it is bounded because a prompt that grows without limit eventually pushes out the measurements
    /// the note is asking about.
    /// </summary>
    public static int MostNotes { get; set; } = 10;

    private static readonly List<Note> _notes = [];

    private static bool _listening;

    /// <summary>How many notes have been left, and how many are still waiting to be looked at.</summary>
    public static int Heard { get; private set; }

    public static int Waiting
    {
        get
        {
            var waiting = 0;

            for (var i = 0; i < _notes.Count; i++)
            {
                if (!_notes[i].Read)
                {
                    waiting++;
                }
            }

            return waiting;
        }
    }

    public static void Listen()
    {
        if (_listening)
        {
            return;
        }

        _listening = true;
        EventSink.Speech += Said;

        // Registered under the debugger's own name, so it follows a rename, and under a stable word as well
        // so that somebody who has forgotten what it is called can still find it.
        CommandSystem.Register(BotVigil.Name, AccessLevel.Administrator, Summon);

        if (!string.Equals(BotVigil.Name, "debugger", StringComparison.OrdinalIgnoreCase))
        {
            CommandSystem.Register("debugger", AccessLevel.Administrator, Summon);
        }

        // And one per helper, so [Lynceus and [Heimdall go to them the way [Argus goes to the lead.
        for (var i = 0; i < BotVigil.Helpers.Length; i++)
        {
            var helper = BotVigil.Helpers[i];

            if (!string.IsNullOrWhiteSpace(helper) && !string.Equals(helper, BotVigil.Name, StringComparison.OrdinalIgnoreCase))
            {
                CommandSystem.Register(helper.Trim(), AccessLevel.Administrator, e => Summon(e, helper.Trim()));
            }
        }

        logger.Information(
            "{Name} is listening: say \"Hey {Name}, ...\" anywhere and it goes into its log and its next report. [{Name} takes you to it, [{Name} here brings it to you",
            BotVigil.Name
        );
    }

    public static void Forget()
    {
        if (!_listening)
        {
            return;
        }

        _listening = false;
        EventSink.Speech -= Said;
    }

    /// <summary>Somebody spoke. Almost always not to the debugger, so this must be cheap to refuse.</summary>
    private static void Said(SpeechEventArgs e)
    {
        var from = e?.Mobile;
        var said = e?.Speech;

        if (from == null || from.AccessLevel <= AccessLevel.Player || string.IsNullOrWhiteSpace(said))
        {
            return;
        }

        var text = Addressed(said);

        if (text == null)
        {
            return;
        }

        Heard++;

        var note = new Note { Who = from.Name, What = text, When = DateTime.Now };

        _notes.Add(note);

        // And into the long memory, because a question outlives the process it was asked in. A note left at
        // midnight and a shard restarted at one o'clock used to mean the question had never been asked.
        BotDebugMemory.Ask(from.Name, text);

        while (_notes.Count > MostNotes * 2)
        {
            _notes.RemoveAt(0);
        }

        BotDebugLog.Rule();
        BotDebugLog.Write($"NOTE {Heard} FROM {from.Name} — this is a person speaking, not a measurement");
        BotDebugLog.Block("  what was said:", text);
        BotDebugLog.Block("  what I could answer at once:", Answer());
        BotDebugLog.Rule();

        logger.Information("{Who} left the debugger a note: {What}", from.Name, text);

        // Answered on the spot and privately. It has no voice in the world — nobody but staff can see it at
        // all, and a shout from an invisible thing is a message from nowhere.
        from.SendMessage(0x35, $"{BotVigil.Name}: noted. {Answer()}");
        from.SendMessage(0x35, $"{BotVigil.Name}: it is in my log and goes in front of me at my next report.");
    }

    /// <summary>
    /// Whether this was addressed to the debugger, and what was left once the greeting is taken off.
    ///
    /// Two forms, both natural: <c>Hey Argus, ...</c> and <c>Argus, ...</c>. Anything else is somebody
    /// talking to somebody else.
    /// </summary>
    private static string Addressed(string said)
    {
        var name = BotVigil.Name;
        var trimmed = said.Trim();

        if (Starts(trimmed, $"hey {name}", out var rest) || Starts(trimmed, name, out rest))
        {
            return rest;
        }

        return null;
    }

    private static bool Starts(string said, string opening, out string rest)
    {
        rest = null;

        if (said.Length < opening.Length || !said.StartsWith(opening, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // The next character has to end the word, or "Argusson" would be talking to it.
        if (said.Length > opening.Length && char.IsLetterOrDigit(said[opening.Length]))
        {
            return false;
        }

        rest = said[opening.Length..].TrimStart(' ', ',', ':', '-', '—').Trim();

        return rest.Length > 0;
    }

    /// <summary>
    /// What the debugger can say back without asking anything of a model: where it is, what the last
    /// roll-call found, and when it will next think.
    ///
    /// <para>
    /// It is deliberately not "noted, I will look into it". A person who has just told the watcher something
    /// wants to know whether the watcher is awake and what it already believes, and both are known here
    /// without a single token being spent.
    /// </para>
    /// </summary>
    private static string Answer()
    {
        var body = BotVigil.Body;
        var sb = ValueStringBuilder.Create(256);

        try
        {
            sb.Append(
                body is { Deleted: false }
                    ? $"I am at {body.Location.X},{body.Location.Y} in {body.Region?.Name ?? "nowhere"}"
                    : "I have no body this moment"
            );

            // BotVigil.Describe already ends with the roll-call's own line; saying it here as well printed
            // the same sentence twice in the one answer a person actually reads.
            sb.Append(". ");
            sb.Append(BotVigil.Describe());

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>
    /// The notes as the model is shown them, newest last, with the unread ones marked.
    ///
    /// <para>
    /// Read notes are still recited. A person who asked yesterday whether the smith was making anything is
    /// still owed an answer today, and a note that vanishes the moment it has been seen once turns a standing
    /// question into a single missed opportunity.
    /// </para>
    /// </summary>
    public static string Recite()
    {
        // Anything still unanswered from before this session comes first: it has been waiting longest and is
        // the likeliest thing to be forgotten by a watcher whose memory used to end at the restart.
        var standing = Standing();

        if (_notes.Count == 0)
        {
            return standing;
        }

        var sb = ValueStringBuilder.Create(512);

        try
        {
            if (standing != null)
            {
                sb.Append(standing);
            }

            var from = Math.Max(0, _notes.Count - MostNotes);

            for (var i = from; i < _notes.Count; i++)
            {
                var note = _notes[i];

                sb.Append("- ");
                sb.Append(note.When.ToString("HH:mm"));
                sb.Append(", ");
                sb.Append(note.Who);
                sb.Append(note.Read ? " (you have seen this before): " : " (NEW SINCE YOUR LAST REPORT): ");
                sb.AppendLine(note.What);

                note.Read = true;
            }

            BotDebugMemory.Answered();

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>Questions left in earlier sessions that were never put in front of the model.</summary>
    private static string Standing()
    {
        var notes = BotDebugMemory.Notes;
        var sb = ValueStringBuilder.Create(256);

        try
        {
            var said = 0;

            for (var i = 0; i < notes.Count; i++)
            {
                if (notes[i].Answered)
                {
                    continue;
                }

                sb.Append("- ");
                sb.Append(notes[i].When);
                sb.Append(", ");
                sb.Append(notes[i].Who);
                sb.Append(" (ASKED IN AN EARLIER SESSION AND STILL UNANSWERED): ");
                sb.AppendLine(notes[i].What);

                said++;
            }

            return said == 0 ? null : sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>Everything back to nothing, for a world reload.</summary>
    public static void Reset()
    {
        _notes.Clear();
        Heard = 0;
    }

    [Usage("argus [here]")]
    [Description("Goes to the debugger, or with \"here\" brings the debugger to you.")]
    private static void Summon(CommandEventArgs e) => Summon(e, BotVigil.Name);

    private static void Summon(CommandEventArgs e, string name)
    {
        var from = e?.Mobile;
        var watcher = BotVigil.Called(name);
        var body = watcher?.Body;

        if (from == null)
        {
            return;
        }

        if (body is not { Deleted: false } || body.Map == null || body.Map == Map.Internal)
        {
            from.SendMessage(0x22, $"{name} has no body at the moment, so there is nowhere to go.");

            return;
        }

        // "here" brings it to you; anything else takes you to it. Two directions, because half the time the
        // thing worth looking at is where you are and half the time it is where the debugger went.
        if (e.Length > 0 && e.GetString(0).InsensitiveEquals("here"))
        {
            body.Hover(from.Map, from.Location);

            from.SendMessage(0x35, $"{name} is beside you. {Answer()}");

            BotDebugLog.Write($"{from.Name} called {name} over to {from.Location.X},{from.Location.Y} in {from.Region?.Name ?? "nowhere"}");

            return;
        }

        from.MoveToWorld(body.Location, body.Map);

        from.SendMessage(0x35, $"{name} is here. {Answer()}");

        BotDebugLog.Write($"{from.Name} came to look over {name}'s shoulder at {body.Location.X},{body.Location.Y}");
    }
}
