using System;
using Server.Network;

namespace Server.Items
{
    [Flippable(0x2A5D, 0x2A61)]
    public class AwesomeDisturbingPortraitComponent : AddonComponent
    {
        private InternalTimer m_Timer;

        public AwesomeDisturbingPortraitComponent() : base(0x2A5D)
        {
            m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(1));
            m_Timer.Start();
        }

        public AwesomeDisturbingPortraitComponent(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074479; // Disturbing portrait
        public bool FacingSouth => ItemID < 0x2A61;

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(Location, from.Location, 2))
            {
                Clock.GetTime(Map, X, Y, out var hours, out int _);

                if (hours < 4 || hours > 20)
                {
                    Effects.PlaySound(Location, Map, 0x569);
                }

                UpdateImage();
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Timer?.Running == true)
            {
                m_Timer.Stop();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Timer = new InternalTimer(this, TimeSpan.Zero);
            m_Timer.Start();
        }

        private void UpdateImage()
        {
            Clock.GetTime(Map, X, Y, out var hours, out int _);

            if (FacingSouth)
            {
                if (hours < 4)
                {
                    ItemID = 0x2A60;
                }
                else if (hours < 6)
                {
                    ItemID = 0x2A5F;
                }
                else if (hours < 8)
                {
                    ItemID = 0x2A5E;
                }
                else if (hours < 16)
                {
                    ItemID = 0x2A5D;
                }
                else if (hours < 18)
                {
                    ItemID = 0x2A5E;
                }
                else if (hours < 20)
                {
                    ItemID = 0x2A5F;
                }
                else
                {
                    ItemID = 0x2A60;
                }
            }
            else
            {
                if (hours < 4)
                {
                    ItemID = 0x2A64;
                }
                else if (hours < 6)
                {
                    ItemID = 0x2A63;
                }
                else if (hours < 8)
                {
                    ItemID = 0x2A62;
                }
                else if (hours < 16)
                {
                    ItemID = 0x2A61;
                }
                else if (hours < 18)
                {
                    ItemID = 0x2A62;
                }
                else if (hours < 20)
                {
                    ItemID = 0x2A63;
                }
                else
                {
                    ItemID = 0x2A64;
                }
            }
        }

        private class InternalTimer : Timer
        {
            private readonly AwesomeDisturbingPortraitComponent m_Component;

            public InternalTimer(AwesomeDisturbingPortraitComponent c, TimeSpan delay) : base(
                delay,
                TimeSpan.FromMinutes(10)
            )
            {
                m_Component = c;
            }

            protected override void OnTick()
            {
                if (m_Component?.Deleted == false)
                {
                    m_Component.UpdateImage();
                }
            }
        }
    }

    public class AwesomeDisturbingPortraitAddon : BaseAddon
    {
        [Constructible]
        public AwesomeDisturbingPortraitAddon()
        {
            AddComponent(new AwesomeDisturbingPortraitComponent(), 0, 0, 0);
        }

        public AwesomeDisturbingPortraitAddon(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new AwesomeDisturbingPortraitDeed();

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class AwesomeDisturbingPortraitDeed : BaseAddonDeed
    {
        [Constructible]
        public AwesomeDisturbingPortraitDeed() => LootType = LootType.Blessed;

        public AwesomeDisturbingPortraitDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new AwesomeDisturbingPortraitAddon();
        public override int LabelNumber => 1074479; // Disturbing portrait

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
