using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1EC0, 0x1EC3)]
    public partial class PickpocketDip : AddonComponent
    {
        private Timer m_Timer;

        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private double _minSkill;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private double _maxSkill;

        public PickpocketDip(int itemID) : base(itemID)
        {
            MinSkill = -25.0;
            MaxSkill = +25.0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Swinging => m_Timer != null;

        public void UpdateItemID()
        {
            var baseItemID = 0x1EC0 + (ItemID - 0x1EC0) / 3 * 3;

            ItemID = baseItemID + (Swinging ? 1 : 0);
        }

        public void BeginSwing()
        {
            m_Timer?.Stop();

            m_Timer = new InternalTimer(this);
            m_Timer.Start();

            UpdateItemID();
        }

        public void EndSwing()
        {
            m_Timer?.Stop();

            m_Timer = null;

            UpdateItemID();
        }

        public void Use(Mobile from)
        {
            from.Direction = from.GetDirectionTo(GetWorldLocation());

            Effects.PlaySound(GetWorldLocation(), Map, 0x4F);

            if (from.CheckSkill(SkillName.Stealing, MinSkill, MaxSkill))
            {
                SendLocalizedMessageTo(from, 501834); // You successfully avoid disturbing the dip while searching it.
            }
            else
            {
                Effects.PlaySound(GetWorldLocation(), Map, 0x390);

                BeginSwing();
                ProcessDelta();
                SendLocalizedMessageTo(from, 501831); // You carelessly bump the dip and start it swinging.
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 1))
            {
                SendLocalizedMessageTo(from, 501816); // You are too far away to do that.
            }
            else if (Swinging)
            {
                SendLocalizedMessageTo(from, 501815); // You have to wait until it stops swinging.
            }
            else if (from.Skills.Stealing.Base >= MaxSkill)
            {
                // Your ability to steal cannot improve any further by simply practicing on a dummy.
                SendLocalizedMessageTo(from, 501830);
            }
            else if (from.Mounted)
            {
                SendLocalizedMessageTo(from, 501829); // You can't practice on this while on a mount.
            }
            else
            {
                Use(from);
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            UpdateItemID();
        }

        private class InternalTimer : Timer
        {
            private readonly PickpocketDip m_Dip;

            public InternalTimer(PickpocketDip dip) : base(TimeSpan.FromSeconds(3.0))
            {
                m_Dip = dip;
            }

            protected override void OnTick()
            {
                m_Dip.EndSwing();
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PickpocketDipEastAddon : BaseAddon
    {
        [Constructible]
        public PickpocketDipEastAddon()
        {
            AddComponent(new PickpocketDip(0x1EC3), 0, 0, 0);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PickpocketDipEastDeed : BaseAddonDeed
    {
        [Constructible]
        public PickpocketDipEastDeed()
        {
        }

        public override BaseAddon Addon => new PickpocketDipEastAddon();
        public override int LabelNumber => 1044337; // pickpocket dip (east)
    }

    [SerializationGenerator(0, false)]
    public partial class PickpocketDipSouthAddon : BaseAddon
    {
        [Constructible]
        public PickpocketDipSouthAddon()
        {
            AddComponent(new PickpocketDip(0x1EC0), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new PickpocketDipSouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class PickpocketDipSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public PickpocketDipSouthDeed()
        {
        }

        public override BaseAddon Addon => new PickpocketDipSouthAddon();
        public override int LabelNumber => 1044338; // pickpocket dip (south)
    }
}
