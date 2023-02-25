namespace Server.Engines.ConPVP
{
    public class TournamentBracketItem : Item
    {
        [Constructible]
        public TournamentBracketItem() : base(3774) => Movable = false;

        public TournamentBracketItem(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TournamentController Tournament { get; set; }

        public override string DefaultName => "tournament bracket";

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
            }
            else
            {
                var tourney = Tournament?.Tournament;

                if (tourney != null)
                {
                    from.CloseGump<TournamentBracketGump>();
                    from.SendGump(new TournamentBracketGump(from, tourney, TourneyBracketGumpType.Index));
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(Tournament);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Tournament = reader.ReadEntity<TournamentController>();
                        break;
                    }
            }
        }
    }
}
