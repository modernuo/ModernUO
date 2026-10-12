using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;

namespace Server.BotAI.V2;

/// <summary>
/// The pictures the live window draws its scene with, decoded from the client's own files on the listener's threads and
/// kept (02.10.2026: "the picture straight from the game").
///
/// <para>
/// <b>Never on the game loop.</b> Every route here runs on the thread that took the request; it reads the client's files
/// through <see cref="BotArtFiles"/> and the server's tiledata table, which is loaded at boot and never written again. The
/// world is not touched. A decoded picture is kept as PNG bytes in a bounded cache (the oldest asked-for goes first), and
/// the browser is told to keep it for a week: the page puts the files' fingerprint in every address, so a patched client
/// gets new addresses rather than stale pictures.
/// </para>
///
/// <para>
/// The routes: <c>/api/art/land/ID.png</c> (the 44 by 44 diamond), <c>/api/art/tex/ID.png</c> (the land's texture, for
/// slopes), <c>/api/art/static/ID.png?hue=H</c> (a static or item through its hue), <c>/api/art/tiledata</c> (heights and the
/// flags the draw order needs), <c>/api/anim/BODY/DIR.png?hue=H&amp;a=stand|war|war2|ride|die</c> (one frame of a body),
/// <c>/api/anim/item/ITEM/DIR.png?body=B&amp;hue=H&amp;a=…</c> (what a worn or ridden item looks like on that body) and
/// <c>/api/anim/BODY/meta</c>. Every picture says where it stands in <c>X-Anchor: x,y</c>.
/// </para>
/// </summary>
public static class BotWebArt
{
    /// <summary>Most pictures kept, and most bytes.</summary>
    public static int MostKept { get; set; } = 4096;

    public static long MostKeptBytes { get; set; } = 96L * 1024 * 1024;

    // ---- the counts /api/health prints ------------------------------------------------------------------------

    public static long Decoded => Interlocked.Read(ref _decoded);

    public static long Hits => Interlocked.Read(ref _hits);

    public static long Missing => Interlocked.Read(ref _missing);

    private static long _decoded;
    private static long _decodeTicks;
    private static long _worstTicks;
    private static long _hits;
    private static long _missing;
    private static long _evicted;

    // ---- the cache ----------------------------------------------------------------------------------------------

    private sealed class Kept
    {
        public string Key;
        public byte[] Png;
        public int AnchorX;
        public int AnchorY;
        public int Width;
        public int Height;
    }

    private static readonly object _lock = new();
    private static readonly Dictionary<string, LinkedListNode<Kept>> _kept = new(StringComparer.Ordinal);
    private static readonly LinkedList<Kept> _order = [];
    private static long _bytes;

    /// <summary>The fingerprint the page puts in its addresses: the files' and this code's.</summary>
    public static string Version => "1" + (BotArtFiles.Instance?.Stamp ?? "none");

    private static Kept Get(string key, Func<BotSprite> decode)
    {
        lock (_lock)
        {
            if (_kept.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _order.AddFirst(node);
                Interlocked.Increment(ref _hits);

                return node.Value;
            }
        }

        // Decoded outside the lock: two threads asking for the same new picture at once both decode it, which is cheaper
        // than making every other picture wait behind one.
        var started = Stopwatch.GetTimestamp();
        BotSprite sprite = null;

        try
        {
            sprite = decode();
        }
        catch
        {
            // A damaged entry is a missing picture, not a broken page.
        }

        var kept = new Kept { Key = key };

        if (sprite != null)
        {
            kept.Png = Png(sprite.Width, sprite.Height, sprite.Pixels);
            kept.AnchorX = sprite.AnchorX;
            kept.AnchorY = sprite.AnchorY;
            kept.Width = sprite.Width;
            kept.Height = sprite.Height;
        }
        else
        {
            Interlocked.Increment(ref _missing);
        }

        var ticks = Stopwatch.GetTimestamp() - started;

        Interlocked.Increment(ref _decoded);
        Interlocked.Add(ref _decodeTicks, ticks);

        long worst;

        while (ticks > (worst = Interlocked.Read(ref _worstTicks)) && Interlocked.CompareExchange(ref _worstTicks, ticks, worst) != worst)
        {
        }

        lock (_lock)
        {
            if (_kept.TryGetValue(key, out var raced))
            {
                return raced.Value;
            }

            var node = _order.AddFirst(kept);
            _kept[key] = node;
            _bytes += kept.Png?.Length ?? 0;

            while (_kept.Count > Math.Max(64, MostKept) || _bytes > MostKeptBytes && _order.Count > 1)
            {
                var last = _order.Last!;
                _order.RemoveLast();
                _kept.Remove(last.Value.Key);
                _bytes -= last.Value.Png?.Length ?? 0;
                _evicted++;
            }
        }

        return kept;
    }

