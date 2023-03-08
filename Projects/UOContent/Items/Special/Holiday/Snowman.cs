using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2328, 0x2329)]
[SerializationGenerator(0, false)]
public partial class Snowman : Item, IDyable
{
    // All hail OSI staff
    private static readonly string[] titles =
    {
        /*  1 */ "Backflash",
        /*  2 */ "Carbon",
        /*  3 */ "Colbalistic",
        /*  4 */ "Comforl",
        /*  5 */ "Coppacchia",
        /*  6 */ "Cyrus",
        /*  7 */ "DannyB",
        /*  8 */ "DJSoul",
        /*  9 */ "DraconisRex",
        /* 10 */ "Earia",
        /* 11 */ "Foster",
        /* 12 */ "Gonzo",
        /* 13 */ "Haan",
        /* 14 */ "Halona",
        /* 15 */ "Hugo",
        /* 16 */ "Hyacinth",
        /* 17 */ "Imirian",
        /* 18 */ "Jinsol",
        /* 19 */ "Liciatia",
        /* 20 */ "Loewen",
        /* 21 */ "Loke",
        /* 22 */ "Magnus",
        /* 23 */ "Maleki",
        /* 24 */ "Morpheus",
        /* 25 */ "Obberron",
        /* 26 */ "Odee",
        /* 27 */ "Orbeus",
        /* 28 */ "Pax",
        /* 29 */ "Phields",
        /* 30 */ "Pigpen",
        /* 31 */ "Platinum",
        /* 32 */ "Polpol",
        /* 33 */ "Prume",
        /* 34 */ "Quinnly",
        /* 35 */ "Ragnarok",
        /* 36 */ "Rend",
        /* 37 */ "Roland",
        /* 38 */ "RyanM",
        /* 39 */ "Screach",
        /* 40 */ "Seraph",
        /* 41 */ "Silvani",
        /* 42 */ "Sherbear",
        /* 43 */ "SkyWalker",
        /* 44 */ "Snark",
        /* 45 */ "Sowl",
        /* 46 */ "Spada",
        /* 47 */ "Starblade",
        /* 48 */ "Tenacious",
        /* 49 */ "Tnez",
        /* 50 */ "Wasia",
        /* 51 */ "Zilo",
        /* 52 */ "Zippy",
        /* 53 */ "Zoer"
    };

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _title;

    [Constructible]
    public Snowman() : this(Utility.RandomDyedHue())
    {
    }

    [Constructible]
    public Snowman(int hue) : this(hue, GetRandomTitle())
    {
    }

    [Constructible]
    public Snowman(string title) : this(Utility.RandomDyedHue(), title)
    {
    }

    [Constructible]
    public Snowman(int hue, string title) : base(Utility.Random(0x2328, 2))
    {
        Weight = 10.0;
        Hue = hue;
        LootType = LootType.Blessed;

        _title = title;
    }

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;
        return true;
    }

    public static string GetRandomTitle() => titles.RandomElement();

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_title != null)
        {
            list.Add(1062841, _title); // ~1_NAME~ the Snowman
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Utility.Intern(ref _title);
    }
}
