using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The reach map on the dashboard: a summary the loop writes, and the tiles themselves, cut out of the immutable snapshot on
/// the listener's own threads.
///
/// <para>
/// <b>Classes, not colours (01.10.2026).</b> The page is sent one byte a tile — or one a square of tiles for the whole map —
/// and paints them in its own theme's colours, so a light page and a dark one agree and a tile under the pointer can be named
/// exactly. Sent deflated: the open sea and the open fields are long runs of one byte, and the whole map at four tiles a pixel
/// goes out in a few hundred kilobytes.
/// </para>
/// </summary>
public static class BotReachWeb
{
    /// <summary>The most tiles one picture may hold, so a request cannot ask for the whole map a tile a pixel.</summary>
    public static int MostCells { get; set; } = 2 * 1024 * 1024;

    private static volatile string _json = "{\"ready\":false,\"state\":\"starting\"}";

    /// <summary>The summary as the loop last wrote it.</summary>
    public static string Json => _json;

    private sealed record Picture(int Version, string Key, byte[] Bytes, int Columns, int Rows);

    private static volatile Picture _last;

    /// <summary>Writes the summary. On the loop, every couple of seconds (<see cref="BotReachMap"/>'s tick).</summary>
    public static void Build()
    {
        var snap = BotReachMap.Snapshot;
        var check = BotReachCheck.Report;
        using var j = ValueStringBuilder.Create(2048);

        j.Append($"{{\"ready\":{(snap != null ? "true" : "false")},\"state\":\"{Esc(BotReachMap.State)}\",\"progress\":{BotReachMap.Progress:F3}");
        j.Append($",\"clusters\":{BotReachMap.Clusters},\"drawn\":{BotReachMap.ClustersDrawn}");

        if (snap != null)
        {
            j.Append($",\"map\":\"{Esc(snap.MapName)}\",\"width\":{snap.Width},\"height\":{snap.Height},\"version\":{snap.Version},\"at\":\"{snap.At:O}\"");
            j.Append($",\"tiles\":{{\"unknown\":{snap.Tiles[0]},\"open\":{snap.Tiles[1]},\"isle\":{snap.Tiles[2]},\"pocket\":{snap.Tiles[3]},\"water\":{snap.Tiles[4]},\"blocked\":{snap.Tiles[5]}}}");
            j.Append($",\"lands\":{snap.Lands},\"regions\":{snap.Regions},\"gates\":{snap.Gates},\"home\":[{snap.Home.X},{snap.Home.Y}],\"homed\":{(snap.Homed ? "true" : "false")}");
            j.Append(",\"largest\":[");

            for (var i = 0; i < snap.Largest.Length; i++)
            {
                var land = snap.Largest[i];

                j.Append(i > 0 ? "," : "");
                j.Append($"{{\"x\":{land.X},\"y\":{land.Y},\"tiles\":{land.Tiles},\"shore\":{(land.Coastal ? "true" : "false")}}}");
            }

            j.Append("]");
            j.Append($",\"borders\":{snap.OwnArrays}");
        }

        j.Append($",\"cost\":{{\"drawMs\":{BotReachMap.DrawMs:F0},\"signMs\":{BotReachMap.SignMs:F0},\"handMs\":{BotReachMap.HandMs:F1},\"worstTickMs\":{BotReachMap.WorstTickMs:F2}");
        j.Append($",\"drawn\":{BotReachMap.Drawn},\"redrawn\":{BotReachMap.Redrawn},\"signed\":{BotReachMap.Signed},\"signChanged\":{BotReachMap.SignChanged}");
        j.Append($",\"solves\":{BotReachMap.Solves},\"solveMs\":{BotReachMap.LastSolveMs:F0},\"solveInline\":{(BotReachMap.LastSolveInline ? "true" : "false")}");
        j.Append($",\"file\":\"{Esc(BotReachMap.FileState)}\",\"fileBytes\":{BotReachMap.FileBytes}}}");
        j.Append($",\"refused\":{{\"asked\":{BotReachMap.Asked},\"walks\":{BotReachMap.Refused},\"aboard\":{BotReachMap.RefusedAboard},\"samplers\":{BotReachMap.SamplerRefused},\"planner\":{BotReachMap.PlannerRefused}}}");
        j.Append($",\"steps\":{{\"seen\":{BotReachMap.StepsSeen},\"contradicted\":{BotReachMap.Contradicted},\"away\":{BotReachMap.StepsAway},\"redrawn\":{BotReachMap.RedrawnForSteps}}}");

        var agree = 0;
        var disagree = 0;
        var unsure = 0;

        for (var k = 0; k < 6; k++)
        {
            agree += check.Agree[k];
            disagree += check.Disagree[k];
            unsure += check.Unsure[k];
        }

        j.Append($",\"check\":{{\"version\":{check.Version},\"state\":\"{Esc(check.State)}\",\"asked\":{check.Asked},\"done\":{check.Done},\"agree\":{agree},\"disagree\":{disagree},\"unsure\":{unsure},\"stood\":{check.Stood},\"ms\":{check.Ms:F0}");
        j.Append(",\"byClass\":{");

        var first = true;

        for (var k = 1; k < 6; k++)
        {
            if (check.Agree[k] + check.Disagree[k] + check.Unsure[k] == 0)
            {
                continue;
            }

            j.Append(first ? "" : ",");
            j.Append($"\"{Name((BotReachClass)k)}\":[{check.Agree[k]},{check.Disagree[k]},{check.Unsure[k]}]");
            first = false;
        }

        j.Append("},\"misses\":[");

        for (var i = 0; i < check.Misses.Length && i < 20; i++)
        {
            j.Append(i > 0 ? "," : "");
            j.Append($"\"{Esc(check.Misses[i])}\"");
        }

        j.Append("]}}");

        _json = j.ToString();
    }

