using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Misc;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NameChangeDeed : Item
{
    [Constructible]
    public NameChangeDeed() : base(0x14F0) => LootType = LootType.Blessed;

    public override string DefaultName => "a name change deed";

    public override void OnDoubleClick(Mobile from)
    {
        if (RootParent == from)
        {
            NameChangeDeedGump.DisplayTo(from, this);
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }
}

public class NameChangeDeedGump : StaticGump<NameChangeDeedGump>
{
    private readonly Item _sender;

    public override bool Singleton => true;

    private NameChangeDeedGump(Item sender) : base(50, 50) => _sender = sender;

    public static void DisplayTo(Mobile from, Item sender)
    {
        if (from?.NetState == null || sender?.Deleted != false)
        {
            return;
        }

        from.SendGump(new NameChangeDeedGump(sender));
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        AddBlackAlpha(ref builder, 10, 120, 250, 85);
        builder.AddHtml(10, 125, 250, 20, "Name Change Deed".Center(0xFFFFFF));

        builder.AddLabel(73, 15, 1152, "");
        builder.AddLabel(20, 150, 0x480, "New Name:");
        AddTextField(ref builder, 100, 150, 150, 20, 0);

        AddButtonLabeled(ref builder, 75, 180, 1, "Submit");
    }

    private static void AddBlackAlpha(ref StaticGumpBuilder builder, int x, int y, int width, int height)
    {
        builder.AddImageTiled(x, y, width, height, 2624);
        builder.AddAlphaRegion(x, y, width, height);
    }

    private static void AddTextField(ref StaticGumpBuilder builder, int x, int y, int width, int height, int index)
    {
        builder.AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
        builder.AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
    }

    private static void AddButtonLabeled(ref StaticGumpBuilder builder, int x, int y, int buttonID, string text)
    {
        builder.AddButton(x, y - 1, 4005, 4007, buttonID);
        builder.AddHtml(x + 35, y, 240, 20, text.Color(0xFFFFFF));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_sender?.Deleted != false || info.ButtonID != 1 || _sender.RootParent != sender.Mobile)
        {
            return;
        }

        var m = sender.Mobile;

        var newName = info.GetTextEntry(0).AsSpan().Trim();

        if (!NameVerification.ValidatePlayerName(newName))
        {
            m.SendMessage("That name is unacceptable.");
            return;
        }

        m.RawName = newName.ToString();
        m.SendMessage("Your name has been changed!");
        m.SendMessage($"You are now known as {newName}");
        _sender.Delete();
    }
}
