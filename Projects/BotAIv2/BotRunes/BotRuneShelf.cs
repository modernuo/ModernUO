using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>One place a guild holds a rune for: what it is called, what kind of place, and where the rune puts a body down.</summary>
public sealed class BotRunePlace
{
    /// <summary>"Trinsic", "the seat of The Blade", "the mouth of Despise".</summary>
    public string Name;

    /// <summary>town, seat or mouth.</summary>
    public string Kind;

    public Map Map;

    /// <summary>Where the rune was marked, which is where a recall or a gate puts a body down.</summary>
    public Point3D Spot;

    /// <summary>The mage that marked it.</summary>
    public string By;

    public DateTime When;

    /// <summary>Recalls and gates made to it.</summary>
    public int Uses;

    public override string ToString() => $"{Name} ({Spot.X}, {Spot.Y})";
}

/// <summary>
/// What each guild holds runes for, kept across restarts: the guild's knowledge of the places its mages have stood.
///
/// <para>
/// <b>The knowledge, not the runes.</b> A rune is an item in somebody's pack or in the library's book, and a restart
/// raises the population afresh with empty packs; what a guild knows is kept here instead, by guild name, and the
/// library's book is written from it (<see cref="BotRuneLibrary"/>) — the same thing <c>BotProgress</c> does for skills.
/// A place is one per name: the seat marked again after the hall moved replaces the old seat.
/// </para>
/// </summary>
public static class BotRuneShelf
{
    private static readonly Dictionary<string, List<BotRunePlace>> _shelves = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, int> _versions = new(StringComparer.Ordinal);

    private static readonly List<BotRunePlace> _none = [];

    /// <summary>Places dropped off a full shelf, the least used first.</summary>
    public static long Evicted { get; private set; }

    public static IReadOnlyList<BotRunePlace> Of(string guild) =>
        guild != null && _shelves.TryGetValue(guild, out var places) ? places : _none;

    /// <summary>A number that changes whenever the guild's shelf does, so the library knows when to write.</summary>
    public static int Version(string guild) => guild != null && _versions.TryGetValue(guild, out var v) ? v : 0;

    /// <summary>The guild's place nearest to <paramref name="at"/> within <paramref name="within"/>, or null.</summary>
    public static BotRunePlace Held(string guild, Map map, Point3D at, int within)
    {
        var places = Of(guild);
        BotRunePlace best = null;
        var nearest = int.MaxValue;

        for (var i = 0; i < places.Count; i++)
        {
            var place = places[i];
            var away = BotRunes.Apart(place.Spot, at);

            if (place.Map == map && away <= within && away < nearest)
            {
                nearest = away;
                best = place;
            }
        }

        return best;
    }

