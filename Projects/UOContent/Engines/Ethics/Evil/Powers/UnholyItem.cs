using Server.Items;
using Server.Targeting;

namespace Server.Ethics.Evil
{
    public sealed class UnholyItem : Power
    {
        public UnholyItem() =>
            m_Definition = new PowerDefinition(
                5,
                "Unholy Item",
                "Vidda K'balc",
                ""
            );

        public override void BeginInvoke(Player from)
        {
            from.Mobile.BeginTarget(12, false, TargetFlags.None, Power_OnTarget, from);
            from.Mobile.SendMessage("Which item do you wish to imbue?");
        }

        private void Power_OnTarget(Mobile fromMobile, object obj, Player from)
        {
            if (obj is not Item item)
            {
                from.Mobile.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You may not imbue that.");
                return;
            }

            if (item.Parent != from.Mobile)
            {
                from.Mobile.LocalOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    false,
                    "You may only imbue items you are wearing."
                );
                return;
            }

            if ((item.SavedFlags & 0x300) != 0)
            {
                from.Mobile.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "That has already been imbued.");
                return;
            }

            var canImbue = item is Spellbook or BaseClothing or BaseArmor or BaseWeapon &&
                           item.Name == null;

            if (canImbue)
            {
                if (!CheckInvoke(from))
                {
                    return;
                }

                item.Hue = Ethic.Evil.Definition.PrimaryHue;
                item.SavedFlags |= 0x200;

                from.Mobile.FixedEffect(0x375A, 10, 20);
                from.Mobile.PlaySound(0x209);

                FinishInvoke(from);
            }
            else
            {
                from.Mobile.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You may not imbue that.");
            }
        }
    }
}
