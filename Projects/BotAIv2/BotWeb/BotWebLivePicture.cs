using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The live window's picture (Patrick, 02.10.2026: "can the picture be shown straight from the game?"): what stands where
/// round a watched bot, in the client's own terms, for the page to draw with the client's own pictures.
///
/// <para>
/// <b>Two parts, sent at two speeds.</b> The ground and what is built on it — the land tile and its height, every static and
/// every piece of a house or a boat — changes only when the bot walks off the square it was cut for or a multi comes, goes
/// or moves; it is cut on the loop then, kept as one string, and the page fetches it from <c>/api/live/scene</c> when the
/// frame's <c>pic.scene</c> number moves on. What moves — the mobiles with their bodies, hues, facing, war mode, mount and
/// worn items, and the items on the ground — rides in every frame, once a second.
/// </para>
///
/// <para>
/// <b>Compact.</b> The land is three bytes a tile (the id and the height) and a static is seven (where in the square, the
/// id, the height, the hue), both base64: about 12 kB for open country and 40 kB for a town, a few times a minute for a
/// walking bot. A mobile is a few numbers, its worn items one flat array.
/// </para>
///
/// <para>
/// <b>Cheap on the loop, and bounded.</b> The map blocks are the tile matrix's own, read once and kept by the matrix; at
/// most <see cref="SceneBlockBudget"/> blocks are read for the first time in one beat across all watched bots, and a square
/// that could not be finished is finished in the next beats. The pictures themselves are never decoded here: see
/// <see cref="BotWebArt"/>.
/// </para>
/// </summary>
public static partial class BotWebLive
{
    /// <summary>The square the picture covers round the bot, in tiles each way.</summary>
    public static int PictureRadius { get; set; } = 18;

    /// <summary>How far the bot may walk before the ground is cut again round where it stands.</summary>
    public static int PictureSlack { get; set; } = 6;

    /// <summary>Most map blocks read for the first time in one beat, across every watched bot.</summary>
    public static int SceneBlockBudget { get; set; } = 32;

    /// <summary>Most items on the ground in one frame.</summary>
    public static int MostGround { get; set; } = 300;

    /// <summary>Most map blocks remembered as read; past it the memory is emptied, which costs only a re-count.</summary>
    public static int MostSceneBlocks { get; set; } = 16384;

    // ---- the counts /api/health prints ----------------------------------------------------------------------

    public static long Scenes { get; private set; }

    public static double ScenesMs { get; private set; }

    public static double WorstSceneMs { get; private set; }

    public static long SceneBlocksRead { get; private set; }

    public static long SceneBytes { get; private set; }

    public static double PictureMs { get; private set; }

    public static long Pictures { get; private set; }

    private static readonly HashSet<long> _sceneSeen = [];

    private static readonly ArrayBufferWriter<byte> _statics = new(16 * 1024);

    private static readonly List<Item> _groundScratch = new(64);

    /// <summary>The newest scene of a watched bot, as the loop cut it; null until the first. Any thread; does not mark it watched.</summary>
    public static string SceneOf(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        lock (_lock)
        {
            return _watches.TryGetValue(name.Trim(), out var watch) ? watch.Scene : null;
        }
    }

    // ---- the frame's part: what moves, and which scene goes with it ---------------------------------------------

    /// <summary>
    /// Writes <c>look</c> (the bot itself as the client would draw it), <c>ground</c> (the items lying about) and <c>pic</c>
    /// (which scene goes with this frame), cutting a new scene first when the bot has walked off the last one.
    /// </summary>
    private static void WritePicture(Utf8JsonWriter w, Watch watch, BotMobile bot, Map map, ref int blocks)
    {
        var started = Stopwatch.GetTimestamp();

        w.WritePropertyName("look");
        w.WriteStartObject();
        Look(w, bot);
        w.WriteEndObject();

        Ground(w);

        CutScene(watch, map, bot.X, bot.Y, ref blocks);

        w.WritePropertyName("pic");
        w.WriteStartObject();
        w.WriteNumber("scene", watch.SceneVer);
        w.WriteBoolean("sceneComplete", watch.SceneComplete);
        w.WriteNumber("radius", PictureRadius);
        w.WriteEndObject();

        Pictures++;
        PictureMs += Elapsed(started);
    }

    /// <summary>The layers the client draws on a body; the backpack, the bank, the mount and the vendor boxes are not among them.</summary>
    private static bool Drawn(Layer layer) => layer is >= Layer.OneHanded and <= Layer.InnerLegs and not Layer.Backpack;

