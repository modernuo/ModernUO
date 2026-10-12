using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>One people of the population, as <c>Configuration/bot-names.json</c> describes it.</summary>
public sealed class BotPeopleSettings
{
    /// <summary>The engine's race the people is born as: Human or Elf. A people with no race of its own (dwarves) is Human.</summary>
    public string Race { get; set; }

    /// <summary>How many of every hundred newborns are of this people, before the class's own leaning.</summary>
    public double? Share { get; set; }

    /// <summary>Whether its men wear beards.</summary>
    public bool? Beards { get; set; }

    public string[] Male { get; set; }

    public string[] Female { get; set; }

    /// <summary>Family names, for when the given names run out: "Aldric" and then "Aldric Ashdown".</summary>
    public string[] Houses { get; set; }

    /// <summary>How much likelier a newborn of a class is to be of this people: class name to weight; a class not named weighs one.</summary>
    public Dictionary<string, double> Classes { get; set; }
}

public sealed class BotNamesSettings
{
    public Dictionary<string, BotPeopleSettings> Peoples { get; set; }
}

/// <summary>
/// Who the bots are born as: their people, their name, their race and their looks.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, points seven and eight: "add a settings document that says where the bots' names
/// come from; invent two hundred names of the period, human, elvish and dwarvish, from the warlike to the magical; and
/// teach the bots to be born of different classes and races."</b> Until now every bot was a human of one of sixty-four
/// given names and twenty-two houses, dealt in order (<c>BotPopulation.Names</c>), and every class drew from the same
/// pool.
/// </para>
///
/// <para>
/// <b>The file is the authority, and the code is what the file says when it says nothing.</b> The whole bank below is
/// written to <c>Configuration/bot-names.json</c> the first time the shard runs, so the names can be changed, added to
/// or replaced without touching the code; a people taken out of the file is a people nobody is born as. A name is
/// never dealt twice while somebody wears it, and a newcomer is never given a name whose learning the shard still
/// remembers (<c>BotProgress</c>), which is the old rule kept: the name is the key to everything a bot has learned.
/// </para>
///
/// <para>
/// <b>Race is the engine's, and a people is a name for it.</b> Humans and elves are bodies the client draws
/// (<c>Race.Human</c>, <c>Race.Elf</c>); there are no dwarves in this era's client, so the dwarves are humans with
/// beards, dwarvish names and a dwarf's leanings — smiths, warriors and architects more often than mages. Elves lean
/// the other way. The class is dealt first, from the configured mix, as it always was; the people is drawn for the
/// class, by share times the class's weight; the name follows the people and the sex.
/// </para>
///
/// <para>
/// <b>Which people a bot is, is read off its name.</b> Nothing new is saved: the race is the engine's and survives a
/// restart with the body; the people is whichever list the given name is in, so the dashboard and the log can say it
/// and the wipe has nothing to clear.
/// </para>
/// </summary>
public static class BotPeoples
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPeoples));

    private const string ConfigPath = "Configuration/bot-names.json";

    public sealed class People
    {
        public string Name;

        public Race Race;

        public double Share;

        public bool Beards;

        public string[] Male = [];

        public string[] Female = [];

        public string[] Houses = [];

        public Dictionary<string, double> Classes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Where the dealing stands in each list, so names go out in the file's order.</summary>
        public int NextMale;

        public int NextFemale;

        public long Born;

        public double Weight(BotClass klass) =>
            Share * (klass?.Name != null && Classes.TryGetValue(klass.Name, out var w) ? Math.Max(0.0, w) : 1.0);

        public override string ToString() => $"{Name} ({Race?.Name}, share {Share:F0}, {Male.Length + Female.Length} names, {Houses.Length} houses)";
    }

    private static readonly List<People> _peoples = [];

    private static readonly Dictionary<string, People> _byName = new(StringComparer.OrdinalIgnoreCase);

    public static bool Loaded { get; private set; }

    public static IReadOnlyList<People> All => _peoples;

    /// <summary>Whether the file gave any people at all; when not, the population's old pool is used as before.</summary>
    public static bool Any => _peoples.Count > 0;

    public static long Named { get; private set; }

    /// <summary>Reads the file, or writes the bank below as the file and reads that.</summary>
    public static void Load()
    {
        if (Loaded)
        {
            return;
        }

        Loaded = true;

        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotNamesSettings>(path);

        if (settings?.Peoples == null)
        {
            settings = Defaults();
            JsonConfig.Serialize(path, settings);
            logger.Information("Wrote the bank of names to {Path}: {Peoples} peoples, {Names} names; edit it to change who is born", ConfigPath, settings.Peoples.Count, Count(settings));
        }

        foreach (var (name, people) in settings.Peoples)
        {
            if (string.IsNullOrWhiteSpace(name) || people == null)
            {
                continue;
            }

            var race = string.Equals(people.Race, "Elf", StringComparison.OrdinalIgnoreCase) ? Race.Elf : Race.Human;
            var made = new People
            {
                Name = name,
                Race = race,
                Share = Math.Max(0.0, people.Share ?? 1.0),
                Beards = people.Beards ?? false,
                Male = Clean(people.Male),
                Female = Clean(people.Female),
                Houses = Clean(people.Houses)
            };

            if (people.Classes != null)
            {
                foreach (var (klass, weight) in people.Classes)
                {
                    if (!string.IsNullOrWhiteSpace(klass))
                    {
                        made.Classes[klass] = weight;
                    }
                }
            }

            if (made.Male.Length + made.Female.Length == 0)
            {
                logger.Warning("The people {People} in {Path} has no names and nobody will be born of it", name, ConfigPath);

                continue;
            }

            _peoples.Add(made);
            _byName[name] = made;

            for (var i = 0; i < made.Male.Length; i++)
            {
                _byName.TryAdd(made.Male[i], made);
            }

            for (var i = 0; i < made.Female.Length; i++)
            {
                _byName.TryAdd(made.Female[i], made);
            }
        }

        logger.Information("Peoples: {Peoples} read from {Path}: {List}", _peoples.Count, ConfigPath, string.Join("; ", _peoples));
    }

    private static string[] Clean(string[] names)
    {
        if (names == null)
        {
            return [];
        }

        List<string> kept = [];

        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i]?.Trim();

            if (!string.IsNullOrEmpty(name) && !kept.Exists(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)))
            {
                kept.Add(name);
            }
        }

        return kept.ToArray();
    }

    private static int Count(BotNamesSettings settings)
    {
        var n = 0;

        foreach (var (_, p) in settings.Peoples)
        {
            n += (p?.Male?.Length ?? 0) + (p?.Female?.Length ?? 0);
        }

        return n;
    }

    /// <summary>The people a newborn of this class is drawn as: share times the class's weight, by lot.</summary>
    public static People Pick(BotClass klass)
    {
        Load();

        if (_peoples.Count == 0)
        {
            return null;
        }

        var total = 0.0;

        for (var i = 0; i < _peoples.Count; i++)
        {
            total += _peoples[i].Weight(klass);
        }

        if (total <= 0.0)
        {
            return _peoples[Utility.Random(_peoples.Count)];
        }

        var roll = Utility.RandomDouble() * total;

        for (var i = 0; i < _peoples.Count; i++)
        {
            roll -= _peoples[i].Weight(klass);

            if (roll <= 0.0)
            {
                return _peoples[i];
            }
        }

        return _peoples[^1];
    }

    /// <summary>The people this bot belongs to, by its given name; null for a name from the old pool.</summary>
    public static People Of(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var space = name.IndexOf(' ');
        var given = space > 0 ? name[..space] : name;

        return _byName.TryGetValue(given, out var people) && !string.Equals(people.Name, given, StringComparison.OrdinalIgnoreCase) ? people : null;
    }

    /// <summary>Whether this name is one of the bank's, of any people.</summary>
    public static bool Knows(string name) => Of(name) != null;

    /// <summary>
    /// A name of the people for a newborn of that sex: the next unworn one in the file's order, and past the end of the
    /// list a house name — "Aldric Ashdown" — and never a number. See <c>BotPopulation.Christen</c> for the rule.
    /// </summary>
    public static string Christen(People people, bool female, Func<string, bool> taken)
    {
        if (people == null)
        {
            return null;
        }

        var pool = female ? people.Female : people.Male;

        if (pool.Length == 0)
        {
            pool = female ? people.Male : people.Female;
            female = !female;
        }

        if (pool.Length == 0)
        {
            return null;
        }

        var houses = people.Houses.Length > 0 ? people.Houses : [ "of the Road" ];
        var most = pool.Length * (houses.Length + 1);

        for (var looked = 0; looked < most; looked++)
        {
            var index = female ? people.NextFemale++ : people.NextMale++;
            var name = index < pool.Length
                ? pool[index]
                : $"{pool[index % pool.Length]} {houses[(index / pool.Length - 1) % houses.Length]}";

            if (taken == null || !taken(name))
            {
                people.Born++;
                Named++;

                return name;
            }
        }

        return null;
    }

    public static string Describe()
    {
        if (!Any)
        {
            return "no peoples: the old pool of names";
        }

        using var line = Server.Text.ValueStringBuilder.Create(256);

        line.Append($"{Named} named of {_peoples.Count} peoples:");

        for (var i = 0; i < _peoples.Count; i++)
        {
            line.Append($" {_peoples[i].Name} {_peoples[i].Born}");
        }

        return line.ToString();
    }

    public static void Forget()
    {
        for (var i = 0; i < _peoples.Count; i++)
        {
            _peoples[i].NextMale = 0;
            _peoples[i].NextFemale = 0;
            _peoples[i].Born = 0;
        }

        Named = 0;
    }

    /// <summary>
    /// The bank: two hundred names of the period, three peoples. Humans of the towns; elves of the greenwood, who lean to the
    /// bow and the book; dwarves of the hills, who lean to the hammer and the shield. Written to the file once and read from
    /// it after.
    /// </summary>
    public static BotNamesSettings Defaults() =>
        new()
        {
            Peoples = new Dictionary<string, BotPeopleSettings>
            {
                ["Human"] = new()
                {
                    Race = "Human",
                    Share = 5,
                    Beards = false,
                    Male =
                    [
                        "Aldric", "Berengar", "Cedric", "Dunstan", "Edmund", "Falk", "Godfrey", "Harald", "Ingram", "Joran",
                        "Kael", "Leofwin", "Marcus", "Norbert", "Osric", "Piran", "Rafe", "Sigurd", "Tancred", "Ansgar",
                        "Varen", "Wilhelm", "Yorick", "Ambrose", "Baldwin", "Corvin", "Dorian", "Everard", "Fulk", "Gareth",
                        "Hadrian", "Isembard", "Jasper", "Kendrick", "Lucan", "Malachi", "Nolan", "Orlando", "Percival", "Reynard"
                    ],
                    Female =
                    [
                        "Adela", "Beatrix", "Cerys", "Dalla", "Edith", "Freya", "Giselle", "Hilde", "Imogen", "Juliana",
                        "Katrin", "Liesl", "Maud", "Nerys", "Odile", "Petra", "Rosalind", "Sigrid", "Thora", "Ursula",
                        "Verity", "Wilma", "Ysabel", "Annora", "Brunhild", "Clarice", "Delphine", "Elowen", "Fenella", "Greta",
                        "Helewise", "Isolt", "Jocelyn", "Kirsten", "Lorelei", "Margery", "Nesta", "Orabel", "Philippa", "Rowena"
                    ],
                    Houses =
                    [
                        "Ashdown", "Blackbriar", "Coldwell", "Duskmere", "Eastmarch", "Fairholt", "Greywood", "Hartley",
                        "Ironvale", "Larkspur", "Marlow", "Northgate", "Oakhurst", "Pinewood", "Quarrytop", "Ravenscar",
                        "Stonebridge", "Thornwood", "Umberly", "Vinemoor", "Westford", "Yewdale"
                    ],
                    Classes = new Dictionary<string, double>()
                },
                ["Elf"] = new()
                {
                    Race = "Elf",
                    Share = 3,
                    Beards = false,
                    Male =
                    [
                        "Aelrindel", "Caelith", "Daeron", "Elandor", "Faelar", "Galadrion", "Haldir", "Ithilion", "Kaelthas", "Lorandil",
                        "Mirthal", "Naevys", "Orophin", "Quelindor", "Rhistel", "Sylvaris", "Thalion", "Uldreth", "Vaeril", "Yllarion",
                        "Aerendyl", "Belthorn", "Cirdan", "Elowir", "Fenthwe", "Ilphrin", "Larethian", "Nymarel", "Rillifane", "Sarendil"
                    ],
                    Female =
                    [
                        "Aelith", "Caelynn", "Delvira", "Elenwe", "Faelyn", "Galathil", "Ilyrana", "Keyleth", "Liriel", "Miriel",
                        "Naeris", "Oriel", "Quillathe", "Rhiannel", "Saelihn", "Thalia", "Ulaine", "Valindra", "Ysolde", "Aerith",
                        "Bryseis", "Cyrelle", "Elaria", "Faunalyn", "Ithronel", "Lyrindel", "Meliora", "Nimue", "Sylwen", "Vesryn"
                    ],
                    Houses =
                    [
                        "Silverleaf", "Moonshadow", "Starbrook", "Dawnwhisper", "Nightbloom", "Willowmere", "Everbright", "Greenwood",
                        "Mistvale", "Songwind"
                    ],
                    Classes = new Dictionary<string, double>
                    {
                        ["Mage"] = 3.0, ["Archer"] = 3.0, ["WarriorMage"] = 2.0, ["WarriorArcher"] = 2.0, ["Healer"] = 2.0,
                        ["Sage"] = 2.0, ["Gatherer"] = 1.5, ["Warrior"] = 0.5, ["Crafter"] = 0.5, ["Brawler"] = 0.3,
                        ["Architect"] = 0.7, ["Baron"] = 1.0, ["Captain"] = 1.0
                    }
                },
                ["Dwarf"] = new()
                {
                    Race = "Human",
                    Share = 2,
                    Beards = true,
                    Male =
                    [
                        "Balgrim", "Dorin", "Thorgar", "Brokk", "Durgan", "Gimrund", "Harbek", "Kargan", "Morgrim", "Orsik",
                        "Rurik", "Thrain", "Brom", "Vondal", "Baern", "Einar", "Farrin", "Gorm", "Hrolf", "Ivor",
                        "Kettil", "Lodin", "Magnar", "Nordak", "Ragnar", "Sigmund", "Torvald", "Varek", "Wulfgar", "Ulfgar"
                    ],
                    Female =
                    [
                        "Amberle", "Bardryn", "Dagna", "Eldeth", "Gunnloda", "Helja", "Ingrun", "Katra", "Liftrasa", "Mardred",
                        "Norna", "Ovina", "Riswynn", "Sigra", "Torbera", "Vistra", "Yngrid", "Brynhild", "Dagrun", "Frida",
                        "Gerdrun", "Hallgerd", "Ingibjorg", "Jorunna", "Kolbrun", "Ljufa", "Moira", "Ragna", "Solveig", "Thyra"
                    ],
                    Houses =
                    [
                        "Ironfoot", "Stonebeard", "Deepdelver", "Anvilhand", "Goldvein", "Rockhewer", "Emberforge", "Hammerfall",
                        "Brightaxe", "Coppervein"
                    ],
                    Classes = new Dictionary<string, double>
                    {
                        ["Crafter"] = 3.0, ["Warrior"] = 2.5, ["Brawler"] = 2.0, ["Architect"] = 2.0, ["Gatherer"] = 1.5,
                        ["Captain"] = 1.5, ["Baron"] = 1.0, ["Mage"] = 0.3, ["Archer"] = 0.5, ["Healer"] = 0.7,
                        ["WarriorMage"] = 0.7, ["WarriorArcher"] = 0.7, ["Sage"] = 0.7
                    }
                }
            }
        };
}
