using Server.Gumps;
using Server.Network;

namespace Server.Engines.BulkOrders
{
    public class SmallBODGump : DynamicGump
    {
        private readonly SmallBOD _deed;

        public override bool Singleton => true;

        public SmallBODGump(SmallBOD deed) : base(25, 25) => _deed = deed;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(50, 10, 455, 260, 5054);
            builder.AddImageTiled(58, 20, 438, 241, 2624);
            builder.AddAlphaRegion(58, 20, 438, 241);

            builder.AddImage(45, 5, 10460);
            builder.AddImage(480, 5, 10460);
            builder.AddImage(45, 245, 10460);
            builder.AddImage(480, 245, 10460);

            builder.AddHtmlLocalized(225, 25, 120, 20, 1045133, 0x7FFF); // A bulk order

            builder.AddHtmlLocalized(75, 48, 250, 20, 1045138, 0x7FFF); // Amount to make:
            builder.AddLabel(275, 48, 1152, _deed.AmountMax.ToString());

            builder.AddHtmlLocalized(275, 76, 200, 20, 1045153, 0x7FFF); // Amount finished:
            builder.AddHtmlLocalized(75, 72, 120, 20, 1045136, 0x7FFF);  // Item requested:

            builder.AddItem(410, 72, _deed.Graphic);

            builder.AddHtmlLocalized(75, 96, 210, 20, _deed.Number, 0x7FFF);
            builder.AddLabel(275, 96, 0x480, _deed.AmountCur.ToString());

            if (_deed.RequireExceptional || _deed.Material != BulkMaterialType.None)
            {
                builder.AddHtmlLocalized(75, 120, 200, 20, 1045140, 0x7FFF); // Special requirements to meet:
            }

            if (_deed.RequireExceptional)
            {
                builder.AddHtmlLocalized(75, 144, 300, 20, 1045141, 0x7FFF); // All items must be exceptional.
            }

            if (_deed.Material != BulkMaterialType.None)
            {
                builder.AddHtmlLocalized(
                    75,
                    _deed.RequireExceptional ? 168 : 144,
                    300,
                    20,
                    GetMaterialNumberFor(_deed.Material), // All items must be made with x material.
                    0x7FFF
                );
            }

            builder.AddButton(125, 192, 4005, 4007, 2);
            builder.AddHtmlLocalized(160, 192, 300, 20, 1045154, 0x7FFF); // Combine this deed with the item requested.

            builder.AddButton(125, 216, 4005, 4007, 1);
            builder.AddHtmlLocalized(160, 216, 120, 20, 1011441, 0x7FFF); // EXIT
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
                from.SendGump(new SmallBODGump(_deed));
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
