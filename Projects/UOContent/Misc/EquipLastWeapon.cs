using Server.Items;
using Server.Mobiles;

namespace Server.Network;

public class EquipLastWeaponPacket
{
    public static unsafe void Initialize()
    {
        IncomingPackets.RegisterEncoded( 0x1E, true, &EquipLastWeaponRequest );
    }

    private static void EquipLastWeaponRequest( NetState state, IEntity e, EncodedReader reader )
    {
        if ( state.Mobile is not PlayerMobile { Alive: true } from || from.Backpack == null )
        {
            return;
        }

        if ( Core.TickCount - from.NextActionTime >= 0 )
        {
            var toEquip = from.LastWeapon;
            var toDisarm = from.FindItemOnLayer( Layer.OneHanded ) as BaseWeapon ??
                           from.FindItemOnLayer( Layer.TwoHanded ) as BaseWeapon;

            if ( toDisarm != null )
            {
                from.Backpack.DropItem( toDisarm );
                from.NextActionTime = Core.TickCount + Mobile.ActionDelay;
            }

            if ( toEquip != toDisarm && toEquip is { Movable: true } && toEquip.IsChildOf( from.Backpack ) )
            {
                from.EquipItem( toEquip );
                from.NextActionTime = Core.TickCount + Mobile.ActionDelay;

                from.SendLocalizedMessage( toDisarm != null ? 1063111 /* You put your weapon into your backpack and pick up your last weapon. */ : 1063112 /* You pick up your last weapon. */ );
            }
            else
            {
                if ( toEquip == null )
                {
                    from.SendLocalizedMessage(
                        1063113
                    ); // You put your weapon into your backpack, but cannot pick up your last weapon!
                    return;
                }

                if ( !toEquip.IsChildOf( from.Backpack ) )
                {
                    from.SendLocalizedMessage(
                        1063109
                    ); // Your last weapon must be in your backpack to be able to switch it quickly.
                    return;
                }

                from.SendLocalizedMessage( 1063114 ); // You cannot pick up your last weapon!
            }
        }
        else
        {
            from.SendActionMessage();
        }
    }
}
