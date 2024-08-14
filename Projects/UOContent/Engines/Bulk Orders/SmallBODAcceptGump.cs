using Server.Gumps;
using Server.Network;

namespace Server.Engines.BulkOrders
{
    public class SmallBODAcceptGump : DynamicGump
    {
        private readonly SmallBOD _deed;

        public override bool Singleton => true;

        public SmallBODAcceptGump(SmallBOD deed) : base(50, 50) => _deed = deed;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(25, 10, 430, 264, 5054);

            builder.AddImageTiled(33, 20, 413, 245, 2624);
            builder.AddAlphaRegion(33, 20, 413, 245);

            builder.AddImage(20, 5, 10460);
            builder.AddImage(430, 5, 10460);
            builder.AddImage(20, 249, 10460);
            builder.AddImage(430, 249, 10460);

            builder.AddHtmlLocalized(190, 25, 120, 20, 1045133, 0x7FFF); // A bulk order
            builder.AddHtmlLocalized(40, 48, 350, 20, 1045135, 0x7FFF); // Ah!  Thanks for the goods!  Would you help me out?

            builder.AddHtmlLocalized(40, 72, 210, 20, 1045138, 0x7FFF); // Amount to make:
            builder.AddLabel(250, 72, 1152, _deed.AmountMax.ToString());

            builder.AddHtmlLocalized(40, 96, 120, 20, 1045136, 0x7FFF); // Item requested:
            builder.AddItem(385, 96, _deed.Graphic);
            builder.AddHtmlLocalized(40, 120, 210, 20, _deed.Number, 0x7FFF);

            if (_deed.RequireExceptional || _deed.Material != BulkMaterialType.None)
            {
                builder.AddHtmlLocalized(40, 144, 210, 20, 1045140, 0x7FFF); // Special requirements to meet:

                if (_deed.RequireExceptional)
                {
                    builder.AddHtmlLocalized(40, 168, 350, 20, 1045141, 0x7FFF); // All items must be exceptional.
                }

                if (_deed.Material != BulkMaterialType.None)
                {
                    builder.AddHtmlLocalized(
                        40,
                        _deed.RequireExceptional ? 192 : 168,
                        350,
                        20,
                        GetMaterialNumberFor(_deed.Material), // All items must be made with x material.
                        0x7FFF
                    );
                }
            }

            builder.AddHtmlLocalized(40, 216, 350, 20, 1045139, 0x7FFF); // Do you want to accept this order?

            builder.AddButton(100, 240, 4005, 4007, 1);
            builder.AddHtmlLocalized(135, 240, 120, 20, 1006044, 0x7FFF); // Ok

            builder.AddButton(275, 240, 4005, 4007, 0);
            builder.AddHtmlLocalized(310, 240, 120, 20, 1011012, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (info.ButtonID == 1) // Ok
            {
                if (from.PlaceInBackpack(_deed))
                {
                    from.SendLocalizedMessage(1045152); // The bulk order deed has been placed in your backpack.
                }
                else
                {
                    from.SendLocalizedMessage(1045150); // There is not enough room in your backpack for the deed.
                    _deed.Delete();
                }
            }
            else
            {
                _deed.Delete();
            }
        }

        public override void OnServerClose(NetState owner)
        {
            if (_deed?.Deleted == false)
            {
                _deed.Delete();
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