    public static string Name(BotReachClass k) =>
        k switch
        {
            BotReachClass.Open => "open",
            BotReachClass.Isle => "isle",
            BotReachClass.Pocket => "pocket",
            BotReachClass.Water => "water",
            BotReachClass.Blocked => "blocked",
            _ => "unknown"
        };

    private static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "";
        }

        using var b = ValueStringBuilder.CreateMT(s.Length + 8);

        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];

            switch (ch)
            {
                case '"':
                    {
                        b.Append("\\\"");

                        break;
                    }
                case '\\':
                    {
                        b.Append("\\\\");

                        break;
                    }
                case < ' ':
                    {
                        b.Append(' ');

                        break;
                    }
                default:
                    {
                        b.Append(ch);

                        break;
                    }
            }
        }

        return b.ToString();
    }

    /// <summary>
    /// <c>/api/reach/tiles?x=&amp;y=&amp;w=&amp;h=&amp;s=</c>: the classes of a rectangle, one byte a tile or a square of
    /// <c>s</c> tiles (1, 2, 4, 8 or 16), deflated, with the picture's size and the map's version in the headers. Off the loop,
    /// from the snapshot only.
    /// </summary>
    public static void Tiles(HttpListenerRequest request, HttpListenerResponse response)
    {
        var snap = BotReachMap.Snapshot;

        if (snap == null)
        {
            response.StatusCode = 503;
            response.Close();

            return;
        }

        var q = request.QueryString;
        var scale = Math.Clamp(Int(q["s"], 4), 1, 16);

        scale = scale >= 16 ? 16 : scale >= 8 ? 8 : scale >= 4 ? 4 : scale >= 2 ? 2 : 1;

        var x0 = Math.Clamp(Int(q["x"], 0), 0, snap.Width - 1) / scale * scale;
        var y0 = Math.Clamp(Int(q["y"], 0), 0, snap.Height - 1) / scale * scale;
        var w = Math.Clamp(Int(q["w"], snap.Width), 1, snap.Width - x0);
        var h = Math.Clamp(Int(q["h"], snap.Height), 1, snap.Height - y0);
        var columns = (w + scale - 1) / scale;
        var rows = (h + scale - 1) / scale;

        if ((long)columns * rows > MostCells)
        {
            response.StatusCode = 413;
            response.Close();

            return;
        }

        var key = $"{x0},{y0},{columns},{rows},{scale}";
        var cached = _last;
        byte[] bytes;

        if (cached != null && cached.Version == snap.Version && cached.Key == key)
        {
            bytes = cached.Bytes;
        }
        else
        {
            var raw = snap.Picture(x0, y0, columns, rows, scale);

            using (var packed = new MemoryStream(raw.Length / 8 + 64))
            {
                using (var zip = new GZipStream(packed, CompressionLevel.Fastest, true))
                {
                    zip.Write(raw, 0, raw.Length);
                }

                bytes = packed.ToArray();
            }

            _last = new Picture(snap.Version, key, bytes, columns, rows);
        }

        response.ContentType = "application/octet-stream";
        response.Headers["Content-Encoding"] = "gzip";
        response.Headers["X-Reach-Version"] = snap.Version.ToString();
        response.Headers["X-Reach-X"] = x0.ToString();
        response.Headers["X-Reach-Y"] = y0.ToString();
        response.Headers["X-Reach-Columns"] = columns.ToString();
        response.Headers["X-Reach-Rows"] = rows.ToString();
        response.Headers["X-Reach-Scale"] = scale.ToString();
        response.Headers["Access-Control-Expose-Headers"] = "X-Reach-Version, X-Reach-X, X-Reach-Y, X-Reach-Columns, X-Reach-Rows, X-Reach-Scale";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    /// <summary><c>/api/reach/tile?x=&amp;y=</c>: one tile, in words and numbers. Off the loop, from the snapshot only.</summary>
    public static string Tile(HttpListenerRequest request)
    {
        var snap = BotReachMap.Snapshot;

        if (snap == null)
        {
            return "{\"ready\":false}";
        }

        var x = Int(request.QueryString["x"], -1);
        var y = Int(request.QueryString["y"], -1);

        if (!snap.Inside(x, y))
        {
            return "{\"ready\":true,\"error\":\"off the map\"}";
        }

        var k = snap.ClassAt(x, y);
        var land = snap.LandAt(x, y, out var tiles, out _);
        using var j = ValueStringBuilder.CreateMT(512);

        j.Append($"{{\"ready\":true,\"x\":{x},\"y\":{y},\"class\":\"{Name(k)}\",\"label\":\"{Esc(BotReachMap.Label(k))}\",\"version\":{snap.Version}");
        j.Append($",\"walker\":\"{(k == BotReachClass.Open ? "reachable" : "unreachable")}\",\"ship\":\"{(k == BotReachClass.Water ? "sailable" : "land")}\"");

        if (land >= 0 && k != BotReachClass.Open)
        {
            j.Append($",\"land\":{{\"id\":{land},\"tiles\":{tiles}}}");
        }

        j.Append(",\"layers\":[");

        var c = (y >> 4) * snap.ClustersX + (x >> 4);
        var cell = ((y & 15) << 4) | (x & 15);
        var any = false;

        if (snap.LayerClasses[c] is { } layers)
        {
            for (var i = 0; i < layers.Length; i++)
            {
                if ((layers[i] & 0xFF) != cell)
                {
                    continue;
                }

                j.Append(any ? "," : "");
                j.Append($"{{\"z\":{((layers[i] >> 8) & 0xFF) - 128},\"class\":\"{Name((BotReachClass)((layers[i] >> 16) & 0xFF))}\"}}");
                any = true;
            }
        }

        j.Append("]}");

        return j.ToString();
    }

    private static int Int(string s, int fallback) => int.TryParse(s, out var v) ? v : fallback;
}
