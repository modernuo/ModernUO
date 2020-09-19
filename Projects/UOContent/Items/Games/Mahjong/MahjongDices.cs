namespace Server.Engines.Mahjong
{
    public class MahjongDices
    {
        public MahjongDices(MahjongGame game)
        {
            Game = game;
            First = Utility.Random(1, 6);
            Second = Utility.Random(1, 6);
        }

        public MahjongDices(MahjongGame game, IGenericReader reader)
        {
            Game = game;

            var version = reader.ReadInt();

            First = reader.ReadInt();
            Second = reader.ReadInt();
        }

        public MahjongGame Game { get; }

        public int First { get; private set; }

        public int Second { get; private set; }

        public void RollDices(Mobile from)
        {
            First = Utility.Random(1, 6);
            Second = Utility.Random(1, 6);

            Game.Players.SendGeneralPacket(true, true);

            if (from != null)
            {
                Game.Players.SendLocalizedMessage(
                    1062695,
                    $"{from.Name}\t{First}\t{Second}"
                ); // ~1_name~ rolls the dice and gets a ~2_number~ and a ~3_number~!
            }
        }

        public void Save(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(First);
            writer.Write(Second);
        }
    }
}
