using Server.Mobiles;

namespace Server.Factions
{
    public class JoinStone : BaseSystemController
    {
        private Faction m_Faction;

        [Constructible]
        public JoinStone(Faction faction = null) : base(0xEDC)
        {
            Movable = false;
            Faction = faction;
        }

        public JoinStone(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Faction Faction
        {
            get => m_Faction;
            set
            {
                m_Faction = value;

                Hue = m_Faction?.Definition.HueJoin ?? 0;
                AssignName(m_Faction?.Definition.SignupName);
            }
        }

        public override string DefaultName => "faction signup stone";

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Faction == null)
            {
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (FactionGump.Exists(from))
            {
                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
            }
            else if (Faction.Find(from) == null && from is PlayerMobile mobile)
            {
                mobile.SendGump(new JoinStoneGump(mobile, m_Faction));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            Faction.WriteReference(writer, m_Faction);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Faction = Faction.ReadReference(reader);
                        break;
                    }
            }
        }
    }
}
