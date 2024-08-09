using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.BulkOrders
{
    public sealed class BOBFilterGump : DynamicGump
    {
        private const int LabelColor = 0x7FFF;

        private static readonly int[,] _materialFilters =
        {
            { 1044067, 1 },  // Blacksmithy
            { 1062226, 3 },  // Iron
            { 1018332, 4 },  // Dull Copper
            { 1018333, 5 },  // Shadow Iron
            { 1018334, 6 },  // Copper
            { 1018335, 7 },  // Bronze
            { 0, 0 },        // --Blank--
            { 1018336, 8 },  // Golden
            { 1018337, 9 },  // Agapite
            { 1018338, 10 }, // Verite
            { 1018339, 11 }, // Valorite
            { 0, 0 },        // --Blank--
            { 1044094, 2 },  // Tailoring
            { 1044286, 12 }, // Cloth
            { 1062235, 13 }, // Leather
            { 1062236, 14 }, // Spined
            { 1062237, 15 }, // Horned
            { 1062238, 16 }  // Barbed
        };

        private static readonly int[,] _typeFilters =
        {
            { 1062229, 0 }, // All
            { 1062224, 1 }, // Small
            { 1062225, 2 }  // Large
        };

        private static readonly int[,] _qualityFilters =
        {
            { 1062229, 0 }, // All
            { 1011542, 1 }, // Normal
            { 1060636, 2 }  // Exceptional
        };

        private static readonly int[,] _amountFilters =
        {
            { 1062229, 0 }, // All
            { 1049706, 1 }, // 10
            { 1016007, 2 }, // 15
            { 1062239, 3 }  // 20
        };

        private static readonly int[][,] _filters =
        {
            _typeFilters,
            _qualityFilters,
            _materialFilters,
            _amountFilters
        };

        private static readonly int[] _xOffsets_Type = [0, 75, 170];
        private static readonly int[] _xOffsets_Quality = [0, 75, 170];
        private static readonly int[] _xOffsets_Amount = [0, 75, 180, 275];
        private static readonly int[] _xOffsets_Material = [0, 105, 210, 305, 390, 485];

        private static readonly int[] _xWidths_Small = [50, 50, 70, 50];
        private static readonly int[] _xWidths_Large = [80, 50, 50, 50, 50, 50];

        private readonly BulkOrderBook _book;
        private readonly PlayerMobile _from;

        public override bool Singleton => true;

        public BOBFilterGump(PlayerMobile from, BulkOrderBook book) : base(12, 24)
        {
            _from = from;
            _book = book;
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var f = _from.UseOwnFilter ? _from.BOBFilter : _book.Filter;

            builder.AddPage();

            builder.AddBackground(10, 10, 600, 439, 5054);

            builder.AddImageTiled(18, 20, 583, 420, 2624);
            builder.AddAlphaRegion(18, 20, 583, 420);

            builder.AddImage(5, 5, 10460);
            builder.AddImage(585, 5, 10460);
            builder.AddImage(5, 424, 10460);
            builder.AddImage(585, 424, 10460);

            builder.AddHtmlLocalized(270, 32, 200, 32, 1062223, LabelColor); // Filter Preference

            builder.AddHtmlLocalized(26, 64, 120, 32, 1062228, LabelColor); // Bulk Order Type
            AddFilterList(ref builder, 25, 96, _xOffsets_Type, 40, _typeFilters, _xWidths_Small, f.Type, 0);

            builder.AddHtmlLocalized(320, 64, 50, 32, 1062215, LabelColor); // Quality
            AddFilterList(ref builder, 320, 96, _xOffsets_Quality, 40, _qualityFilters, _xWidths_Small, f.Quality, 1);

            builder.AddHtmlLocalized(26, 160, 120, 32, 1062232, LabelColor); // Material Type
            AddFilterList(ref builder, 25, 192, _xOffsets_Material, 40, _materialFilters, _xWidths_Large, f.Material, 2);

            builder.AddHtmlLocalized(26, 320, 120, 32, 1062217, LabelColor); // Amount
            AddFilterList(ref builder, 25, 352, _xOffsets_Amount, 40, _amountFilters, _xWidths_Small, f.Quantity, 3);

            builder.AddHtmlLocalized(75, 416, 120, 32, 1062477, _from.UseOwnFilter ? LabelColor : 16927); // Set Book Filter
            builder.AddButton(40, 416, 4005, 4007, 1);

            builder.AddHtmlLocalized(235, 416, 120, 32, 1062478, _from.UseOwnFilter ? 16927 : LabelColor); // Set Your Filter
            builder.AddButton(200, 416, 4005, 4007, 2);

            builder.AddHtmlLocalized(405, 416, 120, 32, 1062231, LabelColor); // Clear Filter
            builder.AddButton(370, 416, 4005, 4007, 3);

            builder.AddHtmlLocalized(540, 416, 50, 32, 1011046, LabelColor); // APPLY
            builder.AddButton(505, 416, 4017, 4018, 0);
        }

        private void AddFilterList(ref DynamicGumpBuilder builder,
            int x, int y, int[] xOffsets, int yOffset, int[,] filters, int[] xWidths, int filterValue,
            int filterIndex
        )
        {
            for (var i = 0; i < filters.GetLength(0); ++i)
            {
                var number = filters[i, 0];

                if (number == 0)
                {
                    continue;
                }

                var isSelected = filters[i, 1] == filterValue ||
                                 i % xOffsets.Length == 0 && filterValue == 0;

                builder.AddHtmlLocalized(
                    x + 35 + xOffsets[i % xOffsets.Length],
                    y + i / xOffsets.Length * yOffset,
                    xWidths[i % xOffsets.Length],
                    32,
                    number,
                    isSelected ? 16927 : LabelColor
                );

                builder.AddButton(
                    x + xOffsets[i % xOffsets.Length],
                    y + i / xOffsets.Length * yOffset,
                    4005,
                    4007,
                    4 + filterIndex + i * 4
                );
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var f = _from.UseOwnFilter ? _from.BOBFilter : _book.Filter;

            var index = info.ButtonID;

            switch (index)
            {
                case 0: // Apply
                    {
                        _from.SendGump(new BOBGump(_from, _book));

                        break;
                    }
                case 1: // Set Book Filter
                    {
                        _from.UseOwnFilter = false;
                        _from.SendGump(new BOBFilterGump(_from, _book));

                        break;
                    }
                case 2: // Set Your Filter
                    {
                        _from.UseOwnFilter = true;
                        _from.SendGump(new BOBFilterGump(_from, _book));

                        break;
                    }
                case 3: // Clear Filter
                    {
                        f.Clear();
                        _from.SendGump(new BOBFilterGump(_from, _book));

                        break;
                    }
                default:
                    {
                        index -= 4;

                        var type = index % 4;
                        index /= 4;

                        if (type >= 0 && type < _filters.Length)
                        {
                            var filters = _filters[type];

                            if (index >= 0 && index < filters.GetLength(0))
                            {
                                if (filters[index, 0] == 0)
                                {
                                    break;
                                }

                                switch (type)
                                {
                                    case 0:
                                        f.Type = filters[index, 1];
                                        break;
                                    case 1:
                                        f.Quality = filters[index, 1];
                                        break;
                                    case 2:
                                        f.Material = filters[index, 1];
                                        break;
                                    case 3:
                                        f.Quantity = filters[index, 1];
                                        break;
                                }

                                _from.SendGump(new BOBFilterGump(_from, _book));
                            }
                        }

                        break;
                    }
            }
        }
    }
}
