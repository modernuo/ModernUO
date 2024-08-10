using Server.Accounting;
using Server.Network;

namespace Server.Gumps;

public class YoungDungeonWarningGump : StaticGump<YoungDungeonWarningGump>
{
    public YoungDungeonWarningGump() : base(150, 200)
    {
    }

    public override bool Singleton => true;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddBackground(0, 0, 250, 170, 0x12E);
        builder.AddBackground(10, 10, 230, 150, 0x1400);

        // Warning: monsters may attack you on site down here in the dungeons!
        builder.AddHtmlLocalized(20, 25, 210, 70, 1018030, 0x7FFF);

        builder.AddButton(163, 125, 0xF8, 0xF9, 0);
    }
}

public class YoungDeathNoticeGump : StaticGump<YoungDeathNoticeGump>
{
    public YoungDeathNoticeGump() : base(100, 15)
    {
    }

    public override bool Singleton => true;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddBackground(25, 10, 425, 444, 0x12E);
        builder.AddBackground(35, 20, 405, 424, 0x1400);
        builder.AddAlphaRegion(25, 10, 425, 444);

        builder.AddHtmlLocalized(190, 34, 120, 20, 1046287, 0x7D00); // You have died.

        // As a ghost you cannot interact with the world. You cannot touch items nor can you use them.
        builder.AddHtmlLocalized(50, 60, 380, 40, 1046288, 0x7FFF);
        // You can pass through doors as though they do not exist.  However, you cannot pass through walls.
        builder.AddHtmlLocalized(50, 105, 380, 45, 1046289, 0x7FFF);
        // Since you are a new player, any items you had on your person at the time of your death will be in your backpack upon resurrection.
        builder.AddHtmlLocalized(50, 150, 380, 60, 1046291, 0x7FFF);
        // To be resurrected you must find a healer in town or wandering in the wilderness.  Some powerful players may also be able to resurrect you.
        builder.AddHtmlLocalized(50, 214, 380, 65, 1046292, 0x7FFF);
        // While you are still in young status, you will be transported to the nearest healer (along with your items) at the time of your death.
        builder.AddHtmlLocalized(50, 279, 380, 65, 1046293, 0x7FFF);
        // To rejoin the world of the living simply walk near one of the NPC healers, and they will resurrect you as long as you are not marked as a criminal.
        builder.AddHtmlLocalized(50, 339, 380, 70, 1046294, 0x7FFF);

        builder.AddButton(363, 409, 0xF8, 0xF9, 0);
    }
}

public class RenounceYoungGump : StaticGump<RenounceYoungGump>
{
    public RenounceYoungGump() : base(150, 50)
    {
    }

    public override bool Singleton => true;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddBackground(0, 0, 450, 330, 0x12E);
        builder.AddBackground(10, 10, 430, 310, 0x1400);

        builder.AddHtmlLocalized(0, 24, 450, 20, 1013004, 0x7FFF); // <center> Renouncing 'Young Player' Status</center>

        /*
         * As a 'Young' player, you are currently under a system of protection that prevents
         * you from being attacked by other players and certain monsters.<br><br>
         *
         * If you choose to renounce your status as a 'Young' player, you will lose this protection.
         * You will become vulnerable to other players, and many monsters that had only glared
         * at you menacingly before will now attack you on sight!<br><br>
         *
         * Select OKAY now if you wish to renounce your status as a 'Young' player, otherwise
         * press CANCEL.
         */
        builder.AddHtmlLocalized(25, 50, 380, 210, 1013005, 0x7FFF);

        builder.AddButton(363, 285, 0xF8, 0xF9, 1); // OKAY
        builder.AddButton(283, 285, 0xF1, 0xF2, 0); // CANCEL
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID != 1)
        {
            from.SendLocalizedMessage(502086); // You have chosen not to renounce your `Young' player status.
            return;
        }

        (from.Account as Account)?.RemoveYoungStatus(502085); // You have chosen to renounce your `Young' player status.
    }
}
