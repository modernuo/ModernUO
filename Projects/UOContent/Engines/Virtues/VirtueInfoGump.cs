using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Virtues;

public class VirtueInfoGump : Gump
{
    private readonly PlayerMobile _beholder;
    private readonly int _desc;
    private readonly string _site;
    private readonly VirtueName _virtue;

    public VirtueInfoGump(PlayerMobile beholder, VirtueName virtue, int description, string webPage = null) : base(0, 0)
    {
        _beholder = beholder;
        _virtue = virtue;
        _desc = description;
        _site = webPage;

        var value = VirtueSystem.GetVirtues(beholder)?.GetValue((int)virtue) ?? 0;

        AddPage(0);

        AddImage(30, 40, 2080);
        AddImage(47, 77, 2081);
        AddImage(47, 147, 2081);
        AddImage(47, 217, 2081);
        AddImage(47, 267, 2083);
        AddImage(70, 213, 2091);

        AddPage(1);

        var maxValue = VirtueSystem.GetMaxAmount(_virtue);

        int valueDesc;
        int dots;

        if (value < 4000)
        {
            dots = value / 400;
        }
        else if (value < 10000)
        {
            dots = (value - 4000) / 600;
        }
        else if (value < maxValue)
        {
            dots = (value - 10000) / ((maxValue - 10000) / 10);
        }
        else
        {
            dots = 10;
        }

        for (var i = 0; i < 10; ++i)
        {
            AddImage(95 + i * 17, 50, i < dots ? 2362 : 2360);
        }

        if (value < 1)
        {
            valueDesc = 1052044; // You have not started on the path of this Virtue.
        }
        else if (value < 400)
        {
            valueDesc = 1052045; // You have barely begun your journey through the path of this Virtue.
        }
        else if (value < 2000)
        {
            valueDesc = 1052046; // You have progressed in this Virtue, but still have much to do.
        }
        else if (value < 3600)
        {
            valueDesc = 1052047; // Your journey through the path of this Virtue is going well.
        }
        else if (value < 4000)
        {
            valueDesc = 1052048; // You feel very close to achieving your next path in this Virtue.
        }
        else if (dots < 1)
        {
            valueDesc = 1052049; // You have achieved a path in this Virtue.
        }
        else if (dots < 9)
        {
            valueDesc = 1052047; // Your journey through the path of this Virtue is going well.
        }
        else if (dots < 10)
        {
            valueDesc = 1052048; // You feel very close to achieving your next path in this Virtue.
        }
        else
        {
            valueDesc = 1052050; // You have achieved the highest path in this Virtue.
        }

        AddHtmlLocalized(157, 73, 200, 40, 1051000 + (int)virtue);
        AddHtmlLocalized(75, 95, 220, 140, description);
        AddHtmlLocalized(70, 224, 229, 60, valueDesc);

        AddButton(65, 277, 1209, 1209, 1);

        AddButton(280, 43, 4014, 4014, 2);

        // This virtue is not yet defined. OR -click to learn more (opens webpage)
        AddHtmlLocalized(83, 275, 400, 40, webPage == null ? 1052055 : 1052052);
    }

    public override void OnResponse(NetState state, RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 1:
                {
                    _beholder.SendGump(new VirtueInfoGump(_beholder, _virtue, _desc, _site));

                    if (_site != null)
                    {
                        state.SendLaunchBrowser(_site); // No message about web browser starting on OSI
                    }

                    break;
                }
            case 2:
                {
                    _beholder.SendGump(new VirtueStatusGump(_beholder));
                    break;
                }
        }
    }
}
