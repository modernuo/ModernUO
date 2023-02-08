namespace Server.Items
{
    public class GamblingStone : Item
    {
        private int m_GamblePot = 2500;

        [Constructible]
        public GamblingStone()
            : base(0xED4)
        {
            Movable = false;
            Hue = 0x56;
        }

        public GamblingStone(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GamblePot
        {
            get => m_GamblePot;
            set
            {
                m_GamblePot = value;
                InvalidateProperties();
            }
        }

        public override string DefaultName => "a gambling stone";

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add($"Jackpot: {m_GamblePot}gp");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, $"Jackpot: {m_GamblePot}gp");
        }

        public override void OnDoubleClick(Mobile from)
        {
            var pack = from.Backpack;

            if (pack?.ConsumeTotal(typeof(Gold), 250) == true)
            {
                m_GamblePot += 150;
                InvalidateProperties();

                var roll = Utility.Random(1200);

                if (roll == 0) // Jackpot
                {
                    var maxCheck = 1000000;

                    from.SendMessage(0x35, $"You win the {m_GamblePot}gp jackpot!");

                    while (m_GamblePot > maxCheck)
                    {
                        from.AddToBackpack(new BankCheck(maxCheck));

                        m_GamblePot -= maxCheck;
                    }

                    from.AddToBackpack(new BankCheck(m_GamblePot));

                    m_GamblePot = 2500;
                }
                else if (roll <= 20) // Chance for a regbag
                {
                    from.SendMessage(0x35, "You win a bag of reagents!");
                    from.AddToBackpack(new BagOfReagents());
                }
                else if (roll <= 40) // Chance for gold
                {
                    from.SendMessage(0x35, "You win 1500gp!");
                    from.AddToBackpack(new BankCheck(1500));
                }
                else if (roll <= 100) // Another chance for gold
                {
                    from.SendMessage(0x35, "You win 1000gp!");
                    from.AddToBackpack(new BankCheck(1000));
                }
                else // Loser!
                {
                    from.SendMessage(0x22, "You lose!");
                }
            }
            else
            {
                from.SendMessage(0x22, "You need at least 250gp in your backpack to use this.");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_GamblePot);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_GamblePot = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}