    // ---- the routes ---------------------------------------------------------------------------------------------

    /// <summary>Answers anything under <c>/api/art/</c> and <c>/api/anim/</c>. The listener's thread; files only.</summary>
    public static void Serve(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        if (!BotWebLive.Enabled || !BotWebLive.Picture)
        {
            Text(response, 503, "{\"error\":\"the live picture is off: bot-web.json says Live or LivePicture is false\"}");

            return;
        }

        var files = BotArtFiles.Instance;

        if (files == null)
        {
            Text(response, 503, BotJson.Object(w => w.WriteString("error", "the client files could not be opened: " + (BotArtFiles.Problem ?? "?"))));

            return;
        }

        var query = request.QueryString;
        var hue = Number(query["hue"]);
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api, art|anim, ...

        if (parts.Length == 3 && parts[1] == "art" && parts[2] == "tiledata")
        {
            Text(response, 200, TileData(query["ids"]), "public, max-age=604800");

            return;
        }

        if (parts.Length == 4 && parts[1] == "art" && Id(parts[3], out var id))
        {
            switch (parts[2])
            {
                case "land":
                    Picture(response, Get($"l{id}", () => files.Land(id)));

                    return;
                case "tex":
                    Picture(response, Get($"t{id}", () => files.Texture(id)));

                    return;
                case "static":
                    {
                        var partial = id is >= 0 and < 0x10000 && Server.TileData.ItemTable[id][TileFlag.PartialHue];

                        Picture(response, Get($"s{id}:{hue}", () => files.Static(id, hue, partial)));

                        return;
                    }
            }
        }

        if (parts.Length >= 4 && parts[1] == "anim")
        {
            var pose = Pose(query["a"]);

            // /api/anim/BODY/meta: the eight directions' sizes and anchors, for a reader that cannot see headers.
            if (parts.Length == 4 && Id(parts[2], out var metaBody) && parts[3] == "meta")
            {
                Text(response, 200, Meta(files, metaBody, hue, pose), "public, max-age=604800");

                return;
            }

            // /api/anim/BODY/DIR.png
            if (parts.Length == 4 && Id(parts[2], out var body) && Id(parts[3], out var dir))
            {
                Picture(response, Get($"a{body}:{dir & 7}:{hue}:{(int)pose}", () => files.Anim(body, dir & 7, hue, pose)));

                return;
            }

            // /api/anim/item/ITEM/DIR.png?body=B: a worn or ridden item drawn as the client draws it on that body.
            if (parts.Length == 5 && parts[2] == "item" && Id(parts[3], out var item) && Id(parts[4], out var itemDir))
            {
                var wearer = Number(query["body"]);
                var anim = files.WornAnim(item, wearer, out var convHue);
                var drawHue = hue == 0 && convHue != 0 ? convHue : hue;

                if (anim <= 0)
                {
                    Picture(response, null);

                    return;
                }

                // A partial hue by tiledata is asked for with the top bit, as the client's own hue numbers do.
                if (item < 0x10000 && Server.TileData.ItemTable[item][TileFlag.PartialHue] && drawHue != 0)
                {
                    drawHue |= 0x8000;
                }

                Picture(response, Get($"w{anim}:{itemDir & 7}:{drawHue}:{(int)pose}", () => files.Anim(anim, itemDir & 7, drawHue, pose, true)));

                return;
            }
        }

        Text(response, 404, "{\"error\":\"no such picture\"}");
    }

    private static BotArtFiles.Pose Pose(string a) =>
        a switch
        {
            "war"  => BotArtFiles.Pose.War,
            "war2" => BotArtFiles.Pose.WarTwoHanded,
            "ride" => BotArtFiles.Pose.Ride,
            "die"  => BotArtFiles.Pose.Die,
            _      => BotArtFiles.Pose.Stand
        };

