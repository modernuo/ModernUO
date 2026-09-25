using Server.Items;

namespace Server.Mobiles
{
    public static class SolenHelper
    {
        public static void PackPicnicBasket(BaseCreature solen)
        {
            if (Utility.Random(100) < 1)
            {
                var basket = new PicnicBasket();

                basket.DropItem(new BeverageBottle(BeverageType.Wine));
                basket.DropItem(new CheeseWedge());

                solen.PackItem(basket);
            }
        }

        public static bool CheckRedFriendship(Mobile m)
        {
            if (m is BaseCreature bc)
            {
                if (bc.GetMaster() is PlayerMobile master)
                {
                    return CheckRedFriendship(master);
                }
            }

            return m is PlayerMobile player && player.SolenFriendship == SolenFriendship.Red;
        }

        public static bool CheckBlackFriendship(Mobile m)
        {
            if (m is BaseCreature bc)
            {
                if (bc.GetMaster() is PlayerMobile master)
                {
                    return CheckBlackFriendship(master);
                }
            }

            return m is PlayerMobile player && player.SolenFriendship == SolenFriendship.Black;
        }

        public static void OnRedDamage(Mobile from)
        {
            if (from is BaseCreature bc)
            {
                if (bc.GetMaster() is PlayerMobile master)
                {
                    OnRedDamage(master);
                }
            }

            if (from is PlayerMobile player && player.SolenFriendship == SolenFriendship.Red)
            {
                player.LocalOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    1054103
                ); // The solen revoke their friendship. You will now be considered an intruder.

                player.SolenFriendship = SolenFriendship.None;
            }
        }

        public static void OnBlackDamage(Mobile from)
        {
            if (from is BaseCreature bc)
            {
                if (bc.GetMaster() is PlayerMobile master)
                {
                    OnBlackDamage(master);
                }
            }

            if (from is PlayerMobile player && player.SolenFriendship == SolenFriendship.Black)
            {
                player.LocalOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    1054103
                ); // The solen revoke their friendship. You will now be considered an intruder.

                player.SolenFriendship = SolenFriendship.None;
            }
        }
    }
}
