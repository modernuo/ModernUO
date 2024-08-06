using Badlands.Items;
using Server;
using Server.Misc;
using Server.Mobiles;

namespace Badlands.Misc;

internal class RottweilerGift : GiftGiver
{
    public override DateTime Start { get; } = new( 2024, 1, 1 );
    public override DateTime Finish { get; } = new( 2025, 1, 31 );

    public static void Initialize()
    {
        GiftGiving.Register( new RottweilerGift() );
    }

    public override void GiveGift( Mobile mob )
    {
        if ( mob is not PlayerMobile player )
        {
            return;
        }

        var item = new EtherealRottweiler(player.Account);

        switch ( GiveGift( mob, item ) )
        {
            case GiftResult.Backpack:
                mob.SendMessage( 0x482, "An item has been placed in your backpack" );
                break;
            case GiftResult.BankBox:
                mob.SendMessage( 0x482, "An item has been placed in your bank" );
                break;
        }
    }
}