    /// <summary>The guild's place of this name, or null.</summary>
    public static BotRunePlace Named(string guild, string name)
    {
        var places = Of(guild);

        for (var i = 0; i < places.Count; i++)
        {
            if (string.Equals(places[i].Name, name, StringComparison.Ordinal))
            {
                return places[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Files a marked rune in the guild's knowledge. A place of the same name is replaced — a seat that moved is a new
    /// seat — and a shelf past <see cref="BotRunes.Shelf"/> drops its least used place, never the seat.
    /// </summary>
    public static BotRunePlace File(string guild, string name, string kind, Map map, Point3D spot, string by)
    {
        if (guild == null || name == null || map == null)
        {
            return null;
        }

        if (!_shelves.TryGetValue(guild, out var places))
        {
            _shelves[guild] = places = [];
        }

        var place = Named(guild, name);

        if (place == null)
        {
            place = new BotRunePlace { Name = name };
            places.Add(place);
        }

        place.Kind = kind;
        place.Map = map;
        place.Spot = spot;
        place.By = by;
        place.When = Core.Now;

        while (places.Count > Math.Max(1, BotRunes.Shelf))
        {
            var drop = -1;

            for (var i = 0; i < places.Count; i++)
            {
                var other = places[i];

                if (ReferenceEquals(other, place) || other.Kind == "seat")
                {
                    continue;
                }

                if (drop < 0 || other.Uses < places[drop].Uses || other.Uses == places[drop].Uses && other.When < places[drop].When)
                {
                    drop = i;
                }
            }

            if (drop < 0)
            {
                break;
            }

            places.RemoveAt(drop);
            Evicted++;
        }

        _versions[guild] = Version(guild) + 1;

        return place;
    }

    /// <summary>A recall or a gate to the guild's place nearest to where it landed.</summary>
    public static void Used(string guild, Map map, Point3D spot)
    {
        var place = Held(guild, map, spot, BotRunes.Near);

        if (place != null)
        {
            place.Uses++;
        }
    }

    public static int Places
    {
        get
        {
            var many = 0;

            foreach (var places in _shelves.Values)
            {
                many += places.Count;
            }

            return many;
        }
    }

    public static string Describe()
    {
        if (_shelves.Count == 0)
        {
            return "no guild holds a rune";
        }

        using var line = Server.Text.ValueStringBuilder.Create(256);

        line.Append($"{Places} places held by {_shelves.Count} guilds (");

        var first = true;

        foreach (var (guild, places) in _shelves)
        {
            line.Append(first ? "" : ", ");
            line.Append($"{guild} {places.Count}");
            first = false;
        }

        line.Append($"), {Evicted} dropped off a full shelf");

        return line.ToString();
    }

    // ---- the store ----

    internal static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_shelves.Count);

        foreach (var (guild, places) in _shelves)
        {
            writer.Write(guild);
            writer.WriteEncodedInt(places.Count);

            for (var i = 0; i < places.Count; i++)
            {
                var place = places[i];

                writer.Write(place.Name);
                writer.Write(place.Kind);
                writer.Write(place.Map);
                writer.Write(place.Spot);
                writer.Write(place.By);
                writer.Write(place.When);
                writer.WriteEncodedInt(place.Uses);
            }
        }
    }

    internal static int Load(IGenericReader reader)
    {
        _shelves.Clear();
        _versions.Clear();

        var read = 0;
        var guilds = reader.ReadEncodedInt();

        for (var g = 0; g < guilds; g++)
        {
            var guild = reader.ReadString();
            var many = reader.ReadEncodedInt();
            List<BotRunePlace> places = [];

            for (var i = 0; i < many; i++)
            {
                var place = new BotRunePlace
                {
                    Name = reader.ReadString(),
                    Kind = reader.ReadString(),
                    Map = reader.ReadMap(),
                    Spot = reader.ReadPoint3D(),
                    By = reader.ReadString(),
                    When = reader.ReadDateTime(),
                    Uses = reader.ReadEncodedInt()
                };

                if (place.Name != null && place.Map != null && place.Map != Map.Internal)
                {
                    places.Add(place);
                    read++;
                }
            }

            if (!string.IsNullOrEmpty(guild) && places.Count > 0)
            {
                _shelves[guild] = places;
                _versions[guild] = 1;
            }
        }

        return read;
    }

    /// <summary>
    /// Nothing, on a world reload: the knowledge is the guilds' and outlives the population, as the roadbook's walks do.
    /// A wipe clears it with the rest of the saves.
    /// </summary>
    public static void Forget()
    {
        Evicted = 0;
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26 review): every guild's shelf emptied, so the save writes <c>Saves/BotRunes</c>
    /// empty.</b> The note above promised it and the reset never did it. The shelf is keyed by the guild's name, and a new world
    /// frees every name (<c>BotGuilds.Forget</c>), so a guild founded again under an old name inherited the old guild's runes —
    /// "the seat of" a hall the reset had razed among them. Runes are the guild's property, not the island's map: the danger map,
    /// the zones and the roads walked stay; this goes. Empties exactly what <see cref="Save"/> writes, and the libraries' memory of
    /// what they wrote, so a new guild's book is written from its own shelf. Returns the guilds' shelves emptied.
    /// </summary>
    public static int Wipe()
    {
        var shelves = _shelves.Count;

        _shelves.Clear();
        _versions.Clear();
        Forget();
        BotRuneLibrary.Forget();

        return shelves;
    }
}

/// <summary>What each guild holds runes for, kept between boots. Shape 1. See <see cref="BotRuneShelf"/>.</summary>
public sealed class BotRuneStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRuneStore));

    private const int Shape = 1;

    private static BotRuneStore _store;

    public static void Configure() => _store ??= new BotRuneStore();

    public BotRuneStore() : base("BotRunes", 15)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotRuneShelf.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape < 1 || shape > Shape)
        {
            logger.Warning("The saved runes are shape {Found} and this build reads {Wanted}; the guilds start with none", shape, Shape);

            return;
        }

        var read = BotRuneShelf.Load(reader);

        if (read > 0)
        {
            logger.Information("Runes: {Read} places the guilds hold runes for read back from the save", read);
        }
    }
}
