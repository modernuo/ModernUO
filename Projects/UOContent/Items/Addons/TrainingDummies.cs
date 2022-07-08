using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1070, 0x1074)]
    public partial class TrainingDummy : AddonComponent
    {
        private Timer m_Timer;

        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private double _minSkill;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private double _maxSkill;

        [Constructible]
        public TrainingDummy(int itemID = 0x1074) : base(itemID)
        {
            MinSkill = -25.0;
            MaxSkill = +25.0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Swinging => m_Timer != null;

        public void UpdateItemID()
        {
            var baseItemID = ItemID / 2 * 2;

            ItemID = baseItemID + (Swinging ? 1 : 0);
        }

        public void BeginSwing()
        {
            m_Timer?.Stop();

            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        public void EndSwing()
        {
            m_Timer?.Stop();

            m_Timer = null;

            UpdateItemID();
        }

        public void OnHit()
        {
            UpdateItemID();
            Effects.PlaySound(GetWorldLocation(), Map, Utility.RandomList(0x3A4, 0x3A6, 0x3A9, 0x3AE, 0x3B4, 0x3B6));
        }

        public void Use(Mobile from, BaseWeapon weapon)
        {
            BeginSwing();

            from.Direction = from.GetDirectionTo(GetWorldLocation());
            weapon.PlaySwingAnimation(from);

            from.CheckSkill(weapon.Skill, MinSkill, MaxSkill);
        }

        public override void OnDoubleClick(Mobile from)
        {
            var weapon = from.Weapon as BaseWeapon;

            if (weapon is BaseRanged)
            {
                SendLocalizedMessageTo(from, 501822); // You can't practice ranged weapons on this.
            }
            else if (weapon == null || !from.InRange(GetWorldLocation(), weapon.MaxRange))
            {
                SendLocalizedMessageTo(from, 501816); // You are too far away to do that.
            }
            else if (Swinging)
            {
                SendLocalizedMessageTo(from, 501815); // You have to wait until it stops swinging.
            }
            else if (from.Skills[weapon.Skill].Base >= MaxSkill)
            {
                SendLocalizedMessageTo(
                    from,
                    501828
                ); // Your skill cannot improve any further by simply practicing with a dummy.
            }
            else if (from.Mounted)
            {
                SendLocalizedMessageTo(from, 501829); // You can't practice on this while on a mount.
            }
            else
            {
                Use(from, weapon);
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            UpdateItemID();
        }

        private class InternalTimer : Timer
        {
            private readonly TrainingDummy m_Dummy;
            private bool m_Delay = true;

            public InternalTimer(TrainingDummy dummy) : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(2.75))
            {
                m_Dummy = dummy;
            }

            protected override void OnTick()
            {
                if (m_Delay)
                {
                    m_Dummy.OnHit();
                }
                else
                {
                    m_Dummy.EndSwing();
                }

                m_Delay = !m_Delay;
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class TrainingDummyEastAddon : BaseAddon
    {
        [Constructible]
        public TrainingDummyEastAddon()
        {
            AddComponent(new TrainingDummy(), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new TrainingDummyEastDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class TrainingDummyEastDeed : BaseAddonDeed
    {
        [Constructible]
        public TrainingDummyEastDeed()
        {
        }

        public override BaseAddon Addon => new TrainingDummyEastAddon();
        public override int LabelNumber => 1044335; // training dummy (east)
    }

    [SerializationGenerator(0, false)]
    public partial class TrainingDummySouthAddon : BaseAddon
    {
        [Constructible]
        public TrainingDummySouthAddon()
        {
            AddComponent(new TrainingDummy(0x1070), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new TrainingDummySouthDeed();
    }

    [SerializationGenerator(0, false)]
    public partial class TrainingDummySouthDeed : BaseAddonDeed
    {
        [Constructible]
        public TrainingDummySouthDeed()
        {
        }

        public override BaseAddon Addon => new TrainingDummySouthAddon();
        public override int LabelNumber => 1044336; // training dummy (south)
    }
}
