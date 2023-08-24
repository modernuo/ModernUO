using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Commands;

public static class StaffDress
{
    public static void Initialize()
    {
        CommandSystem.Register("ResetStaffDress", AccessLevel.Counselor, StaffDress_OnCommand);
    }

    [Usage("ResetStaffDress")]
    [Description("Resets staff to proper GM")]
    public static void StaffDress_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.Race = Race.Human;
        pm.Karma = pm.Fame = pm.Kills = pm.ShortTermMurders = pm.BodyMod = 0;
        pm.Body = 987;
        pm.SolidHueOverride = pm.HueMod = -1;
        pm.FacialHairItemID = 0;
        pm.Blessed = true;
        pm.DisplayGuildTitle = false;
        pm.DisplayChampionTitle = false;
        if (pm.Mount != null)
        {
            pm.Mount.Rider = null;
        }

        pm.NetState.SendSpeedControl(SpeedControlSetting.Mount);
        pm.ResetStaffAccess();

        if (pm.AccessLevel < AccessLevel.Administrator)
        {
            pm.Hue = Race.Human.ClipSkinHue((pm.Hue + 1) & 0x3FFF);
        }

        for (var i = pm.Items.Count - 1; i >= 0; i--)
        {
            var item = pm.Items[i];

            if (item.Layer is Layer.FacialHair)
            {
                item.Delete();
            }

            if (item.Layer is not Layer.Backpack
                and not Layer.Bank
                and not Layer.Hair
                and not Layer.Mount
                and not Layer.ShopBuy
                and not Layer.ShopResale
                and not Layer.ShopSell)
            {
                pm.AddToBackpack(item);
            }
        }

        if (pm.AccessLevel > AccessLevel.Player)
        {
            pm.AddItem(new StaffRobe(pm.AccessLevel));
        }
    }
}
