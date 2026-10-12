using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;

namespace Server.BotAI.V2;

/// <summary>
/// The guilds' dealings on the page: what each pair of guilds thinks of the other and what stands between them, each guild's
/// standing in each town it deals with, and the meetings standing. Written into the snapshot by one line in
/// <c>BotWebSnapshot.Build</c>; every figure is one the log's <c>Diplomacy:</c> line already prints.
/// </summary>
public static class BotDiplomacyWeb
{
    public static void Write(Utf8JsonWriter w)
    {
        w.WritePropertyName("diplomacy");
        w.WriteStartObject();

        Relations(w);
        Standing(w);
        Meetings(w);

        w.WritePropertyName("recent");
        w.WriteStartArray();

        var recent = BotParley.Recent;

        for (var i = recent.Count - 1; i >= 0; i--)
        {
            w.WriteStringValue(recent[i]);
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    /// <summary>Every pair of standing guilds that has an opinion, an agreement, an alliance or a war between them.</summary>
    private static void Relations(Utf8JsonWriter w)
    {
        w.WritePropertyName("relations");
        w.WriteStartArray();

        List<Guild> guilds = [];

        foreach (var guild in BotGuilds.Standing)
        {
            if (guild is { Disbanded: false })
            {
                guilds.Add(guild);
            }
        }

        for (var i = 0; i < guilds.Count; i++)
        {
            for (var j = i + 1; j < guilds.Count; j++)
            {
                var a = guilds[i];
                var b = guilds[j];
                var ab = BotRegard.Of(a.Name, b.Name);
                var ba = BotRegard.Of(b.Name, a.Name);
                var allied = a.IsAlly(b);
                var war = a.IsWar(b);
                var any = BotPact.Any(a.Name, b.Name);

                if (ab == 0.0 && ba == 0.0 && !allied && !war && !any)
                {
                    continue;
                }

                w.WriteStartObject();
                w.WriteString("a", a.Name);
                w.WriteString("b", b.Name);
                w.WriteNumber("ab", System.Math.Round(ab, 1));
                w.WriteNumber("ba", System.Math.Round(ba, 1));
                w.WriteBoolean("allied", allied);
                w.WriteBoolean("war", war);
                w.WritePropertyName("pacts");
                w.WriteStartArray();

                foreach (var (x, y, kind, left) in BotPact.Standing())
                {
                    if (x == a.Name && y == b.Name || x == b.Name && y == a.Name)
                    {
                        w.WriteStartObject();
                        w.WriteString("kind", BotPact.Word(kind));
                        w.WriteNumber("minutesLeft", left / 60000);
                        w.WriteEndObject();
                    }
                }

                w.WriteEndArray();
                w.WriteString("aHolds", BotGrievances.Tell(a.Name, b.Name));
                w.WriteString("bHolds", BotGrievances.Tell(b.Name, a.Name));
                w.WriteEndObject();
            }
        }

        w.WriteEndArray();
    }

    /// <summary>Each guild's standing in each town it has dealt with: prices, failures, exile, and the task in hand.</summary>
    private static void Standing(Utf8JsonWriter w)
    {
        w.WritePropertyName("standing");
        w.WriteStartArray();

        foreach (var st in BotBurgh.All)
        {
            if (st.Task == null && st.Done + st.Failed == 0 && !st.Exiled)
            {
                continue;
            }

            w.WriteStartObject();
            w.WriteString("guild", st.Guild);
            w.WriteString("town", st.Town);
            w.WriteNumber("prices", System.Math.Round(st.Factor, 3));
            w.WriteNumber("done", st.Done);
            w.WriteNumber("failed", st.Failed);
            w.WriteNumber("failsInARow", st.Fails);
            w.WriteBoolean("exiled", st.Exiled);

            if (st.Exiled && st.Out is { } until)
            {
                w.WriteString("exiledUntil", until.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }

            if (st.Task is { } task)
            {
                w.WritePropertyName("task");
                w.WriteStartObject();
                w.WriteString("kind", task.Kind.ToString().ToLowerInvariant());
                w.WriteString("offer", task.Offer);
                w.WriteString("progress", task.Progress);
                BotJson.StringOrNull(w, "said", task.Charge);
                w.WriteNumber("minutesLeft", (task.Deadline - Core.TickCount) / 60000);
                w.WriteNumber("x", task.Target.X);
                w.WriteNumber("y", task.Target.Y);
                w.WriteEndObject();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void Meetings(Utf8JsonWriter w)
    {
        w.WritePropertyName("meetings");
        w.WriteStartArray();

        var meetings = BotParley.Meetings;

        for (var i = 0; i < meetings.Count; i++)
        {
            var m = meetings[i];

            w.WriteStartObject();
            w.WriteNumber("id", m.Id);
            w.WriteString("topic", m.Topic.ToString().ToLowerInvariant());
            w.WriteString("guest", m.Guest);
            BotJson.StringOrNull(w, "host", m.Host);
            BotJson.StringOrNull(w, "town", m.Town?.Name);
            w.WriteNumber("x", m.Place.X);
            w.WriteNumber("y", m.Place.Y);
            w.WriteString("stage", m.Stage.ToString().ToLowerInvariant());
            BotJson.StringOrNull(w, "envoy", m.Envoy?.Name);
            BotJson.StringOrNull(w, "answerer", m.HostRep?.Name);
            BotJson.StringOrNull(w, "witness", m.WitnessName);
            BotJson.StringOrNull(w, "why", m.Why);
            w.WriteNumber("minutes", (Core.TickCount - m.CalledTick) / 60000);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }
}
