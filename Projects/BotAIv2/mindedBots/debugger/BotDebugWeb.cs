using System;
using System.Collections.Generic;
using Server.BotAI.V2;

namespace Server.BotAI.Mind;

/// <summary>
/// The debugger squad on the dashboard's live window. Patrick, 02.10.2026: "windows that show the debuggers and what they
/// are thinking right now."
///
/// <para>
/// <b>Reported through <see cref="BotWebHooks.Debuggers"/>, as the minds' own line is.</b> The bot assembly may not reference
/// this one, so it asks once a second, on the game loop and only while somebody has the window open, and gets one record a
/// watcher: where its body stands, what it is doing there, the last thing it asked the model and what came back, and the last
/// lines it wrote into <c>logs/bot-debugger.log</c>.
/// </para>
///
/// <para>
/// <b>Nothing here is a new opinion.</b> The doing is read off the same states the vigil reads before it moves a watcher on —
/// a meeting it witnesses, a zone it holds for the tour, a place it heralds, the dungeons it charts, the bot it went to stand
/// beside — in that order, which is the vigil's own order of precedence. The thinking is the answer as the vigil filed it, and
/// the feed is the log's own lines, given to the watcher they name.
/// </para>
/// </summary>
public static class BotDebugWeb
{
    /// <summary>The squad, one record a watcher. Game loop only.</summary>
    public static IReadOnlyList<BotDebuggerReport> Report()
    {
        var squad = BotVigil.Squad;
        var rows = new List<BotDebuggerReport>(squad.Count);

        for (var i = 0; i < squad.Count; i++)
        {
            var w = squad[i];
            var body = w.Body is { Deleted: false } live ? live : null;
            var (doing, what, subject) = Doing(w, i, body);

            rows.Add(
                new BotDebuggerReport(
                    w.Name,
                    w.Hue,
                    w.Charge,
                    w.Organiser,
                    body?.Map?.Name,
                    body?.X ?? 0,
                    body?.Y ?? 0,
                    body is { Hidden: false },
                    doing,
                    what,
                    subject,
                    w.ThoughtAt,
                    w.ThoughtKind,
                    w.Question,
                    w.Thought,
                    w.Answer,
                    w.About,
                    w.Sure,
                    w.Asking,
                    w.Asking == null ? null : w.AskingSince,
                    w.Asked,
                    w.Findings,
                    w.Quiet,
                    w.Reflections,
                    w.Labels,
                    w.Echoes,
                    body?.Hops ?? 0,
                    w.Feed.ToArray()
                )
            );
        }

        return rows;
    }

    /// <summary>What the watcher is doing where it stands, in the vigil's own order: the first that holds it is the one that moves it.</summary>
    private static (string Doing, string What, string Subject) Doing(BotWatcher w, int rank, BotDebugger body)
    {
        if (body == null)
        {
            return ("no body", "it has no body in the world; the vigil raises one at its next beat", null);
        }

        if (BotWitness.Held(w) is { } meeting)
        {
            return ("witness", meeting, null);
        }

        // The zone keeper: on the zones and nothing else while the watch runs (07.10.2026). See BotVigil.Keeper.
        if (BotVigil.IsKeeper(w))
        {
            return ("zone keeper", BotZoneTour.StopOf(body) ?? "between zones, choosing the next by what the zones' accuracy most needs", null);
        }

        if (BotZoneTour.StopOf(body) is { } stop)
        {
            return ("zone tour", stop, null);
        }

        if (BotVenues.HeraldOf(w) is { } herald)
        {
            return ("herald", herald, null);
        }

        if (rank == 0 && !BotHalls.Charted && BotHalls.Probing != Point3D.Zero)
        {
            return ("charting", $"charting the dungeons: standing in the room being probed at {BotHalls.Probing.X},{BotHalls.Probing.Y}", null);
        }

        // Until its first move it stands where it was raised, and the vigil's opening reason belongs to no bot yet.
        var beside = w.Beside != null ? $"beside {w.Beside}: {w.Because}" : "where it was raised, not yet beside any bot";

        if (w.Organiser)
        {
            var revel = BotRevel.Current();
            var next = $"asked for the next event in {BotMarshal.NextInMinutes(Core.TickCount)} min";

            return revel != null
                ? ("marshal", $"keeping {revel}; {next}; standing {beside}", w.Beside)
                : ("marshal", $"between events, {next}; standing {beside}", w.Beside);
        }

        return ("watching", beside, w.Beside);
    }

    /// <summary>
    /// One line of the debugger's log, given to the watcher it is about: the squad name that comes first in it, else the witness
    /// of the meeting it names, else the marshal for a revel and the lead for the rest (the roll-call, the charting, the door).
    /// The bodies of blocks — the measurements under a finding — and the rules between entries are left in the file.
    /// </summary>
    public static void Heard(string line)
    {
        var squad = BotVigil.Squad;

        if (string.IsNullOrWhiteSpace(line) || squad.Count == 0 || line.StartsWith("    ", StringComparison.Ordinal))
        {
            return;
        }

        var text = line.Trim();

        if (text.Length == 0 || text[0] == '=')
        {
            return;
        }

        BotWatcher who = null;
        var first = int.MaxValue;

        for (var i = 0; i < squad.Count; i++)
        {
            var at = Word(text, squad[i].Name);

            if (at >= 0 && at < first)
            {
                first = at;
                who = squad[i];
            }
        }

        if (who == null)
        {
            // A block's heading with nobody in it ("claim:", "evidence it quoted:") belongs to the entry above it, which is
            // already in the feed; on its own it says nothing.
            if (text[^1] == ':')
            {
                return;
            }

            if (text.StartsWith("meeting #", StringComparison.Ordinal))
            {
                var end = 9;

                while (end < text.Length && char.IsDigit(text[end]))
                {
                    end++;
                }

                who = int.TryParse(text.AsSpan(9, end - 9), out var id) ? BotWitness.WitnessOf(id) : null;
            }
            else if (text.StartsWith("REVEL", StringComparison.Ordinal) || text.StartsWith("revel", StringComparison.Ordinal)
                     || text.StartsWith("no revel", StringComparison.Ordinal))
            {
                who = BotMarshal.Standing;
            }

            who ??= BotVigil.Lead;
        }

        who?.Heard(text.Length > 400 ? text[..400] + "…" : text);
    }

    /// <summary>Where the name stands as a whole word in the line, or -1.</summary>
    private static int Word(string text, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return -1;
        }

        var from = 0;

        while (true)
        {
            var at = text.IndexOf(name, from, StringComparison.Ordinal);

            if (at < 0)
            {
                return -1;
            }

            if ((at == 0 || !char.IsLetterOrDigit(text[at - 1]))
                && (at + name.Length >= text.Length || !char.IsLetterOrDigit(text[at + name.Length])))
            {
                return at;
            }

            from = at + 1;
        }
    }
}
