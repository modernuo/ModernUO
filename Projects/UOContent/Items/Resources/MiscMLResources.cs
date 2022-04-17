using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Blight : Item
    {
        [Constructible]
        public Blight(int amount = 1) : base(0x3183)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class LuminescentFungi : Item
    {
        [Constructible]
        public LuminescentFungi(int amount = 1) : base(0x3191)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class CapturedEssence : Item
    {
        [Constructible]
        public CapturedEssence(int amount = 1) : base(0x318E)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EyeOfTheTravesty : Item
    {
        [Constructible]
        public EyeOfTheTravesty(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public EyeOfTheTravesty(int amount = 1) : base(0x318D)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Corruption : Item
    {
        [Constructible]
        public Corruption(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Corruption(int amount = 1) : base(0x3184)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class DreadHornMane : Item
    {
        [Constructible]
        public DreadHornMane(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public DreadHornMane(int amount = 1) : base(0x318A)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class ParasiticPlant : Item
    {
        [Constructible]
        public ParasiticPlant(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public ParasiticPlant(int amount = 1) : base(0x3190)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Muculent : Item
    {
        [Constructible]
        public Muculent(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Muculent(int amount = 1) : base(0x3188)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class DiseasedBark : Item
    {
        [Constructible]
        public DiseasedBark(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public DiseasedBark(int amount = 1) : base(0x318B)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class BarkFragment : Item
    {
        [Constructible]
        public BarkFragment(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public BarkFragment(int amount = 1) : base(0x318F)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class GrizzledBones : Item
    {
        [Constructible]
        public GrizzledBones(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public GrizzledBones(int amount = 1) : base(0x318C)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class LardOfParoxysmus : Item
    {
        [Constructible]
        public LardOfParoxysmus(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public LardOfParoxysmus(int amount = 1) : base(0x3189)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PerfectEmerald : Item
    {
        [Constructible]
        public PerfectEmerald(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public PerfectEmerald(int amount = 1) : base(0x3194)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class DarkSapphire : Item
    {
        [Constructible]
        public DarkSapphire(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public DarkSapphire(int amount = 1) : base(0x3192)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Turquoise : Item
    {
        [Constructible]
        public Turquoise(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Turquoise(int amount = 1) : base(0x3193)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EcruCitrine : Item
    {
        [Constructible]
        public EcruCitrine(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public EcruCitrine(int amount = 1) : base(0x3195)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class WhitePearl : Item
    {
        [Constructible]
        public WhitePearl(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public WhitePearl(int amount = 1) : base(0x3196)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class FireRuby : Item
    {
        [Constructible]
        public FireRuby(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public FireRuby(int amount = 1) : base(0x3197)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class BlueDiamond : Item
    {
        [Constructible]
        public BlueDiamond(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public BlueDiamond(int amount = 1) : base(0x3198)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class BrilliantAmber : Item
    {
        [Constructible]
        public BrilliantAmber(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public BrilliantAmber(int amount = 1) : base(0x3199)
        {
            Stackable = true;
            Amount = amount;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Scourge : Item
    {
        [Constructible]
        public Scourge(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Scourge(int amount = 1) : base(0x3185)
        {
            Stackable = true;
            Amount = amount;
            Hue = 150;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Putrefication : Item
    {
        [Constructible]
        public Putrefication(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Putrefication(int amount = 1) : base(0x3186)
        {
            Stackable = true;
            Amount = amount;
            Hue = 883;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Taint : Item
    {
        [Constructible]
        public Taint(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Taint(int amount = 1) : base(0x3187)
        {
            Stackable = true;
            Amount = amount;
            Hue = 731;
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x315A, 0x315B)]
    public partial class PristineDreadHorn : Item
    {
        [Constructible]
        public PristineDreadHorn() : base(0x315A)
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class SwitchItem : Item
    {
        [Constructible]
        public SwitchItem(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public SwitchItem(int amount = 1) : base(0x2F5F)
        {
            Stackable = true;
            Amount = amount;
        }
    }
}
