using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public static class BarkeeperGump
{
    public static void DisplayTo(Mobile from, PlayerBarkeeper barkeeper)
    {
        from.CloseGump<BarkeeperTitleGump>();
        from.CloseGump<BarkeeperHumanGump>();
        from.CloseGump<BarkeeperNonHumanGump>();

        if ((int)barkeeper.Body is not 0x340 and not 0x402)
        {
            from.SendGump(new BarkeeperHumanGump(barkeeper));
        }
        else
        {
            from.SendGump(new BarkeeperNonHumanGump(barkeeper));
        }
    }
}

file class BarkeeperHumanGump(PlayerBarkeeper barkeeper) : BarkeeperGump<BarkeeperHumanGump>(barkeeper)
{
    protected override bool ModifyAppearance => true;
}

file class BarkeeperNonHumanGump(PlayerBarkeeper barkeeper) : BarkeeperGump<BarkeeperNonHumanGump>(barkeeper)
{
    protected override bool ModifyAppearance => false;
}

abstract file class BarkeeperGump<T> : StaticGump<T> where T : BarkeeperGump<T>
{
    private readonly PlayerBarkeeper _barkeeper;

    public override bool Singleton => true;

    protected abstract bool ModifyAppearance { get; }

    protected BarkeeperGump(PlayerBarkeeper barkeeper) : base(0, 0) => _barkeeper = barkeeper;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        RenderBackground(ref builder);
        RenderCategories(ref builder);
        RenderMessageManagement(ref builder);
        RenderDismissConfirmation(ref builder);
        RenderMessageManagement_Message_AddOrChange(ref builder);
        RenderMessageManagement_Message_Remove(ref builder);
        RenderMessageManagement_Tip_AddOrChange(ref builder);
        RenderMessageManagement_Tip_Remove(ref builder);
        RenderAppearanceCategories(ref builder);
    }

    private static void RenderBackground(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(30, 40, 585, 410, 5054);

        builder.AddImage(30, 40, 9251);
        builder.AddImage(180, 40, 9251);
        builder.AddImage(30, 40, 9253);
        builder.AddImage(30, 130, 9253);
        builder.AddImage(598, 40, 9255);
        builder.AddImage(598, 130, 9255);
        builder.AddImage(30, 433, 9257);
        builder.AddImage(180, 433, 9257);
        builder.AddImage(30, 40, 9250);
        builder.AddImage(598, 40, 9252);
        builder.AddImage(598, 433, 9258);
        builder.AddImage(30, 433, 9256);

        builder.AddItem(30, 40, 6816);
        builder.AddItem(30, 125, 6817);
        builder.AddItem(30, 233, 6817);
        builder.AddItem(30, 341, 6817);
        builder.AddItem(580, 40, 6814);
        builder.AddItem(588, 125, 6815);
        builder.AddItem(588, 233, 6815);
        builder.AddItem(588, 341, 6815);

        builder.AddBackground(183, 25, 280, 30, 5054);

        builder.AddImage(180, 25, 10460);
        builder.AddImage(434, 25, 10460);
        builder.AddImage(560, 20, 1417);

        builder.AddHtmlLocalized(223, 32, 200, 40, 1078366); // BARKEEP CUSTOMIZATION MENU
        builder.AddBackground(243, 433, 150, 30, 5054);

        builder.AddImage(240, 433, 10460);
        builder.AddImage(375, 433, 10460);
    }

    private static void RenderCategories(ref StaticGumpBuilder builder)
    {
        builder.AddPage(1);

        builder.AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(170, 120, 200, 40, 1078352); // Message Control

        builder.AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 8);
        builder.AddHtmlLocalized(170, 200, 200, 40, 1078353); // Customize your barkeep

        builder.AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 3);
        builder.AddHtmlLocalized(170, 280, 200, 40, 1078354); // Dismiss your barkeep

        builder.AddButton(338, 437, 4014, 4016, 0);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(574, 43, 5360);
    }

    private static void RenderMessageManagement(ref StaticGumpBuilder builder)
    {
        builder.AddPage(2);

        builder.AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 4);
        builder.AddHtmlLocalized(170, 120, 380, 20, 1078355); // Add or change a message and keyword

        builder.AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 5);
        builder.AddHtmlLocalized(170, 200, 380, 20, 1078356); // Remove a message and keyword from your barkeep

        builder.AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 6);
        builder.AddHtmlLocalized(170, 280, 380, 20, 1078357); // Add or change your barkeeper's tip message

        builder.AddButton(130, 360, 4005, 4007, 0, GumpButtonType.Page, 7);
        builder.AddHtmlLocalized(170, 360, 380, 20, 1078358); // Delete your barkeepers tip message

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private static void RenderDismissConfirmation(ref StaticGumpBuilder builder)
    {
        builder.AddPage(3);

        builder.AddHtmlLocalized(170, 160, 380, 20, 1078359); // Are you sure you want to dismiss your barkeeper?

        builder.AddButton(205, 280, 4005, 4007, GetButtonID(0, 0));
        builder.AddHtmlLocalized(240, 280, 100, 20, 1046362); // Yes

        builder.AddButton(395, 280, 4005, 4007, 0);
        builder.AddHtmlLocalized(430, 280, 100, 20, 1046363); // No

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(574, 43, 5360);
        builder.AddItem(584, 34, 6579);
    }

    private static void RenderMessageManagement_Message_AddOrChange(ref StaticGumpBuilder builder)
    {
        builder.AddPage(4);

        builder.AddHtmlLocalized(250, 60, 500, 25, 1078360); // Add or change a message

        for (var i = 0; i < PlayerBarkeeper.RumorCount; ++i)
        {
            builder.AddHtml(100, 70 + i * 120, 50, 20, "Message");
            builder.AddHtmlPlaceholder(100, 90 + i * 120, 450, 40, $"r{i}msg", background: true);
            builder.AddHtmlLocalized(100, 130 + i * 120, 50, 20, 1078361); // Keyword
            builder.AddHtmlPlaceholder(100, 150 + i * 120, 450, 40, $"r{i}key", background: true);

            builder.AddButton(60, 90 + i * 120, 4005, 4007, GetButtonID(1, i));
        }

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private static void RenderMessageManagement_Message_Remove(ref StaticGumpBuilder builder)
    {
        builder.AddPage(5);

        builder.AddHtmlLocalized(190, 60, 500, 25, 1078362); // Choose the message you would like to remove

        for (var i = 0; i < PlayerBarkeeper.RumorCount; ++i)
        {
            builder.AddHtml(100, 70 + i * 120, 50, 20, "Message");
            builder.AddHtmlPlaceholder(100, 90 + i * 120, 450, 40, $"r{i}msg", background: true);
            builder.AddHtmlLocalized(100, 130 + i * 120, 50, 20, 1078361); // Keyword
            builder.AddHtmlPlaceholder(100, 150 + i * 120, 450, 40, $"r{i}key", background: true);

            builder.AddButton(60, 90 + i * 120, 4005, 4007, GetButtonID(2, i));
        }

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private static int GetButtonID(int type, int index) => 1 + index * 6 + type;

    private static void RenderMessageManagement_Tip_AddOrChange(ref StaticGumpBuilder builder)
    {
        builder.AddPage(6);

        builder.AddHtmlLocalized(250, 95, 500, 20, 1078363); // Change this tip message
        builder.AddHtml(100, 190, 50, 20, "Message");
        builder.AddHtmlPlaceholder(100, 210, 450, 40, "tipMsg", background: true);

        builder.AddButton(60, 210, 4005, 4007, GetButtonID(3, 0));

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private static void RenderMessageManagement_Tip_Remove(ref StaticGumpBuilder builder)
    {
        builder.AddPage(7);

        builder.AddHtmlLocalized(250, 95, 500, 20, 1078364); // Remove this tip message
        builder.AddHtml(100, 190, 50, 20, "Message");
        builder.AddHtmlPlaceholder(100, 210, 450, 40, "tipMsg", background: true);

        builder.AddButton(60, 210, 4005, 4007, GetButtonID(4, 0));

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private void RenderAppearanceCategories(ref StaticGumpBuilder builder)
    {
        builder.AddPage(8);

        builder.AddButton(130, 120, 4005, 4007, GetButtonID(5, 0));
        builder.AddHtml(170, 120, 120, 20, "Title");

        if (ModifyAppearance)
        {
            builder.AddButton(130, 200, 4005, 4007, GetButtonID(5, 1));
            builder.AddHtmlLocalized(170, 200, 120, 20, 1077829); // Appearance

            builder.AddButton(130, 280, 4005, 4007, GetButtonID(5, 2));
            builder.AddHtmlLocalized(170, 280, 120, 20, 1078365); // Male / Female
        }

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 44, 4033);
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        var rumors = _barkeeper.Rumors ?? PlayerBarkeeper.EmptyRumors;

        for (var i = 0; i < rumors.Length; ++i)
        {
            var rumor = rumors[i];
            builder.SetStringSlot($"r{i}msg", rumor?.Message ?? "No current message");
            builder.SetStringSlot($"r{i}key", rumor?.Keyword ?? "None");
        }

        builder.SetStringSlot("tipMsg", _barkeeper.TipMessage ?? "No current tip message");
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (!_barkeeper.IsOwner(from))
        {
            return;
        }

        var index = info.ButtonID - 1;

        if (index < 0)
        {
            return;
        }

        index = Math.DivRem(index, 6, out var type);

        switch (type)
        {
            case 0: // Controls
                {
                    switch (index)
                    {
                        case 0: // Dismiss
                            {
                                _barkeeper.Dismiss();
                                break;
                            }
                    }

                    break;
                }
            case 1: // Change message
                {
                    _barkeeper.BeginChangeRumor(from, index);
                    break;
                }
            case 2: // Remove message
                {
                    _barkeeper.RemoveRumor(index);
                    break;
                }
            case 3: // Change tip
                {
                    _barkeeper.BeginChangeTip(from);
                    break;
                }
            case 4: // Remove tip
                {
                    _barkeeper.RemoveTip();
                    break;
                }
            case 5: // Appearance category selection
                {
                    switch (index)
                    {
                        case 0:
                            {
                                _barkeeper.BeginChangeTitle(from);
                                break;
                            }
                        case 1:
                            {
                                _barkeeper.BeginChangeAppearance(from);
                                break;
                            }
                        case 2:
                            {
                                _barkeeper.ChangeGender();
                                break;
                            }
                    }

                    break;
                }
        }
    }
}