    /// <summary>
    /// A mobile as the client is told about it: serial, height, body, hue, facing, war mode (2 holding a two-handed weapon),
    /// the mount's body and hue, and the worn items as layer, item id and hue in one flat array, hair and beard among them.
    /// </summary>
    private static void Look(Utf8JsonWriter w, Mobile m)
    {
        var solid = m.SolidHueOverride;

        w.WriteNumber("s", m.Serial.Value);
        w.WriteNumber("z", m.Z);
        w.WriteNumber("b", m.Body.BodyID);
        w.WriteNumber("h", solid >= 0 ? solid : m.Hue);
        w.WriteNumber("d", (int)m.Direction & 7);

        if (m.Warmode)
        {
            w.WriteNumber("w", m.FindItemOnLayer(Layer.TwoHanded) is BaseWeapon ? 2 : 1);
        }

        if (m.FindItemOnLayer(Layer.Mount) is { Deleted: false } mount)
        {
            w.WriteNumber("mb", MountBody(mount));
            w.WriteNumber("mh", solid >= 0 ? solid : mount.Hue);
        }

        w.WritePropertyName("eq");
        w.WriteStartArray();

        var items = m.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !item.Visible || !Drawn(item.Layer))
            {
                continue;
            }

            w.WriteNumberValue((int)item.Layer);
            w.WriteNumberValue(item.ItemID);
            w.WriteNumberValue(solid >= 0 ? solid : item.Hue);
        }

        if (m.HairItemID > 0)
        {
            w.WriteNumberValue((int)Layer.Hair);
            w.WriteNumberValue(m.HairItemID);
            w.WriteNumberValue(solid >= 0 ? solid : m.HairHue);
        }

        if (m.FacialHairItemID > 0)
        {
            w.WriteNumberValue((int)Layer.FacialHair);
            w.WriteNumberValue(m.FacialHairItemID);
            w.WriteNumberValue(solid >= 0 ? solid : m.FacialHairHue);
        }

        w.WriteEndArray();
    }

    /// <summary>
    /// The body a mount is drawn with. The client looks the mount item up in its own table; the server knows the creature
    /// under it, which is the same body. A mount with no creature (an ethereal, a guard's virtual horse) is drawn as the horse
    /// whose item it uses, as the server's own horses pair them, else as a brown horse: tiledata names no body for these items.
    /// </summary>
    private static int MountBody(Item mount)
    {
        if (mount is IMountItem { Mount: Mobile creature })
        {
            return creature.Body.BodyID;
        }

        return mount.ItemID switch
        {
            0x3EA0 => 0xE2,
            0x3EA1 => 0xE4,
            0x3EA2 => 0xCC,
            _      => 0xC8
        };
    }

    /// <summary>Called by <see cref="Items"/> for every item in the square, so the map's item query is walked once a frame.</summary>
    private static void NoteGround(BotMobile bot, Item item)
    {
        if (_groundScratch.Count >= MostGround || item.Deleted || !item.Visible || item is BaseMulti)
        {
            return;
        }

        if (Math.Max(Math.Abs(item.X - bot.X), Math.Abs(item.Y - bot.Y)) > PictureRadius)
        {
            return;
        }

        _groundScratch.Add(item);
    }

    /// <summary>
    /// The items lying about: id as drawn, place, height, hue, a number that is the body for a corpse and 1 for a stack the
    /// client draws twice, and the facing; a corpse adds what it wears.
    /// </summary>
    private static void Ground(Utf8JsonWriter w)
    {
        w.WritePropertyName("ground");
        w.WriteStartArray();

        for (var i = 0; i < _groundScratch.Count; i++)
        {
            var item = _groundScratch[i];
            var id = item.ItemID & 0xFFFF;
            var extra = 0;

            if (item is Corpse corpse && id == 0x2006)
            {
                extra = corpse.Amount;
            }
            else if (item.Amount > 1)
            {
                // Coins change their picture with the pile; other stacks are drawn twice, a little apart.
                if (id is 0x0EEA or 0x0EED or 0x0EF0)
                {
                    id += item.Amount > 5 ? 2 : 1;
                }
                else if ((TileData.ItemTable[id & TileData.MaxItemValue].Flags & TileFlag.Generic) != 0)
                {
                    extra = 1;
                }
            }

            w.WriteStartArray();
            w.WriteNumberValue(id);
            w.WriteNumberValue(item.X);
            w.WriteNumberValue(item.Y);
            w.WriteNumberValue(item.Z);
            w.WriteNumberValue(item.Hue);
            w.WriteNumberValue(extra);
            w.WriteNumberValue((int)item.Direction & 7);

            if (item is Corpse dead && id == 0x2006)
            {
                w.WriteStartArray();

                var worn = dead.EquipItems;

                for (var j = 0; worn != null && j < worn.Count; j++)
                {
                    if (worn[j] is { Deleted: false } piece && piece.Parent == dead && Drawn(piece.Layer))
                    {
                        w.WriteNumberValue((int)piece.Layer);
                        w.WriteNumberValue(piece.ItemID);
                        w.WriteNumberValue(piece.Hue);
                    }
                }

                if (dead.HairItemId > 0)
                {
                    w.WriteNumberValue((int)Layer.Hair);
                    w.WriteNumberValue(dead.HairItemId);
                    w.WriteNumberValue(dead.HairHue);
                }

                if (dead.FacialHairItemId > 0)
                {
                    w.WriteNumberValue((int)Layer.FacialHair);
                    w.WriteNumberValue(dead.FacialHairItemId);
                    w.WriteNumberValue(dead.FacialHairHue);
                }

                w.WriteEndArray();
            }

            w.WriteEndArray();
        }

        w.WriteEndArray();
        _groundScratch.Clear();
    }

    // ---- the scene: the ground and what is built on it ------------------------------------------------------------

    /// <summary>
    /// Cuts the scene again when the bot has walked further than the slack, the last one was not finished, or the multis in
    /// the square have changed; otherwise leaves it as it is.
    /// </summary>
    private static void CutScene(Watch watch, Map map, int x, int y, ref int blocks)
    {
        var half = PictureRadius + PictureSlack;
        var moved = watch.Scene == null || watch.SceneMap != map
                    || Math.Max(Math.Abs(x - watch.SceneX), Math.Abs(y - watch.SceneY)) > PictureSlack;
        var cx = moved ? x : watch.SceneX;
        var cy = moved ? y : watch.SceneY;

        // One more row and column than the square: the slopes of its last row and column are shaped by the tiles past them.
        var x0 = cx - half;
        var y0 = cy - half;
        var side = half * 2 + 2;
        var multis = Multis(map, x0, y0, side);

        if (!moved && watch.SceneComplete && multis == watch.SceneMultis)
        {
            return;
        }

        var started = Stopwatch.GetTimestamp();
        var tiles = map.Tiles;
        var complete = true;

        // Emptied before the square is counted, never halfway through it: a block forgotten in the middle would be left out of
        // a scene that still calls itself complete.
        if (_sceneSeen.Count >= MostSceneBlocks)
        {
            _sceneSeen.Clear();
        }

        // The blocks never read before, within the budget; the matrix keeps them once read.
        for (var by = y0 >> 3; by <= (y0 + side - 1) >> 3; by++)
        {
            for (var bx = x0 >> 3; bx <= (x0 + side - 1) >> 3; bx++)
            {
                if (bx < 0 || by < 0 || bx >= tiles.BlockWidth || by >= tiles.BlockHeight)
                {
                    continue;
                }

                var key = ((long)map.MapID << 40) | ((long)bx << 20) | (uint)by;

                if (_sceneSeen.Contains(key))
                {
                    continue;
                }

                if (blocks <= 0)
                {
                    complete = false;

                    continue;
                }

                blocks--;
                tiles.GetLandBlock(bx, by);
                tiles.GetStaticBlock(bx, by);
                _sceneSeen.Add(key);
                _sceneSeenCount = _sceneSeen.Count;
                SceneBlocksRead++;
            }
        }

        // Off the map, or in a block not read yet: 0xFFFF, which the page leaves undrawn.
        var land = new byte[side * side * 3];
        Array.Fill(land, (byte)0xFF);
        _statics.Clear();

        // Block by block, each block's land and statics looked up once.
        for (var by = y0 >> 3; by <= (y0 + side - 1) >> 3; by++)
        {
            for (var bx = x0 >> 3; bx <= (x0 + side - 1) >> 3; bx++)
            {
                if (bx < 0 || by < 0 || bx >= tiles.BlockWidth || by >= tiles.BlockHeight
                    || !_sceneSeen.Contains(((long)map.MapID << 40) | ((long)bx << 20) | (uint)by))
                {
                    continue;
                }

                var grounds = tiles.GetLandBlock(bx, by);
                var block = tiles.GetStaticBlock(bx, by);

                for (var wy = Math.Max(by << 3, y0); wy < Math.Min((by << 3) + 8, y0 + side); wy++)
                {
                    for (var wx = Math.Max(bx << 3, x0); wx < Math.Min((bx << 3) + 8, x0 + side); wx++)
                    {
                        if (wx >= map.Width || wy >= map.Height)
                        {
                            continue;
                        }

                        var tx = wx - x0;
                        var ty = wy - y0;
                        var at = (ty * side + tx) * 3;
                        var ground = grounds[((wy & 7) << 3) + (wx & 7)];
                        var id = ground.ID & 0x3FFF;

                        land[at] = (byte)id;
                        land[at + 1] = (byte)(id >> 8);
                        land[at + 2] = (byte)(sbyte)ground.Z;

                        var list = block[wx & 7][wy & 7];

                        for (var i = 0; list != null && i < list.Length; i++)
                        {
                            Record(tx, ty, list[i].ID, list[i].Z, list[i].Hue);
                        }
                    }
                }
            }
        }

        // The pieces of houses and boats, as the client is sent them: every piece the multi lists as shown.
        var pieces = 0;

        if (multis != 0)
        {
            foreach (var multi in map.GetMultisInBounds(new Rectangle2D(x0 - 32, y0 - 32, side + 64, side + 64)))
            {
                if (multi.Deleted)
                {
                    continue;
                }

                var list = multi.Components?.List;

                for (var i = 0; list != null && i < list.Length; i++)
                {
                    var entry = list[i];

                    if (i > 0 && entry.Flags == TileFlag.None)
                    {
                        continue;
                    }

                    var tx = multi.X + entry.OffsetX - x0;
                    var ty = multi.Y + entry.OffsetY - y0;

                    if (tx < 0 || ty < 0 || tx >= side || ty >= side)
                    {
                        continue;
                    }

                    Record(tx, ty, entry.ItemId, Math.Clamp(multi.Z + entry.OffsetZ, -128, 127), multi.Hue);
                    pieces++;
                }
            }
        }

        var ver = watch.SceneVer + 1;
        var name = watch.Real ?? watch.Name;
        var json = BotWebArt.PlainObject(
            w =>
            {
                w.WriteString("bot", name);
                w.WriteNumber("ver", ver);
                w.WriteString("map", map.Name);
                w.WriteNumber("x0", x0);
                w.WriteNumber("y0", y0);
                w.WriteNumber("side", side);
                w.WriteBoolean("complete", complete);
                w.WriteNumber("pieces", pieces);
                w.WriteString("land", Convert.ToBase64String(land));
                w.WriteString("statics", Convert.ToBase64String(_statics.WrittenSpan));
            }
        );

        watch.SceneMap = map;
        watch.SceneX = cx;
        watch.SceneY = cy;
        watch.SceneComplete = complete;
        watch.SceneMultis = multis;

        lock (_lock)
        {
            watch.SceneVer = ver;
            watch.Scene = json;
        }

        var ms = Elapsed(started);

        Scenes++;
        ScenesMs += ms;
        SceneBytes += json.Length;

        if (ms > WorstSceneMs)
        {
            WorstSceneMs = ms;
        }
    }

    /// <summary>A static as seven bytes: where in the square, the id, the height, the hue.</summary>
    private static void Record(int tx, int ty, int id, int z, int hue)
    {
        var span = _statics.GetSpan(7);

        span[0] = (byte)tx;
        span[1] = (byte)ty;
        span[2] = (byte)id;
        span[3] = (byte)(id >> 8);
        span[4] = (byte)(sbyte)z;
        span[5] = (byte)hue;
        span[6] = (byte)(hue >> 8);
        _statics.Advance(7);
    }

    /// <summary>A number that changes when a multi in or near the square comes, goes, moves or is rebuilt; 0 when there are none.</summary>
    private static long Multis(Map map, int x0, int y0, int side)
    {
        long sum = 0;

        foreach (var multi in map.GetMultisInBounds(new Rectangle2D(x0 - 32, y0 - 32, side + 64, side + 64)))
        {
            if (multi.Deleted)
            {
                continue;
            }

            sum = sum * 31 + multi.Serial.Value;
            sum = sum * 31 + multi.X * 7919 + multi.Y * 104729 + multi.Z;
            sum = sum * 31 + multi.ItemID + (multi.Components?.List?.Length ?? 0) * 65537L;
        }

        return sum;
    }

    // ---- what it costs ----------------------------------------------------------------------------------------

    private static void PictureHealth(Utf8JsonWriter w)
    {
        w.WritePropertyName("picture");
        w.WriteStartObject();
        w.WriteBoolean("enabled", Picture);
        w.WriteNumber("frames", Pictures);
        w.WriteNumber("msEach", Pictures <= 0 ? 0.0 : Math.Round(PictureMs / Pictures, 3));
        w.WriteNumber("scenes", Scenes);
        w.WriteNumber("sceneMsEach", Scenes <= 0 ? 0.0 : Math.Round(ScenesMs / Scenes, 3));
        w.WriteNumber("worstSceneMs", Math.Round(WorstSceneMs, 2));
        w.WriteNumber("sceneKBEach", Scenes <= 0 ? 0 : SceneBytes / Scenes / 1024);
        w.WriteNumber("blocksRead", SceneBlocksRead);
        w.WriteNumber("blocksKnown", _sceneSeenCount);
        w.WriteEndObject();

        BotWebArt.Health(w);
    }

    private static volatile int _sceneSeenCount;
}
