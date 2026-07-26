using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class CancelRenewInventoryInsuranceGump : StaticGump<CancelRenewInventoryInsuranceGump>
{
    private readonly ItemInsuranceMenuGump _insuranceGump;

    public override bool Singleton => true;

    public CancelRenewInventoryInsuranceGump(ItemInsuranceMenuGump insuranceGump) : base(250, 200) =>
        _insuranceGump = insuranceGump;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddBackground(0, 0, 240, 142, 0x13BE);
        builder.AddImageTiled(6, 6, 228, 100, 0xA40);
        builder.AddImageTiled(6, 116, 228, 20, 0xA40);
        builder.AddAlphaRegion(6, 6, 228, 142);

        // You are about to disable inventory insurance auto-renewal.
        builder.AddHtmlLocalized(8, 8, 228, 100, 1071021, 0x7FFF);

        builder.AddButton(6, 116, 0xFB1, 0xFB2, 0);
        builder.AddHtmlLocalized(40, 118, 450, 20, 1060051, 0x7FFF); // CANCEL

        builder.AddButton(114, 116, 0xFA5, 0xFA7, 1);
        builder.AddHtmlLocalized(148, 118, 450, 20, 1071022, 0x7FFF); // DISABLE IT!
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile pm || !pm.CheckAlive())
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            // You have cancelled automatically reinsuring all insured items upon death
            pm.SendLocalizedMessage(1061075, "", 0x23);
            pm.AutoRenewInsurance = false;
        }
        else
        {
            pm.SendLocalizedMessage(1042021); // Cancelled.
        }

        if (_insuranceGump != null)
        {
            pm.SendGump(_insuranceGump);
        }
    }
}
