using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Virtues;

public class VirtueStatusGump : StaticGump<VirtueStatusGump>
{
    private readonly PlayerMobile _beholder;

    public override bool Singleton => true;

    private VirtueStatusGump(PlayerMobile beholder) : base(0, 0)
    {
        _beholder = beholder;
    }

    public static void DisplayTo(PlayerMobile beholder)
    {
        if (beholder?.NetState == null)
        {
            return;
        }

        beholder.SendGump(new VirtueStatusGump(beholder));
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddImage(30, 40, 2080);
        builder.AddImage(47, 77, 2081);
        builder.AddImage(47, 147, 2081);
        builder.AddImage(47, 217, 2081);
        builder.AddImage(47, 267, 2083);
        builder.AddImage(70, 213, 2091);

        builder.AddPage(1);

        builder.AddHtmlLocalized(140, 73, 200, 20, 1077972); // The Virtues

        builder.AddHtmlLocalized(80, 100, 100, 40, 1051000);  // Humility
        builder.AddHtmlLocalized(80, 129, 100, 40, 1051001);  // Sacrifice
        builder.AddHtmlLocalized(80, 159, 100, 40, 1051002);  // Compassion
        builder.AddHtmlLocalized(80, 189, 100, 40, 1051003);  // Spirituality
        builder.AddHtmlLocalized(200, 100, 200, 40, 1051004); // Valor
        builder.AddHtmlLocalized(200, 129, 200, 40, 1051005); // Honor
        builder.AddHtmlLocalized(200, 159, 200, 40, 1051006); // Justice
        builder.AddHtmlLocalized(200, 189, 200, 40, 1051007); // Honesty

        builder.AddHtmlLocalized(75, 224, 220, 60, 1052062); // Click on a blue gem to view your status in that virtue.

        builder.AddButton(60, 100, 1210, 1210, 1);  // Humility
        builder.AddButton(60, 129, 1210, 1210, 2);  // Sacrifice
        builder.AddButton(60, 159, 1210, 1210, 3);  // Compassion
        builder.AddButton(60, 189, 1210, 1210, 4);  // Spirituality
        builder.AddButton(180, 100, 1210, 1210, 5); // Valor
        builder.AddButton(180, 129, 1210, 1210, 6); // Honor
        builder.AddButton(180, 159, 1210, 1210, 7); // Justice
        builder.AddButton(180, 189, 1210, 1210, 8); // Honesty

        builder.AddButton(280, 43, 4014, 4014, 9);
    }

    private static int GetVirtueDescription(VirtueName virtue) =>
        virtue switch
        {
            VirtueName.Humility => 1052051, // Humility is perceiving one's place in the world, not according to one's own accomplishments, but according to the intrinsic value of all individuals. One gains Humility through Humility Hunts.
            VirtueName.Sacrifice => 1052053, // Sacrifice is the courage to give of oneself in the name of love. One gains in Sacrifice by giving away their fame to certain monsters to save them from their eternal torment.
            VirtueName.Compassion => 1053000, // Compassion is the nonjudgmental empathy for one's fellow creatures.  You gain in Compassion by escorting NPCs and prisoners safely to their destinations.
            VirtueName.Spirituality => 1052056, // Spirituality is the concern with one's inner being and how one deals with truth, love, and courage. One gains Spirituality by healing others.
            VirtueName.Valor => 1054033, // Valor is the courage to take actions in support of one's convictions. You gain in Valor by slaying creatures spawned in regions controlled by Champions of Evil.
            VirtueName.Honor => 1052058, // Honor is the courage to stand for truth, against any odds. Honor is gained by entering Honorable combat with thy foes.
            VirtueName.Justice => 1052059, // Justice is the devotion to truth, tempered by love. Justice is gained by making those who would murder innocents pay for their crimes.
            VirtueName.Honesty => 1052060, // Honesty is the scrupulous respect for truth, the willingness to never deceive oneself or another.
            _ => 0
        };

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 9)
        {
            VirtueGump.RequestVirtueGump(_beholder, _beholder);
            return;
        }

        if (info.ButtonID is < 1 or > (int)VirtueName.Honesty)
        {
            return;
        }

        var virtue = (VirtueName)(info.ButtonID - 1);

        VirtueInfoGump.DisplayTo(
            _beholder,
            virtue,
            GetVirtueDescription(virtue),
            @$"https://uo.com/wiki/ultima-online-wiki/gameplay/the-virtues/#{VirtueSystem.GetLowerCaseName(virtue)}"
        );
    }
}
