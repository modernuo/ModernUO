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
            from.SendGump(new NameChangeDeedGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }
}

public class NameChangeDeedGump : Gump
{
    private readonly Item m_Sender;

    public override bool Singleton => true;

    public NameChangeDeedGump(Item sender) : base(50, 50)
    {
        m_Sender = sender;

        Closable = true;
        Draggable = true;
        Resizable = false;

        AddPage(0);

        AddBlackAlpha(10, 120, 250, 85);
        AddHtml(10, 125, 250, 20, "Name Change Deed".Center(0xFFFFFF));

        AddLabel(73, 15, 1152, "");
        AddLabel(20, 150, 0x480, "New Name:");
        AddTextField(100, 150, 150, 20, 0);

        AddButtonLabeled(75, 180, 1, "Submit");
    }

    public void AddBlackAlpha(int x, int y, int width, int height)
    {
        AddImageTiled(x, y, width, height, 2624);
        AddAlphaRegion(x, y, width, height);
    }

    public void AddTextField(int x, int y, int width, int height, int index)
    {
        AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
        AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
    }

    public void AddButtonLabeled(int x, int y, int buttonID, string text)
    {
        AddButton(x, y - 1, 4005, 4007, buttonID);
        AddHtml(x + 35, y, 240, 20, text.Color(0xFFFFFF));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (m_Sender?.Deleted != false || info.ButtonID != 1 || m_Sender.RootParent != sender.Mobile)
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
        m_Sender.Delete();
    }
}
