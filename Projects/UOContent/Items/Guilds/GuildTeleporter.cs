using Server.Guilds;
using Server.Multis;

namespace Server.Items
{
    public class GuildTeleporter : Item
    {
        private Item m_Stone;

        [Constructible]
        public GuildTeleporter(Item stone = null) : base(0x1869)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;

            m_Stone = stone;
        }

        public GuildTeleporter(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041054; // guildstone teleporter

        public override bool DisplayLootType => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Stone);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Stone = reader.ReadEntity<Item>();

                        break;
                    }
            }

            if (Weight == 0.0)
            {
                Weight = 1.0;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Guild.NewGuildSystem)
            {
                return;
            }

            var stone = m_Stone as Guildstone;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (stone?.Deleted != false || stone.Guild?.Teleporter != this)
            {
                from.SendLocalizedMessage(501197); // This teleporting object can not determine what guildstone to teleport
            }
            else
            {
                var house = BaseHouse.FindHouseAt(from);

                if (house == null)
                {
                    from.SendLocalizedMessage(501138); // You can only place a guildstone in a house.
                }
                else if (!house.IsOwner(from))
                {
                    from.SendLocalizedMessage(501141); // You can only place a guildstone in a house you own!
                }
                else if (house.FindGuildstone() != null)
                {
                    from.SendLocalizedMessage(501142); // Only one guildstone may reside in a given house.
                }
                else
                {
                    m_Stone.MoveToWorld(from.Location, from.Map);
                    Delete();
                    stone.Guild.Teleporter = null;
                }
            }
        }
    }
}
