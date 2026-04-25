using System;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.VeteranRewards;

public class RewardChoiceGump : DynamicGump
{
    private readonly Mobile _from;

    public override bool Singleton => true;

    private RewardChoiceGump(Mobile from) : base(0, 0) => _from = from;

    public static void DisplayTo(Mobile from)
    {
        if (from?.NetState == null)
        {
            return;
        }

        from.SendGump(new RewardChoiceGump(from));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        RenderBackground(ref builder);
        RenderCategories(ref builder);
    }

    private static void RenderBackground(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(10, 10, 600, 450, 2600);

        builder.AddButton(530, 415, 4017, 4019, 0);

        builder.AddButton(60, 415, 4014, 4016, 0, GumpButtonType.Page, 1);
        builder.AddHtmlLocalized(95, 415, 200, 20, 1049755); // Main Menu
    }

    private void RenderCategories(ref DynamicGumpBuilder builder)
    {
        var rewardInterval = RewardSystem.RewardInterval;

        string intervalAsString;

        if (rewardInterval == TimeSpan.FromDays(30.0))
        {
            intervalAsString = "month";
        }
        else if (rewardInterval == TimeSpan.FromDays(60.0))
        {
            intervalAsString = "two months";
        }
        else if (rewardInterval == TimeSpan.FromDays(90.0))
        {
            intervalAsString = "three months";
        }
        else if (rewardInterval == TimeSpan.FromDays(365.0))
        {
            intervalAsString = "year";
        }
        else
        {
            intervalAsString = $"{rewardInterval.TotalDays} day{(rewardInterval.TotalDays == 1 ? "" : "s")}";
        }

        builder.AddPage(1);

        builder.AddHtml(
            60,
            35,
            500,
            70,
            $"<B>Ultima Online Rewards Program</B><BR>Thank you for being a part of the Ultima Online community for a full {intervalAsString}.  As a token of our appreciation,  you may select from the following in-game reward items listed below.  The gift items will be attributed to the character you have logged-in with on the shard you are on when you chose the item(s).  The number of rewards you are entitled to are listed below and are for your entire account.  To read more about these rewards before making a selection, feel free to visit the uo.com site at <A HREF=\"http://www.uo.com/rewards\">http://www.uo.com/rewards</A>.",
            background: true,
            scrollbar: true
        );

        RewardSystem.ComputeRewardInfo(_from, out var cur, out var max);

        builder.AddHtmlLocalized(60, 105, 300, 35, 1006006); // Your current total of rewards to choose:
        builder.AddLabel(370, 107, 50, (max - cur).ToString());

        builder.AddHtmlLocalized(60, 140, 300, 35, 1006007); // You have already chosen:
        builder.AddLabel(370, 142, 50, cur.ToString());

        var categories = RewardSystem.Categories;

        var page = 2;

        for (var i = 0; i < categories.Length; ++i)
        {
            if (!RewardSystem.HasAccess(_from, categories[i]))
            {
                page += 1;
                continue;
            }

            builder.AddButton(100, 180 + i * 40, 4005, 4005, 0, GumpButtonType.Page, page);

            page += PagesPerCategory(categories[i]);

            if (categories[i].NameString != null)
            {
                builder.AddHtml(135, 180 + i * 40, 300, 20, categories[i].NameString);
            }
            else
            {
                builder.AddHtmlLocalized(135, 180 + i * 40, 300, 20, categories[i].Name);
            }
        }

        page = 2;

        for (var i = 0; i < categories.Length; ++i)
        {
            RenderCategory(ref builder, categories[i], i, ref page);
        }
    }

    private int PagesPerCategory(RewardCategory category)
    {
        var entries = category.Entries;
        var i = 0;

        for (var j = 0; j < entries.Count; j++)
        {
            if (RewardSystem.HasAccess(_from, entries[j]))
            {
                i++;
            }
        }

        return (int)Math.Ceiling(i / 24.0);
    }

    private static int GetButtonID(int type, int index) => 2 + index * 20 + type;

    private void RenderCategory(ref DynamicGumpBuilder builder, RewardCategory category, int index, ref int page)
    {
        builder.AddPage(page);

        var entries = category.Entries;

        var i = 0;

        for (var j = 0; j < entries.Count; ++j)
        {
            var entry = entries[j];

            if (!RewardSystem.HasAccess(_from, entry))
            {
                continue;
            }

            if (i == 24)
            {
                builder.AddButton(305, 415, 0xFA5, 0xFA7, 0, GumpButtonType.Page, ++page);
                builder.AddHtmlLocalized(340, 415, 200, 20, 1011066); // Next page

                builder.AddPage(page);

                builder.AddButton(270, 415, 0xFAE, 0xFB0, 0, GumpButtonType.Page, page - 1);
                builder.AddHtmlLocalized(185, 415, 200, 20, 1011067); // Previous page

                i = 0;
            }

            builder.AddButton(55 + i / 12 * 250, 80 + i % 12 * 25, 5540, 5541, GetButtonID(index, j));

            if (entry.NameString != null)
            {
                builder.AddHtml(80 + i / 12 * 250, 80 + i % 12 * 25, 250, 20, entry.NameString);
            }
            else
            {
                builder.AddHtmlLocalized(80 + i / 12 * 250, 80 + i % 12 * 25, 250, 20, entry.Name);
            }

            ++i;
        }

        page += 1;
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var buttonID = info.ButtonID - 1;

        if (buttonID == 0)
        {
            RewardSystem.ComputeRewardInfo(_from, out var cur, out var max);

            if (cur < max)
            {
                RewardNoticeGump.DisplayTo(_from);
            }
        }
        else
        {
            --buttonID;

            var type = buttonID % 20;
            var index = buttonID / 20;

            var categories = RewardSystem.Categories;

            if (type >= 0 && type < categories.Length)
            {
                var category = categories[type];

                if (index >= 0 && index < category.Entries.Count)
                {
                    var entry = category.Entries[index];

                    if (!RewardSystem.HasAccess(_from, entry))
                    {
                        return;
                    }

                    RewardConfirmGump.DisplayTo(_from, entry);
                }
            }
        }
    }
}
