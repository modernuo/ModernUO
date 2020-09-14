namespace Server.Misc
{
    public class TreasuresOfTokunoPersistance : Item
    {
        public TreasuresOfTokunoPersistance() : base(1)
        {
            Movable = false;

            if (Instance?.Deleted != false)
            {
                Instance = this;
            }
            else
            {
                base.Delete();
            }
        }

        public TreasuresOfTokunoPersistance(Serial serial) : base(serial) => Instance = this;

        public static TreasuresOfTokunoPersistance Instance { get; private set; }

        public override string DefaultName => "TreasuresOfTokuno Persistance - Internal";

        public static void Initialize()
        {
            if (Instance == null)
            {
                new TreasuresOfTokunoPersistance();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.WriteEncodedInt((int)TreasuresOfTokuno.RewardEra);
            writer.WriteEncodedInt((int)TreasuresOfTokuno.DropEra);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        TreasuresOfTokuno.RewardEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();
                        TreasuresOfTokuno.DropEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public override void Delete()
        {
        }
    }
}
