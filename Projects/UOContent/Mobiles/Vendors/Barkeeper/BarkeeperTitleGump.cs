using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class BarkeeperTitleGump : StaticGump<BarkeeperTitleGump>
{
    private static readonly Entry[] _entries =
    [
        new(1076083, "the alchemist"),
        new(1077244, "the animal tamer"),
        new(1078440, "the apothecary"),
        new(1078441, "the artist"),
        new(1078442, "the baker", true),
        new(1073298, "the bard"),
        new(1076102, "the barkeep", true),
        new(1078443, "the beggar"),
        new(1073297, "the blacksmith"),
        new(1078444, "the bounty hunter"),
        new(1078446, "the brigand"),
        new(1078447, "the butler"),
        new(1060774, "the carpenter"),
        new(1078448, "the chef", true),
        new(1078449, "the commander"),
        new(1078450, "the curator"),
        new(1078451, "the drunkard"),
        new(1078452, "the farmer"),
        new(1078453, "the fisherman"),
        new(1078454, "the gambler"),
        new(1078455, "the gypsy"),
        new(1075996, "the herald"),
        new(1076107, "the herbalist"),
        new(1078465, "the hermit"),
        new(1078466, "the innkeeper", true),
        new(1078467, "the jailor"),
        new(1078468, "the jester"),
        new(1078469, "the librarian"),
        new(1073292, "the mage"),
        new(1078470, "the mercenary"),
        new(1060775, "the merchant"),
        new(1078472, "the messenger"),
        new(1076093, "the miner"),
        new(1078475, "the monk"),
        new(1078476, "the noble"),
        new(1073290, "the paladin"),
        new(1078479, "the peasant"),
        new(1078480, "the pirate"),
        new(1078481, "the prisoner"),
        new(1078482, "the prophet"),
        new(1078484, "the ranger"),
        new(1078487, "the sage"),
        new(1078488, "the sailor"),
        new(1078489, "the scholar"),
        new(1060773, "the scribe"),
        new(1078490, "the sentry"),
        new(1060795, "the servant"),
        new(1078491, "the shepherd"),
        new(1078492, "the soothsayer"),
        new(1078493, "the stoic"),
        new(1078494, "the storyteller"),
        new(1076134, "the tailor"),
        new(1076096, "the thief"),
        new(1076137, "the tinker"),
        new(1076097, "the town crier"),
        new(1073291, "the treasure hunter"),
        new(1076112, "the waiter", true),
        new(1077242, "the warrior"),
        new(1078496, "the watchman"),
        new(1078495, null) // No Title
    ];

    private static readonly int _pageCount = (_entries.Length + 19) / 20;

    private readonly PlayerBarkeeper _barkeeper;

    public override bool Singleton => true;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        RenderBackground(ref builder);

        for (var i = 0; i < _pageCount; ++i)
        {
            RenderPage(ref builder, i);
        }
    }

    public BarkeeperTitleGump(PlayerBarkeeper barkeeper) : base(0, 0) => _barkeeper = barkeeper;

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

        builder.AddImage(560, 20, 1417);
        builder.AddItem(580, 44, 4033);

        builder.AddBackground(183, 25, 280, 30, 5054);

        builder.AddImage(180, 25, 10460);
        builder.AddImage(434, 25, 10460);

        builder.AddHtmlLocalized(223, 32, 200, 40, 1078366); // BARKEEP CUSTOMIZATION MENU
        builder.AddBackground(243, 433, 150, 30, 5054);

        builder.AddImage(240, 433, 10460);
        builder.AddImage(375, 433, 10460);

        builder.AddImage(80, 398, 2151);
        builder.AddItem(72, 406, 2543);

        builder.AddHtmlLocalized(110, 412, 180, 25, 1078445); // sells food and drink
    }

    private static void RenderPage(ref StaticGumpBuilder builder, int page)
    {
        var currentPage = page + 1;
        builder.AddPage(currentPage);

        if (_pageCount == 3 && currentPage is >= 1 and <= 3)
        {
            var pageCliloc = currentPage switch
            {
                1 => 1078439, // Page 1 of 3
                2 => 1078464, // Page 2 of 3
                3 => 1078483, // Page 3 of 3
            };

            builder.AddHtmlLocalized(430, 70, 180, 25, pageCliloc);
        }
        else
        {
            // Page ~1_CUR~ of ~2_MAX~
            builder.AddHtmlLocalized(430, 70, 180, 25, 1153561, $"{currentPage}\t{_pageCount}", 0x7FFF);
        }

        for (int count = 0, i = page * 20; count < 20 && i < _entries.Length; ++count, ++i)
        {
            var entry = _entries[i];

            var xOffset = Math.DivRem(count, 10, out var yOffset);

            builder.AddButton(80 + xOffset * 260, 100 + yOffset * 30, 4005, 4007, 2 + i);
            builder.AddHtmlLocalized(
                120 + xOffset * 260,
                100 + yOffset * 30,
                entry.Vendor ? 148 : 180,
                25,
                entry.Description,
                true
            );

            if (entry.Vendor)
            {
                builder.AddImage(270 + xOffset * 260, 98 + yOffset * 30, 2151);
                builder.AddItem(262 + xOffset * 260, 106 + yOffset * 30, 2543);
            }
        }

        builder.AddButton(340, 400, 4005, 4007, 0, GumpButtonType.Page, currentPage + 1 % _pageCount);
        builder.AddHtmlLocalized(380, 400, 180, 25, 1078456); // More Job Titles

        builder.AddButton(338, 437, 4014, 4016, 1);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_barkeeper.Deleted)
        {
            return;
        }

        var buttonID = info.ButtonID;

        if (buttonID-- <= 0)
        {
            return;
        }

        var from = sender.Mobile;

        if (buttonID-- <= 0)
        {
            _barkeeper.CancelChangeTitle(from);
            return;
        }

        if (buttonID < _entries.Length)
        {
            var entry = _entries[buttonID];
            _barkeeper.EndChangeTitle(entry.Title);
        }
    }

    private class Entry
    {
        public readonly int Description;
        public readonly string Title;
        public readonly bool Vendor;

        public Entry(int desc, string title, bool vendor = false)
        {
            Description = desc;
            Title = title;
            Vendor = vendor;
        }
    }
}
