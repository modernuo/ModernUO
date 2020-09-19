using System;

namespace Server.Items
{
    [Flippable(0x1070, 0x1074)]
    public class TrainingDummy : AddonComponent
    {
        private Timer m_Timer;

        [Constructible]
        public TrainingDummy(int itemID = 0x1074) : base(itemID)
        {
            MinSkill = -25.0;
            MaxSkill = +25.0;
        }

        public TrainingDummy(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MinSkill { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MaxSkill { get; set; }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(MinSkill);
            writer.Write(MaxSkill);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        MinSkill = reader.ReadDouble();
                        MaxSkill = reader.ReadDouble();

                        if (MinSkill == 0.0 && MaxSkill == 30.0)
                        {
                            MinSkill = -25.0;
                            MaxSkill = +25.0;
                        }

                        break;
                    }
            }

            UpdateItemID();
        }

        private class InternalTimer : Timer
        {
            private readonly TrainingDummy m_Dummy;
            private bool m_Delay = true;

            public InternalTimer(TrainingDummy dummy) : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(2.75))
            {
                m_Dummy = dummy;
                Priority = TimerPriority.FiftyMS;
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

    public class TrainingDummyEastAddon : BaseAddon
    {
        [Constructible]
        public TrainingDummyEastAddon()
        {
            AddComponent(new TrainingDummy(), 0, 0, 0);
        }

        public TrainingDummyEastAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new TrainingDummyEastDeed();

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

    public class TrainingDummyEastDeed : BaseAddonDeed
    {
        [Constructible]
        public TrainingDummyEastDeed()
        {
        }

        public TrainingDummyEastDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new TrainingDummyEastAddon();
        public override int LabelNumber => 1044335; // training dummy (east)

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

    public class TrainingDummySouthAddon : BaseAddon
    {
        [Constructible]
        public TrainingDummySouthAddon()
        {
            AddComponent(new TrainingDummy(0x1070), 0, 0, 0);
        }

        public TrainingDummySouthAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new TrainingDummySouthDeed();

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

    public class TrainingDummySouthDeed : BaseAddonDeed
    {
        [Constructible]
        public TrainingDummySouthDeed()
        {
        }

        public TrainingDummySouthDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new TrainingDummySouthAddon();
        public override int LabelNumber => 1044336; // training dummy (south)

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
