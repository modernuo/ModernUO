using System;
using System.Collections.Generic;

namespace Server.Items
{
    [Furniture]
    [Flippable(0x2815, 0x2816)]
    public class TallCabinet : BaseContainer
    {
        [Constructible]
        public TallCabinet() : base(0x2815) => Weight = 1.0;

        public TallCabinet(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0x2817, 0x2818)]
    public class ShortCabinet : BaseContainer
    {
        [Constructible]
        public ShortCabinet() : base(0x2817) => Weight = 1.0;

        public ShortCabinet(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0x2857, 0x2858)]
    public class RedArmoire : BaseContainer
    {
        [Constructible]
        public RedArmoire() : base(0x2857) => Weight = 1.0;

        public RedArmoire(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0x285D, 0x285E)]
    public class CherryArmoire : BaseContainer
    {
        [Constructible]
        public CherryArmoire() : base(0x285D) => Weight = 1.0;

        public CherryArmoire(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0x285B, 0x285C)]
    public class MapleArmoire : BaseContainer
    {
        [Constructible]
        public MapleArmoire() : base(0x285B) => Weight = 1.0;

        public MapleArmoire(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0x2859, 0x285A)]
    public class ElegantArmoire : BaseContainer
    {
        [Constructible]
        public ElegantArmoire() : base(0x2859) => Weight = 1.0;

        public ElegantArmoire(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Serializable(0)]
    [Flippable(0x2D07, 0x2D08)]
    public partial class FancyElvenArmoire : BaseContainer
    {
        [Constructible]
        public FancyElvenArmoire() : base(0x2D07) => Weight = 1.0;
        public override int DefaultGumpID => 0x4E;
        public override int DefaultDropSound => 0x42;
    }

    [Furniture]
    [Serializable(0)]
    [Flippable(0x2D05, 0x2D06)]
    public partial class SimpleElvenArmoire : BaseContainer
    {
        [Constructible]
        public SimpleElvenArmoire() : base(0x2D05) => Weight = 1.0;
        public override int DefaultGumpID => 0x4F;
        public override int DefaultDropSound => 0x42;
    }

    [Furniture]
    [Flippable(0xa97, 0xa99, 0xa98, 0xa9a, 0xa9b, 0xa9c)]
    public class FullBookcase : BaseContainer
    {
        [Constructible]
        public FullBookcase() : base(0xA97) => Weight = 1.0;

        public FullBookcase(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0xa9d, 0xa9e)]
    public class EmptyBookcase : BaseContainer
    {
        [Constructible]
        public EmptyBookcase() : base(0xA9D)
        {
        }

        public EmptyBookcase(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version == 0 && Weight == 1.0)
            {
                Weight = -1;
            }
        }
    }

    [Furniture]
    [Flippable(0xa2c, 0xa34)]
    public class Drawer : BaseContainer
    {
        [Constructible]
        public Drawer() : base(0xA2C) => Weight = 1.0;

        public Drawer(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0xa30, 0xa38)]
    public class FancyDrawer : BaseContainer
    {
        [Constructible]
        public FancyDrawer() : base(0xA30) => Weight = 1.0;

        public FancyDrawer(Serial serial) : base(serial)
        {
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

    [Furniture]
    [Flippable(0xa4f, 0xa53)]
    public class Armoire : BaseContainer
    {
        [Constructible]
        public Armoire() : base(0xA4F) => Weight = 1.0;

        public Armoire(Serial serial) : base(serial)
        {
        }

        public override void DisplayTo(Mobile m)
        {
            if (DynamicFurniture.Open(this, m))
            {
                base.DisplayTo(m);
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

            DynamicFurniture.Close(this);
        }
    }

    [Furniture]
    [Flippable(0xa4d, 0xa51)]
    public class FancyArmoire : BaseContainer
    {
        [Constructible]
        public FancyArmoire() : base(0xA4D) => Weight = 1.0;

        public FancyArmoire(Serial serial) : base(serial)
        {
        }

        public override void DisplayTo(Mobile m)
        {
            if (DynamicFurniture.Open(this, m))
            {
                base.DisplayTo(m);
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

            DynamicFurniture.Close(this);
        }
    }

    public static class DynamicFurniture
    {
        private static readonly Dictionary<Container, Timer> m_Table = new();

        public static bool Open(Container c, Mobile m)
        {
            if (m_Table.ContainsKey(c))
            {
                c.SendRemovePacket();
                Close(c);
                c.Delta(ItemDelta.Update);
                c.ProcessDelta();
                return false;
            }

            if (c is Armoire || c is FancyArmoire)
            {
                Timer t = new FurnitureTimer(c, m);
                t.Start();
                m_Table[c] = t;

                c.ItemID = c.ItemID switch
                {
                    0xA4D => 0xA4C,
                    0xA4F => 0xA4E,
                    0xA51 => 0xA50,
                    0xA53 => 0xA52,
                    _     => c.ItemID
                };
            }

            return true;
        }

        public static void Close(Container c)
        {
            if (m_Table.Remove(c, out var t))
            {
                t.Stop();
            }

            if (c is Armoire || c is FancyArmoire)
            {
                c.ItemID = c.ItemID switch
                {
                    0xA4C => 0xA4D,
                    0xA4E => 0xA4F,
                    0xA50 => 0xA51,
                    0xA52 => 0xA53,
                    _     => c.ItemID
                };
            }
        }
    }

    public class FurnitureTimer : Timer
    {
        private readonly Container m_Container;
        private readonly Mobile m_Mobile;

        public FurnitureTimer(Container c, Mobile m) : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5))
        {

            m_Container = c;
            m_Mobile = m;
        }

        protected override void OnTick()
        {
            if (m_Mobile.Map != m_Container.Map || !m_Mobile.InRange(m_Container.GetWorldLocation(), 3))
            {
                DynamicFurniture.Close(m_Container);
            }
        }
    }
}
