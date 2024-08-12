using Server.Gumps;
using Server.Network;

namespace Server.Engines.BulkOrders
{
    public class LargeBODGump : DynamicGump
    {
        private readonly LargeBOD _deed;

        public override bool Singleton => true;

        public LargeBODGump(LargeBOD deed) : base(25, 25) => _deed = deed;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var entries = _deed.Entries;

            builder.AddPage();

            builder.AddBackground(50, 10, 455, 236 + entries.Length * 24, 5054);

            builder.AddImageTiled(58, 20, 438, 217 + entries.Length * 24, 2624);
            builder.AddAlphaRegion(58, 20, 438, 217 + entries.Length * 24);

            builder.AddImage(45, 5, 10460);
            builder.AddImage(480, 5, 10460);
            builder.AddImage(45, 221 + entries.Length * 24, 10460);
            builder.AddImage(480, 221 + entries.Length * 24, 10460);

            builder.AddHtmlLocalized(225, 25, 120, 20, 1045134, 0x7FFF); // A large bulk order

            builder.AddHtmlLocalized(75, 48, 250, 20, 1045138, 0x7FFF); // Amount to make:
            builder.AddLabel(275, 48, 1152, _deed.AmountMax.ToString());

            builder.AddHtmlLocalized(75, 72, 120, 20, 1045137, 0x7FFF);  // Items requested:
            builder.AddHtmlLocalized(275, 76, 200, 20, 1045153, 0x7FFF); // Amount finished:

            var y = 96;

            for (var i = 0; i < entries.Length; ++i)
            {
                var entry = entries[i];
                var details = entry.Details;

                builder.AddHtmlLocalized(75, y, 210, 20, details.Number, 0x7FFF);
                builder.AddLabel(275, y, 0x480, entry.Amount.ToString());

                y += 24;
            }

            if (_deed.RequireExceptional || _deed.Material != BulkMaterialType.None)
            {
                builder.AddHtmlLocalized(75, y, 200, 20, 1045140, 0x7FFF); // Special requirements to meet:
                y += 24;
            }

            if (_deed.RequireExceptional)
            {
                builder.AddHtmlLocalized(75, y, 300, 20, 1045141, 0x7FFF); // All items must be exceptional.
                y += 24;
            }

            if (_deed.Material != BulkMaterialType.None)
            {
                builder.AddHtmlLocalized(
                    75,
                    y,
                    300,
                    20,
                    GetMaterialNumberFor(_deed.Material), // All items must be made with x material.
                    0x7FFF
                );
            }

            builder.AddButton(125, 168 + entries.Length * 24, 4005, 4007, 2);

            builder.AddHtmlLocalized(
                160,
                168 + entries.Length * 24,
                300,
                20,
                1045155, // Combine this deed with another deed.
                0x7FFF
            );

            builder.AddButton(125, 192 + entries.Length * 24, 4005, 4007, 1);
            builder.AddHtmlLocalized(160, 192 + entries.Length * 24, 120, 20, 1011441, 0x7FFF); // EXIT
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (_deed.Deleted || !_deed.IsChildOf(from.Backpack))
            {
                return;
            }

            if (info.ButtonID == 2) // Combine
            {
                sender.SendGump(new LargeBODGump(_deed));
                _deed.BeginCombine(from);
            }
        }

        public static int GetMaterialNumberFor(BulkMaterialType material)
        {
            if (material >= BulkMaterialType.DullCopper && material <= BulkMaterialType.Valorite)
            {
                return 1045142 + (material - BulkMaterialType.DullCopper);
            }

            if (material >= BulkMaterialType.Spined && material <= BulkMaterialType.Barbed)
            {
                return 1049348 + (material - BulkMaterialType.Spined);
            }

            return 0;
        }
    }
}
