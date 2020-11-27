using System;

namespace Server.Factions
{
    public class VendorDefinition
    {
        public VendorDefinition(
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

        public static VendorDefinition[] Definitions { get; } =
        {
            new(
                typeof(FactionBottleVendor),
                0xF0E,
                5000,
                1000,
                10,
                new TextDefinition(1011549, "POTION BOTTLE VENDOR"),
                new TextDefinition(1011544, "Buy Potion Bottle Vendor")
            ),
            new(
                typeof(FactionBoardVendor),
                0x1BD7,
                3000,
                500,
                10,
                new TextDefinition(1011552, "WOOD VENDOR"),
                new TextDefinition(1011545, "Buy Wooden Board Vendor")
            ),
            new(
                typeof(FactionOreVendor),
                0x19B8,
                3000,
                500,
                10,
                new TextDefinition(1011553, "IRON ORE VENDOR"),
                new TextDefinition(1011546, "Buy Iron Ore Vendor")
            ),
            new(
                typeof(FactionReagentVendor),
                0xF86,
                5000,
                1000,
                10,
                new TextDefinition(1011554, "REAGENT VENDOR"),
                new TextDefinition(1011547, "Buy Reagent Vendor")
            ),
            new(
                typeof(FactionHorseVendor),
                0x20DD,
                5000,
                1000,
                1,
                new TextDefinition(1011556, "HORSE BREEDER"),
                new TextDefinition(1011555, "Buy Horse Breeder")
            )
        };
    }
}
