using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A picture of the map for the page to draw the bots on, made from the client's own radar colours.
///
/// <para>
/// <b>Drawn in slices on the game loop, encoded off it.</b> The tile matrix is not safe to read from
/// another thread, so the sampling runs here a few rows at a time; the PNG encoding is pure arithmetic over
/// a byte array and goes to the thread pool. Once the file is on disk it is not drawn again until the map
/// files change, because a shard restarts often and the picture does not.
/// </para>
///
/// <para>
/// <b>Statics over land, top one wins.</b> That is what the client's radar does: a road, a house or a tree
/// shows in its own colour, and bare ground in the land tile's. The colours come from <c>radarcol.mul</c>,
/// sixteen bits each, in the client folder the server already knows.
/// </para>
/// </summary>
public static class BotWebMap
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWebMap));

    public static bool Enabled { get; set; } = true;

    /// <summary>Tiles per pixel.</summary>
    public static int Scale { get; set; } = 4;

    /// <summary>Rows of pixels sampled per slice of the loop.</summary>
    public static int RowsPerSlice { get; set; } = 12;

    public static bool Ready { get; private set; }

    public static bool Failed { get; private set; }

    public static string Path { get; private set; }

    public static int Width { get; private set; }

    public static int Height { get; private set; }

    public static double SliceMs { get; private set; }

    private static Map _map;

    private static ushort[] _colours;

    private static byte[] _rgb;

    private static int _row;

    private static bool _encoding;

    private static ushort[] _radar;

    private static volatile bool _radarTried;

    private static readonly object _radarLock = new();

    /// <summary>
    /// The client's radar colours, read once and kept, for the live window's terrain (02.10.2026). The picture above lets go
    /// of its copy once it is written; this one is small (two bytes a tile kind) and stays. Null without the client files.
    /// A file read, never the world, so any thread may ask; the live window asks from the listener the first time a bot is
    /// watched, so the loop never waits on the disk.
    /// </summary>
    public static ushort[] Radar()
    {
        if (_radarTried)
        {
            return _radar;
        }

        lock (_radarLock)
        {
            if (_radarTried)
            {
                return _radar;
            }

            try
            {
                var file = Core.FindDataFile("radarcol.mul", false);

                if (file != null && File.Exists(file))
                {
                    var bytes = File.ReadAllBytes(file);
                    var colours = new ushort[bytes.Length / 2];

                    for (var i = 0; i < colours.Length; i++)
                    {
                        colours[i] = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                    }

                    _radar = colours;
                }
            }
            catch (Exception e)
            {
                logger.Warning("The radar colours could not be read for the live window: {Message}", e.Message);
            }

            _radarTried = true;

            return _radar;
        }
    }

    /// <summary>
    /// The radar colour of one tile: the topmost static's, else the land tile's, as <see cref="Slice"/> draws the picture.
    /// Game loop only — the tile matrix is not safe to read from another thread.
    /// </summary>
    public static ushort Colour(TileMatrix tiles, ushort[] colours, int x, int y)
    {
        var index = tiles.GetLandTile(x, y).ID & 0x3FFF;
        var top = int.MinValue;

        foreach (var tile in tiles.GetStaticTiles(x, y))
        {
            var height = tile.Z + tile.Height;

            if (height >= top)
            {
                top = height;
                index = 0x4000 + (tile.ID & 0x3FFF);
            }
        }

        return index >= 0 && index < colours.Length ? colours[index] : (ushort)0;
    }

    /// <summary>Points the drawing at a map and a file. Nothing is read until <see cref="Slice"/> is called.</summary>
    public static void Prepare(Map map, string folder)
    {
        if (!Enabled || map == null || map == Map.Internal)
        {
            Failed = !Enabled;

            return;
        }

        _map = map;
        Scale = Math.Max(1, Scale);
        Width = map.Width / Scale;
        Height = map.Height / Scale;
        Path = System.IO.Path.Combine(folder, $"{map.Name.ToLowerInvariant()}.png");

        try
        {
            if (File.Exists(Path))
            {
                Ready = true;

                logger.Information("The map picture is already at {Path}; it is not drawn again", Path);

                return;
            }

            var radar = Core.FindDataFile("radarcol.mul", false);

            if (radar == null || !File.Exists(radar))
            {
                Failed = true;

                logger.Warning("No radarcol.mul in the client folders; the page will draw the bots on a plain grid");

                return;
            }

            var bytes = File.ReadAllBytes(radar);

            _colours = new ushort[bytes.Length / 2];

            for (var i = 0; i < _colours.Length; i++)
            {
                _colours[i] = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            }

            _rgb = new byte[Width * Height * 3];
            _row = 0;

            logger.Information(
                "Drawing a {Width}x{Height} picture of {Map} from {Colours} radar colours, {Rows} rows a slice",
                Width,
                Height,
                map.Name,
                _colours.Length,
                RowsPerSlice
            );
        }
        catch (Exception e)
        {
            Failed = true;

            logger.Warning("The map picture could not be prepared: {Message}", e.Message);
        }
    }

    /// <summary>One slice of sampling on the game loop. Returns true while there is more to do.</summary>
    public static bool Slice()
    {
        if (Ready || Failed || _map == null || _rgb == null || _encoding)
        {
            return false;
        }

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var tiles = _map.Tiles;
        var last = Math.Min(Height, _row + RowsPerSlice);

        try
        {
            for (; _row < last; _row++)
            {
                var y = _row * Scale + Scale / 2;

                for (var px = 0; px < Width; px++)
                {
                    var x = px * Scale + Scale / 2;
                    var index = tiles.GetLandTile(x, y).ID & 0x3FFF;
                    var top = int.MinValue;

                    foreach (var tile in tiles.GetStaticTiles(x, y))
                    {
                        var height = tile.Z + tile.Height;

                        if (height >= top)
                        {
                            top = height;
                            index = 0x4000 + (tile.ID & 0x3FFF);
                        }
                    }

                    var colour = index >= 0 && index < _colours.Length ? _colours[index] : (ushort)0;
                    var at = (_row * Width + px) * 3;

                    _rgb[at] = (byte)(((colour >> 10) & 0x1F) << 3);
                    _rgb[at + 1] = (byte)(((colour >> 5) & 0x1F) << 3);
                    _rgb[at + 2] = (byte)((colour & 0x1F) << 3);
                }
            }
        }
        catch (Exception e)
        {
            Failed = true;

            logger.Warning("The map picture failed at row {Row}: {Message}", _row, e.Message);

            return false;
        }

        SliceMs += (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        if (_row < Height)
        {
            return true;
        }

        _encoding = true;

        var rgb = _rgb;
        var columns = Width;
        var rows = Height;
        var path = Path;

        _ = Task.Run(
            () =>
            {
                try
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                    File.WriteAllBytes(path, Png(columns, rows, rgb));

                    Core.LoopContext.Post(
                        _ =>
                        {
                            Ready = true;
                            _rgb = null;
                            _colours = null;

                            logger.Information("The map picture is written to {Path} ({Ms:F0}ms of the loop to sample)", path, SliceMs);
                        },
                        null
                    );
                }
                catch (Exception e)
                {
                    Core.LoopContext.Post(
                        _ =>
                        {
                            Failed = true;

                            logger.Warning("The map picture could not be written: {Message}", e.Message);
                        },
                        null
                    );
                }
            }
        );

        return false;
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

    private static uint Crc(byte[] bytes, int offset, int count)
    {
        var c = 0xFFFFFFFF;

        for (var i = offset; i < offset + count; i++)
        {
            c = CrcTable[(c ^ bytes[i]) & 0xFF] ^ (c >> 8);
        }

        return c ^ 0xFFFFFFFF;
    }

    private static void Chunk(Stream into, string type, byte[] data)
    {
        var typed = new byte[4 + data.Length];

        typed[0] = (byte)type[0];
        typed[1] = (byte)type[1];
        typed[2] = (byte)type[2];
        typed[3] = (byte)type[3];
        Buffer.BlockCopy(data, 0, typed, 4, data.Length);

        into.Write(Big((uint)data.Length));
        into.Write(typed);
        into.Write(Big(Crc(typed, 0, typed.Length)));
    }

    private static byte[] Big(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    /// <summary>An 8-bit RGB PNG with no filtering, which is all a map needs.</summary>
    public static byte[] Png(int width, int height, byte[] rgb)
    {
        using var png = new MemoryStream();

        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];

        Buffer.BlockCopy(Big((uint)width), 0, header, 0, 4);
        Buffer.BlockCopy(Big((uint)height), 0, header, 4, 4);
        header[8] = 8;
        header[9] = 2;

        Chunk(png, "IHDR", header);

        using (var packed = new MemoryStream())
        {
            using (var zip = new ZLibStream(packed, CompressionLevel.Optimal, true))
            {
                var stride = width * 3;

                for (var y = 0; y < height; y++)
                {
                    zip.WriteByte(0);
                    zip.Write(rgb, y * stride, stride);
                }
            }

            Chunk(png, "IDAT", packed.ToArray());
        }

        Chunk(png, "IEND", []);

        return png.ToArray();
    }
}
