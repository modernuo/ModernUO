using Server.Mobiles;

namespace Server.Factions
{
    public class FactionStone : BaseSystemController
    {
        private Faction m_Faction;

        [Constructible]
        public FactionStone(Faction faction = null) : base(0xEDC)
        {
            Movable = false;
            Faction = faction;
        }

        public FactionStone(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Faction Faction
        {
            get => m_Faction;
            set
            {
                m_Faction = value;

                AssignName(m_Faction?.Definition.FactionStoneName);
            }
        }

        public override string DefaultName => "faction stone";

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
            else if (from is PlayerMobile mobile)
            {
                var existingFaction = Faction.Find(mobile);

                if (existingFaction == m_Faction || mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    var pl = PlayerState.Find(mobile);

                    if (pl?.IsLeaving == true)
                    {
                        // You cannot use the faction stone until you have finished quitting your current faction
                        mobile.SendLocalizedMessage(1005051);
                    }
                    else
                    {
                        mobile.SendGump(new FactionStoneGump(mobile, m_Faction));
                    }
                }
                else if (existingFaction != null)
                {
                    // TODO: Validate
                    mobile.SendLocalizedMessage(1005053); // This is not your faction stone!
                }
                else
                {
                    mobile.SendGump(new JoinStoneGump(mobile, m_Faction));
                }
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
