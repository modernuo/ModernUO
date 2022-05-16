using Server.Items;

namespace Server.Mobiles
{
    public class SeekerOfAdventure : BaseEscortable
    {
        private static readonly string[] m_Dungeons =
        {
            "Covetous", "Deceit", "Despise",
            "Destard", "Hythloth", "Shame", // Old Code for Pre-ML shards.
            "Wrong"
        };

        private static readonly string[] m_MLDestinations =
        {
            "Cove", "Serpent's Hold", "Jhelom", // ML List
            "Nujel'm"
        };

        [Constructible]
        public SeekerOfAdventure() => Title = "the seeker of adventure";

        public SeekerOfAdventure(Serial serial) : base(serial)
        {
        }

        public override bool ClickTitle => false; // Do not display 'the seeker of adventure' when single-clicking

        public override string[] GetPossibleDestinations() => Core.ML ? m_MLDestinations : m_Dungeons;

        private static int GetRandomHue()
        {
            return Utility.Random(6) switch
            {
                0 => 0,
                1 => Utility.RandomBlueHue(),
                2 => Utility.RandomGreenHue(),
                3 => Utility.RandomRedHue(),
                4 => Utility.RandomYellowHue(),
                5 => Utility.RandomNeutralHue(),
                _ => 0
            };
        }

        public override void InitOutfit()
        {
            if (Female)
            {
                AddItem(new FancyDress(GetRandomHue()));
            }
            else
            {
                AddItem(new FancyShirt(GetRandomHue()));
            }

            var lowHue = GetRandomHue();

            AddItem(new ShortPants(lowHue));

            if (Female)
            {
                AddItem(new ThighBoots(lowHue));
            }
            else
            {
                AddItem(new Boots(lowHue));
            }

            if (!Female)
            {
                AddItem(new BodySash(lowHue));
            }

            AddItem(new Cloak(GetRandomHue()));

            AddItem(new Longsword());

            Utility.AssignRandomHair(this);

            PackGold(100, 150);
        }

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
