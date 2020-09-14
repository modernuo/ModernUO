using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public class HearthOfHomeFire : BaseAddon
    {
        [Constructible]
        public HearthOfHomeFire(bool east)
        {
            if (east)
            {
                AddLightComponent(new AddonComponent(0x2352), 0, 0, 0);
                AddLightComponent(new AddonComponent(0x2358), 0, -1, 0);
            }
            else
            {
                AddLightComponent(new AddonComponent(0x2360), 0, 0, 0);
                AddLightComponent(new AddonComponent(0x2366), -1, 0, 0);
            }
        }

        public HearthOfHomeFire(Serial serial) : base(serial)
        {
        }

        public override BaseAddonDeed Deed => new HearthOfHomeFireDeed();

        private void AddLightComponent(AddonComponent component, int x, int y, int z)
        {
            component.Light = LightType.Circle150;

            AddComponent(component, x, y, z);
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
        }
    }

    public class HearthOfHomeFireDeed : BaseAddonDeed
    {
        private bool m_East;

        [Constructible]
        public HearthOfHomeFireDeed() => LootType = LootType.Blessed;

        public HearthOfHomeFireDeed(Serial serial) : base(serial)
        {
        }

        public override BaseAddon Addon => new HearthOfHomeFire(m_East);

        public override int LabelNumber => 1062919; // Hearth of the Home Fire

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.CloseGump<InternalGump>();
                from.SendGump(new InternalGump(this));
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        private void SendTarget(Mobile m)
        {
            base.OnDoubleClick(m);
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
        }

        private class InternalGump : Gump
        {
            private readonly HearthOfHomeFireDeed m_Deed;

            public InternalGump(HearthOfHomeFireDeed deed) : base(150, 50)
            {
                m_Deed = deed;

                AddBackground(0, 0, 350, 250, 0xA28);

                AddItem(90, 52, 0x2367);
                AddItem(112, 35, 0x2360);
                AddButton(70, 35, 0x868, 0x869, 1); // South

                AddItem(220, 35, 0x2352);
                AddItem(242, 52, 0x2358);
                AddButton(185, 35, 0x868, 0x869, 2); // East
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_Deed.Deleted || info.ButtonID == 0)
                {
                    return;
                }

                m_Deed.m_East = info.ButtonID != 1;
                m_Deed.SendTarget(sender.Mobile);
            }
        }
    }
}
