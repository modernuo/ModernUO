using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Misc;
using Server.Network;

namespace Server.Items
{
    public abstract class BaseHat : BaseClothing, IShipwreckedItem
    {
        public BaseHat(int itemID, int hue = 0) : base(itemID, Layer.Helm, hue)
        {
        }

        public BaseHat(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsShipwreckedItem { get; set; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(IsShipwreckedItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        IsShipwreckedItem = reader.ReadBool();
                        break;
                    }
            }
        }

        public override void AddEquipInfoAttributes(Mobile from, List<EquipInfoAttribute> attrs)
        {
            base.AddEquipInfoAttributes(from, attrs);

            if (IsShipwreckedItem)
            {
                attrs.Add(new EquipInfoAttribute(1041645)); // recovered from a shipwreck
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
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
    public class Kasa : BaseHat
    {
        [Constructible]
        public Kasa(int hue = 0) : base(0x2798, hue) => Weight = 3.0;

        public Kasa(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    [Flippable(0x278F, 0x27DA)]
    public class ClothNinjaHood : BaseHat
    {
        [Constructible]
        public ClothNinjaHood(int hue = 0) : base(0x278F, hue) => Weight = 2.0;

        public ClothNinjaHood(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 9;
        public override int BaseEnergyResistance => 9;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    [Flippable(0x2306, 0x2305)]
    public class FlowerGarland : BaseHat
    {
        [Constructible]
        public FlowerGarland(int hue = 0) : base(0x2306, hue) => Weight = 1.0;

        public FlowerGarland(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 9;
        public override int BaseEnergyResistance => 9;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class FloppyHat : BaseHat
    {
        [Constructible]
        public FloppyHat(int hue = 0) : base(0x1713, hue) => Weight = 1.0;

        public FloppyHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class WideBrimHat : BaseHat
    {
        [Constructible]
        public WideBrimHat(int hue = 0) : base(0x1714, hue) => Weight = 1.0;

        public WideBrimHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Cap : BaseHat
    {
        [Constructible]
        public Cap(int hue = 0) : base(0x1715, hue) => Weight = 1.0;

        public Cap(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SkullCap : BaseHat
    {
        [Constructible]
        public SkullCap(int hue = 0) : base(0x1544, hue) => Weight = 1.0;

        public SkullCap(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 8;

        public override int InitMinHits => Core.ML ? 14 : 7;
        public override int InitMaxHits => Core.ML ? 28 : 12;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Bandana : BaseHat
    {
        [Constructible]
        public Bandana(int hue = 0) : base(0x1540, hue) => Weight = 1.0;

        public Bandana(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 8;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class BearMask : BaseHat
    {
        [Constructible]
        public BearMask(int hue = 0) : base(0x1545, hue) => Weight = 5.0;

        public BearMask(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DeerMask : BaseHat
    {
        [Constructible]
        public DeerMask(int hue = 0) : base(0x1547, hue) => Weight = 4.0;

        public DeerMask(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class HornedTribalMask : BaseHat
    {
        [Constructible]
        public HornedTribalMask(int hue = 0) : base(0x1549, hue) => Weight = 2.0;

        public HornedTribalMask(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class TribalMask : BaseHat
    {
        [Constructible]
        public TribalMask(int hue = 0) : base(0x154B, hue) => Weight = 2.0;

        public TribalMask(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class TallStrawHat : BaseHat
    {
        [Constructible]
        public TallStrawHat(int hue = 0) : base(0x1716, hue) => Weight = 1.0;

        public TallStrawHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class StrawHat : BaseHat
    {
        [Constructible]
        public StrawHat(int hue = 0) : base(0x1717, hue) => Weight = 1.0;

        public StrawHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class OrcishKinMask : BaseHat
    {
        [Constructible]
        public OrcishKinMask(int hue = 0x8A4) : base(0x141B, hue) => Weight = 2.0;

        public OrcishKinMask(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            /*if (Hue != 0x8A4)
              Hue = 0x8A4;*/
        }
    }

    public class SavageMask : BaseHat
    {
        [Constructible]
        public SavageMask() : this(GetRandomHue())
        {
        }

        [Constructible]
        public SavageMask(int hue) : base(0x154B, hue) => Weight = 2.0;

        public SavageMask(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            /*if (Hue != 0 && (Hue < 2101 || Hue > 2130))
              Hue = GetRandomHue();*/
        }
    }

    public class WizardsHat : BaseHat
    {
        [Constructible]
        public WizardsHat(int hue = 0) : base(0x1718, hue) => Weight = 1.0;

        public WizardsHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class MagicWizardsHat : BaseHat
    {
        [Constructible]
        public MagicWizardsHat(int hue = 0) : base(0x1718, hue) => Weight = 1.0;

        public MagicWizardsHat(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Bonnet : BaseHat
    {
        [Constructible]
        public Bonnet(int hue = 0) : base(0x1719, hue) => Weight = 1.0;

        public Bonnet(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class FeatheredHat : BaseHat
    {
        [Constructible]
        public FeatheredHat(int hue = 0) : base(0x171A, hue) => Weight = 1.0;

        public FeatheredHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class TricorneHat : BaseHat
    {
        [Constructible]
        public TricorneHat(int hue = 0) : base(0x171B, hue) => Weight = 1.0;

        public TricorneHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class JesterHat : BaseHat
    {
        [Constructible]
        public JesterHat(int hue = 0) : base(0x171C, hue) => Weight = 1.0;

        public JesterHat(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
