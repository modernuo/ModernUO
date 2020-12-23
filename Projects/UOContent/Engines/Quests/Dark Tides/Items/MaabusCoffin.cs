using System;
using Server.Items;

namespace Server.Engines.Quests.Necro
{
    public class MaabusCoffin : BaseAddon
    {
        [Constructible]
        public MaabusCoffin()
        {
            AddComponent(new MaabusCoffinComponent(0x1C2B, 0x1C2B), -1, -1, 0);

            AddComponent(new MaabusCoffinComponent(0x1D16, 0x1C2C), 0, -1, 0);
            AddComponent(new MaabusCoffinComponent(0x1D17, 0x1C2D), 1, -1, 0);
            AddComponent(new MaabusCoffinComponent(0x1D51, 0x1C2E), 2, -1, 0);

            AddComponent(new MaabusCoffinComponent(0x1D4E, 0x1C2A), 0, 0, 0);
            AddComponent(new MaabusCoffinComponent(0x1D4D, 0x1C29), 1, 0, 0);
            AddComponent(new MaabusCoffinComponent(0x1D4C, 0x1C28), 2, 0, 0);
        }

        public MaabusCoffin(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Maabus Maabus { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D SpawnLocation { get; set; }

        public void Awake(Mobile caller)
        {
            if (Maabus != null || SpawnLocation == Point3D.Zero)
            {
                return;
            }

            foreach (var c in Components)
            {
                (c as MaabusCoffinComponent)?.TurnToEmpty();
            }

            Maabus = new Maabus { Location = SpawnLocation, Map = Map };
            Maabus.Direction = Maabus.GetDirectionTo(caller);

            Timer.DelayCall(TimeSpan.FromSeconds(7.5), BeginSleep);
        }

        public void BeginSleep()
        {
            if (Maabus == null)
            {
                return;
            }

            Effects.PlaySound(Maabus.Location, Maabus.Map, 0x48E);

            Timer.DelayCall(TimeSpan.FromSeconds(2.5), Sleep);
        }

        public void Sleep()
        {
            if (Maabus == null)
            {
                return;
            }

            Effects.SendLocationParticles(
                EffectItem.Create(Maabus.Location, Maabus.Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                0x7E7
            );
            Effects.PlaySound(Maabus.Location, Maabus.Map, 0x1FE);

            Maabus.Delete();
            Maabus = null;

            foreach (var c in Components)
            {
                (c as MaabusCoffinComponent)?.TurnToFull();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Maabus);
            writer.Write(SpawnLocation);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Maabus = reader.ReadEntity<Maabus>();
            SpawnLocation = reader.ReadPoint3D();

            Sleep();
        }
    }

    public class MaabusCoffinComponent : AddonComponent
    {
        private int m_EmptyItemID;
        private int m_FullItemID;

        public MaabusCoffinComponent(int itemID) : this(itemID, itemID)
        {
        }

        public MaabusCoffinComponent(int fullItemID, int emptyItemID) : base(fullItemID)
        {
            m_FullItemID = fullItemID;
            m_EmptyItemID = emptyItemID;
        }

        public MaabusCoffinComponent(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D SpawnLocation
        {
            get => Addon is MaabusCoffin coffin ? coffin.SpawnLocation : Point3D.Zero;
            set
            {
                if (Addon is MaabusCoffin coffin)
                {
                    coffin.SpawnLocation = value;
                }
            }
        }

        public void TurnToEmpty()
        {
            ItemID = m_EmptyItemID;
        }

        public void TurnToFull()
        {
            ItemID = m_FullItemID;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_FullItemID);
            writer.Write(m_EmptyItemID);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_FullItemID = reader.ReadInt();
            m_EmptyItemID = reader.ReadInt();
        }
    }
}
