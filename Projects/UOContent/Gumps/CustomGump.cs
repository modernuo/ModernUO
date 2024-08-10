namespace Server.Gumps;

public class CustomGump : DynamicGump
{
    public static void Configure()
    {
        CommandSystem.Register("CustomGump", AccessLevel.Administrator, e => DisplayTo(e.Mobile));
    }

    public static void DisplayTo(Mobile user)
    {
        if (user == null || user.Deleted || !user.Player || user.NetState == null)
            return;

        user.CloseGump<CustomGump>();
        user.SendGump(new CustomGump());
    }

    public CustomGump()
        : base(150, 150)
    {
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();
        builder.SetNoResize();

        builder.AddPage();
        builder.AddBackground(0, 0, 330, 255, 2620);
        builder.AddItem(20, 80, 4650);
        builder.AddItem(30, 60, 4653);
        builder.AddItem(20, 20, 3);
        builder.AddItem(0, 20, 2);
        builder.AddItem(40, 90, 4651);

        /*
         * It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
         * CONTINUE - You chose to try to come back to life now.<br>
         * CANCEL - You prefer to remain a ghost for now.
         */
         builder.AddHtmlLocalized(90, 20, 220, 200, 1011023 + (int)2, 0x7FFF);

        builder.AddButton(280, 180, 4005, 4007, 0);
        builder.AddHtmlLocalized(220, 182, 110, 35, 1011012, 0x7FFF); // CANCEL

        builder.AddButton(280, 210, 4005, 4007, 1);
        builder.AddHtmlLocalized(210, 212, 110, 35, 1011011, 0x7FFF); // CONTINUE
    }
}