    private static int Number(string text) =>
        text == null ? 0
        : text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex) ? hex : 0
            : int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static bool Id(string part, out int id)
    {
        var dot = part.IndexOf('.');
        var text = dot >= 0 ? part[..dot] : part;

        // ASCII digits only, and a number that parses: "99999999999" or "1abc" is no picture, not picture 0.
        if (text.Length == 0 || !char.IsAsciiDigit(text[0]))
        {
            id = 0;

            return false;
        }

        var parsed = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(text[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out id)
            : int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out id);

        return parsed && id >= 0;
    }

    private static string Meta(BotArtFiles files, int body, int hue, BotArtFiles.Pose pose)
    {
        var buffer = new StringBuilder(256);
        buffer.Append("{\"body\":").Append(body).Append(",\"dirs\":[");

        for (var dir = 0; dir < 8; dir++)
        {
            var d = dir;
            var kept = Get($"a{body}:{d}:{hue}:{(int)pose}", () => files.Anim(body, d, hue, pose));

            if (dir > 0)
            {
                buffer.Append(',');
            }

            if (kept.Png == null)
            {
                buffer.Append("null");
            }
            else
            {
                buffer.Append('[').Append(kept.Width).Append(',').Append(kept.Height).Append(',').Append(kept.AnchorX).Append(',').Append(kept.AnchorY).Append(']');
            }
        }

        return buffer.Append("]}").ToString();
    }

    /// <summary>
    /// The server's tiledata as the page needs it for the draw order and the roofs: for each item a height and a flags byte
    /// (1 background, 2 surface, 4 foliage, 8 roof, 16 transparent, 32 wall, 64 bridge, 128 partial hue), and for each land
    /// tile whether it has a texture to stretch. Whole, as base64, or for <c>?ids=</c> as readable JSON.
    /// </summary>
    private static string TileData(string ids)
    {
        var files = BotArtFiles.Instance;

        if (!string.IsNullOrWhiteSpace(ids))
        {
            return BotJson.Object(
                w =>
                {
                    foreach (var part in ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var id = Number(part.Trim());

                        if (id is < 0 or >= 0x10000)
                        {
                            continue;
                        }

                        var data = Server.TileData.ItemTable[id];

                        w.WritePropertyName(id.ToString(CultureInfo.InvariantCulture));
                        w.WriteStartObject();
                        w.WriteString("name", data.Name ?? "");
                        w.WriteNumber("height", data.Height);
                        w.WriteNumber("flags", Flags(data));
                        w.WriteString("tileFlags", data.Flags.ToString());
                        w.WriteNumber("anim", files?.ItemAnim(id) ?? 0);
                        w.WriteEndObject();
                    }
                }
            );
        }

        var items = new byte[0x10000 * 2];

        for (var id = 0; id < 0x10000; id++)
        {
            var data = Server.TileData.ItemTable[id];
            items[id * 2] = (byte)Math.Clamp(data.Height, 0, 255);
            items[id * 2 + 1] = Flags(data);
        }

        var land = new byte[0x4000];

        for (var id = 0; id < land.Length; id++)
        {
            land[id] = (byte)(files != null && files.HasTexture(id) ? 1 : 0);
        }

        return PlainObject(
            w =>
            {
                w.WriteString("v", Version);
                w.WriteString("items", Convert.ToBase64String(items));
                w.WriteString("land", Convert.ToBase64String(land));
            }
        );
    }

    private static byte Flags(ItemData data)
    {
        var f = data.Flags;
        var b = 0;

        if ((f & TileFlag.Background) != 0)
        {
            b |= 1;
        }

        if ((f & TileFlag.Surface) != 0)
        {
            b |= 2;
        }

        if ((f & TileFlag.Foliage) != 0)
        {
            b |= 4;
        }

        if ((f & TileFlag.Roof) != 0)
        {
            b |= 8;
        }

        if ((f & TileFlag.Transparent) != 0)
        {
            b |= 16;
        }

        if ((f & TileFlag.Wall) != 0)
        {
            b |= 32;
        }

        if ((f & TileFlag.Bridge) != 0)
        {
            b |= 64;
        }

        if ((f & TileFlag.PartialHue) != 0)
        {
            b |= 128;
        }

        return (byte)b;
    }

    private static void Picture(HttpListenerResponse response, Kept kept)
    {
        if (kept?.Png == null)
        {
            Text(response, 404, "{\"error\":\"the client has no such picture\"}", "public, max-age=604800");

            return;
        }

        response.StatusCode = 200;
        response.ContentType = "image/png";
        response.Headers["Cache-Control"] = "public, max-age=604800, immutable";
        response.Headers["X-Anchor"] = $"{kept.AnchorX},{kept.AnchorY}";
        response.Headers["Access-Control-Expose-Headers"] = "X-Anchor";
        response.ContentLength64 = kept.Png.Length;
        response.OutputStream.Write(kept.Png, 0, kept.Png.Length);
        response.Close();
    }

    private static void Text(HttpListenerResponse response, int status, string text, string cache = null)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? "");

        response.StatusCode = status;
        response.ContentType = "application/json; charset=utf-8";

        if (cache != null)
        {
            response.Headers["Cache-Control"] = cache;
        }

        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    private static readonly JsonWriterOptions Relaxed = new() { Indented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// An object written without escaping '+', so base64 stays base64 rather than growing by a sixth in <c>+</c>s. For
    /// JSON read by a script, never pasted into HTML.
    /// </summary>
    internal static string PlainObject(Action<Utf8JsonWriter> write)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>(4096);

        using (var w = new Utf8JsonWriter(buffer, Relaxed))
        {
            w.WriteStartObject();
            write(w);
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    // ---- PNG --------------------------------------------------------------------------------------------------

    /// <summary>An 8-bit RGBA PNG, rows filtered by their left neighbour, which suits pictures with long runs of one colour.</summary>
    public static byte[] Png(int width, int height, uint[] argb)
    {
        using var png = new MemoryStream(width * height + 128);

        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        Buffer.BlockCopy(Big((uint)width), 0, header, 0, 4);
        Buffer.BlockCopy(Big((uint)height), 0, header, 4, 4);
        header[8] = 8;
        header[9] = 6;
        Chunk(png, "IHDR", header);

        using (var packed = new MemoryStream())
        {
            using (var zip = new ZLibStream(packed, CompressionLevel.Fastest, true))
            {
                var row = new byte[1 + width * 4];

                for (var y = 0; y < height; y++)
                {
                    row[0] = 1; // Sub
                    uint left = 0;

                    for (var x = 0; x < width; x++)
                    {
                        var c = argb[y * width + x];
                        var at = 1 + x * 4;

                        row[at] = (byte)((c >> 16) - (left >> 16));
                        row[at + 1] = (byte)((c >> 8) - (left >> 8));
                        row[at + 2] = (byte)(c - left);
                        row[at + 3] = (byte)((c >> 24) - (left >> 24));
                        left = c;
                    }

                    zip.Write(row, 0, row.Length);
                }
            }

            Chunk(png, "IDAT", packed.ToArray());
        }

        Chunk(png, "IEND", []);

        return png.ToArray();
    }

    private static readonly uint[] CrcTable = MakeCrcTable();

    private static uint[] MakeCrcTable()
    {
        var table = new uint[256];

        for (uint n = 0; n < 256; n++)
        {
            var c = n;

            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static void Chunk(Stream into, string type, byte[] data)
    {
        var typed = new byte[4 + data.Length];

        typed[0] = (byte)type[0];
        typed[1] = (byte)type[1];
        typed[2] = (byte)type[2];
        typed[3] = (byte)type[3];
        Buffer.BlockCopy(data, 0, typed, 4, data.Length);

        var c = 0xFFFFFFFF;

        for (var i = 0; i < typed.Length; i++)
        {
            c = CrcTable[(c ^ typed[i]) & 0xFF] ^ (c >> 8);
        }

        into.Write(Big((uint)data.Length));
        into.Write(typed);
        into.Write(Big(c ^ 0xFFFFFFFF));
    }

    private static byte[] Big(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    // ---- what it costs ------------------------------------------------------------------------------------------

    public static void Health(Utf8JsonWriter w)
    {
        int count;
        long bytes;
        long evicted;

        lock (_lock)
        {
            count = _kept.Count;
            bytes = _bytes;
            evicted = _evicted;
        }

        var decoded = Decoded;

        w.WritePropertyName("sprites");
        w.WriteStartObject();
        var files = BotArtFiles.Opened;

        w.WriteString("files", files?.Summary ?? BotArtFiles.Problem ?? "not opened yet: the first picture asked for opens them");
        w.WriteString("version", files == null ? null : Version);
        w.WriteNumber("decoded", decoded);
        w.WriteNumber("missing", Missing);
        w.WriteNumber("hits", Hits);
        w.WriteNumber("cached", count);
        w.WriteNumber("cachedKB", bytes / 1024);
        w.WriteNumber("evicted", evicted);
        w.WriteNumber("msEach", decoded <= 0 ? 0.0 : Math.Round(Interlocked.Read(ref _decodeTicks) * 1000.0 / Stopwatch.Frequency / decoded, 3));
        w.WriteNumber("worstMs", Math.Round(Interlocked.Read(ref _worstTicks) * 1000.0 / Stopwatch.Frequency, 2));
        w.WriteEndObject();
    }
}
