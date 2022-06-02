using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Misc;
using Server.Network;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseHat : BaseClothing, IShipwreckedItem
    {
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _isShipwreckedItem;

        public BaseHat(int itemID, int hue = 0) : base(itemID, Layer.Helm, hue)
        {
        }

        public override void AddEquipInfoAttributes(Mobile from, List<EquipInfoAttribute> attrs)
        {
            base.AddEquipInfoAttributes(from, attrs);

            if (IsShipwreckedItem)
            {
                attrs.Add(new EquipInfoAttribute(1041645)); // recovered from a shipwreck
            }
        }

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            if (IsShipwreckedItem)
            {
                list.Add(1041645); // recovered from a shipwreck
            }
        }

        public override int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            BaseTool tool, CraftItem craftItem, int resHue
        )
        {
            Quality = (ClothingQuality)quality;

            if (Quality == ClothingQuality.Exceptional)
            {
                DistributeBonuses(
                    tool is BaseRunicTool ? 6 :
                    Core.SE ? 15 : 14
                ); // BLAME OSI. (We can't confirm it's an OSI bug yet.)
            }

            return base.OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, craftItem, resHue);
        }
    }

    [Flippable(0x2798, 0x27E3)]
    [SerializationGenerator(0, false)]
    public partial class Kasa : BaseHat
    {
        [Constructible]
        public Kasa(int hue = 0) : base(0x2798, hue) => Weight = 3.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [Flippable(0x278F, 0x27DA)]
    [SerializationGenerator(0, false)]
    public partial class ClothNinjaHood : BaseHat
    {
        [Constructible]
        public ClothNinjaHood(int hue = 0) : base(0x278F, hue) => Weight = 2.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 9;
        public override int BaseEnergyResistance => 9;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [Flippable(0x2306, 0x2305)]
    [SerializationGenerator(0, false)]
    public partial class FlowerGarland : BaseHat
    {
        [Constructible]
        public FlowerGarland(int hue = 0) : base(0x2306, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 9;
        public override int BaseEnergyResistance => 9;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class FloppyHat : BaseHat
    {
        [Constructible]
        public FloppyHat(int hue = 0) : base(0x1713, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class WideBrimHat : BaseHat
    {
        [Constructible]
        public WideBrimHat(int hue = 0) : base(0x1714, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class Cap : BaseHat
    {
        [Constructible]
        public Cap(int hue = 0) : base(0x1715, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class SkullCap : BaseHat
    {
        [Constructible]
        public SkullCap(int hue = 0) : base(0x1544, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 8;

        public override int InitMinHits => Core.ML ? 14 : 7;
        public override int InitMaxHits => Core.ML ? 28 : 12;
    }

    [SerializationGenerator(0, false)]
    public partial class Bandana : BaseHat
    {
        [Constructible]
        public Bandana(int hue = 0) : base(0x1540, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 8;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class BearMask : BaseHat
    {
        [Constructible]
        public BearMask(int hue = 0) : base(0x1545, hue) => Weight = 5.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 8;
        public override int BasePoisonResistance => 4;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class DeerMask : BaseHat
    {
        [Constructible]
        public DeerMask(int hue = 0) : base(0x1547, hue) => Weight = 4.0;

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 8;
        public override int BasePoisonResistance => 1;
        public override int BaseEnergyResistance => 7;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class HornedTribalMask : BaseHat
    {
        [Constructible]
        public HornedTribalMask(int hue = 0) : base(0x1549, hue) => Weight = 2.0;

        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 9;
        public override int BaseColdResistance => 0;
        public override int BasePoisonResistance => 4;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class TribalMask : BaseHat
    {
        [Constructible]
        public TribalMask(int hue = 0) : base(0x154B, hue) => Weight = 2.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 0;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class TallStrawHat : BaseHat
    {
        [Constructible]
        public TallStrawHat(int hue = 0) : base(0x1716, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class StrawHat : BaseHat
    {
        [Constructible]
        public StrawHat(int hue = 0) : base(0x1717, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class OrcishKinMask : BaseHat
    {
        [Constructible]
        public OrcishKinMask(int hue = 0x8A4) : base(0x141B, hue) => Weight = 2.0;

        public override int BasePhysicalResistance => 1;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 7;
        public override int BaseEnergyResistance => 8;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override string DefaultName => "a mask of orcish kin";

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        public override bool CanEquip(Mobile m)
        {
            if (!base.CanEquip(m))
            {
                return false;
            }

            if (m.BodyMod == 183 || m.BodyMod == 184)
            {
                m.SendLocalizedMessage(1061629); // You can't do that while wearing savage kin paint.
                return false;
            }

            return true;
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile mobile)
            {
                Titles.AwardKarma(mobile, -20, true);
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class SavageMask : BaseHat
    {
        [Constructible]
        public SavageMask() : this(GetRandomHue())
        {
        }

        [Constructible]
        public SavageMask(int hue) : base(0x154B, hue) => Weight = 2.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 0;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 10;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public static int GetRandomHue()
        {
            var v = Utility.RandomBirdHue();

            if (v == 2101)
            {
                v = 0;
            }

            return v;
        }

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class WizardsHat : BaseHat
    {
        [Constructible]
        public WizardsHat(int hue = 0) : base(0x1718, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class MagicWizardsHat : BaseHat
    {
        [Constructible]
        public MagicWizardsHat(int hue = 0) : base(0x1718, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override int LabelNumber => 1041072; // a magical wizard's hat

        public override int BaseStrBonus => -5;
        public override int BaseDexBonus => -5;
        public override int BaseIntBonus => +5;
    }

    [SerializationGenerator(0, false)]
    public partial class Bonnet : BaseHat
    {
        [Constructible]
        public Bonnet(int hue = 0) : base(0x1719, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class FeatheredHat : BaseHat
    {
        [Constructible]
        public FeatheredHat(int hue = 0) : base(0x171A, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class TricorneHat : BaseHat
    {
        [Constructible]
        public TricorneHat(int hue = 0) : base(0x171B, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }

    [SerializationGenerator(0, false)]
    public partial class JesterHat : BaseHat
    {
        [Constructible]
        public JesterHat(int hue = 0) : base(0x171C, hue) => Weight = 1.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;
    }
}
