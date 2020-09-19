using Server.Mobiles;

namespace Server.Factions
{
    public class TownStone : BaseSystemController
    {
        private Town m_Town;

        [Constructible]
        public TownStone(Town town = null) : base(0xEDE)
        {
            Movable = false;
            Town = town;
        }

        public TownStone(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Town Town
        {
            get => m_Town;
            set
            {
                m_Town = value;

                AssignName(m_Town?.Definition.TownStoneName);
            }
        }

        public override string DefaultName => "faction town stone";

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Town == null)
            {
                return;
            }

            var faction = Faction.Find(from);

            if (faction == null && from.AccessLevel < AccessLevel.GameMaster)
            {
                return; // TODO: Message?
            }

            if (m_Town.Owner == null || from.AccessLevel < AccessLevel.GameMaster && faction != m_Town.Owner)
            {
                from.SendLocalizedMessage(1010332); // Your faction does not control this town
            }
            else if (!m_Town.Owner.IsCommander(from))
            {
                from.SendLocalizedMessage(1005242); // Only faction Leaders can use townstones
            }
            else if (FactionGump.Exists(from))
            {
                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
            }
            else if (from is PlayerMobile mobile)
            {
                mobile.SendGump(new TownStoneGump(mobile, m_Town.Owner, m_Town));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            Town.WriteReference(writer, m_Town);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Town = Town.ReadReference(reader);
                        break;
                    }
            }
        }
    }
}
