using System;

namespace Server.Factions
{
    public class GuardDefinition
    {
        public GuardDefinition(
            Type type, int itemID, int price, int upkeep, int maximum, TextDefinition header,
            TextDefinition label
        )
        {
            Type = type;

            Price = price;
            Upkeep = upkeep;
            Maximum = maximum;
            ItemID = itemID;

            Header = header;
            Label = label;
        }

        public Type Type { get; }

        public int Price { get; }

        public int Upkeep { get; }

        public int Maximum { get; }

        public int ItemID { get; }

        public TextDefinition Header { get; }

        public TextDefinition Label { get; }
    }
}
