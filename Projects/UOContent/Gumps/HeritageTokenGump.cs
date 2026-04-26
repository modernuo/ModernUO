using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server.Gumps;

public class HeritageTokenGump : StaticGump<HeritageTokenGump>
{
    private static readonly Dictionary<int, (Type[] Types, int CliLoc, int Tooltip, int ItemID, int Hue, int Width, int Height)> ButtonLookup = new()
    {
        // 7th anniversary
        { 0x64, ([typeof(LeggingsOfEmbers)], 1078147, 1062912, 0x1411, 0x2C, 18, 8) },
        { 0x65, ([typeof(RoseOfTrinsic)], 1062913, 1062914, 0x234D, 0x0, 18, 12) },
        { 0x66, ([typeof(ShaminoCrossbow)], 1062915, 1062916, 0x26C3, 0x504, 18, 8) },
        { 0x67, ([typeof(TapestryOfSosaria)], 1062917, 1062918, 0x3F1D, 0x0, 18, 8) },
        { 0x68, ([typeof(HearthOfHomeFireDeed)], 1062919, 1062920, 0x3F14, 0x0, 18, 8) },
        { 0x69, ([typeof(HolySword)], 1062921, 1062922, 0xF60, 0x482, -1, 10) },
        { 0x6A, ([typeof(SamuraiHelm)], 1062923, 1062924, 0x236C, 0x0, 18, 6) },

        // 8th anniversary
        { 0x6B, ([typeof(SpiritualityHelm)], 1075188, 1075223, 0x2B10, 0x226, 18, 11) },
        { 0x6C, ([typeof(ValorGauntlets)], 1075192, 1075224, 0x2B0C, 0x226, 18, 15) },
        { 0x6D, ([typeof(DupresShield)], 1075196, 1075225, 0x2B01, 0x0, 18, 9) },
        { 0x6E, ([typeof(FountainOfLifeDeed)], 1075197, 1075226, 0x2AC6, 0x0, 29, 0) },
        { 0x6F, ([typeof(DawnsMusicBox)], 1075198, 1075227, 0x2AF9, 0x0, -4, -5) },
        { 0x70, ([typeof(OssianGrimoire)], 1078148, 1075228, 0x2253, 0x0, 18, 12) },
        { 0x71, ([typeof(FerretFormTalisman)], 1078142, 1078527, 0x2D98, 0x0, 19, 13) },
        { 0x72, ([typeof(SquirrelFormTalisman)], 1078143, 1078528, 0x2D97, 0x0, 19, 13) },
        { 0x73, ([typeof(CuSidheFormTalisman)], 1078144, 1078529, 0x2D96, 0x0, 19, 8) },
        { 0x74, ([typeof(ReptalonFormTalisman)], 1078145, 1078530, 0x2D95, 0x0, -4, 2) },
        { 0x75, ([typeof(QuiverOfInfinity)], 1075201, 1078526, 0x2B02, 0x0, -2, 9) },

        // evil home decor
        { 0x76, (
            [typeof(BoneThroneDeed), typeof(BoneCouchDeed), typeof(BoneTableDeed)],
            1074797, 1075986, 0x2A91, 0x0, 25, 5
        ) },
        { 0x77, (
            [typeof(CreepyPortraitDeed), typeof(DisturbingPortraitDeed), typeof(UnsettlingPortraitDeed)],
            1078146, 1075987, 0x2A99, 0x0, 18, 1
        ) },
        { 0x78, (
            [
                typeof(MountedPixieBlueDeed), typeof(MountedPixieGreenDeed), typeof(MountedPixieLimeDeed),
                typeof(MountedPixieOrangeDeed), typeof(MountedPixieWhiteDeed)
            ],
            1074799, 1075988, 0x2A71, 0x0, 13, 5
        ) },
        { 0x79, ([typeof(HaunterMirrorDeed)], 1074800, 1075990, 0x2A98, 0x0, 26, 1) },
        { 0x7A, ([typeof(BedOfNailsDeed)], 1074801, 1075989, 0x2A92, 0x0, 18, 1) },
        { 0x7B, ([typeof(SacrificialAltarDeed)], 1074818, 1075991, 0x2AB8, 0x0, 18, 1) },

        // broken furniture
        { 0x7C, ([typeof(BrokenCoveredChairDeed)], 1076257, 1076610, 0x3F26, 0x0, 18, 8) },
        { 0x7D, ([typeof(BrokenBookcaseDeed)], 1076258, 1076610, 0x3F22, 0x0, 18, 8) },
        { 0x7E, ([typeof(StandingBrokenChairDeed)], 1076259, 1076610, 0x3F24, 0x0, 18, 8) },
        { 0x7F, ([typeof(BrokenVanityDeed)], 1076260, 1076610, 0x3F25, 0x0, 18, 8) },
        { 0x80, ([typeof(BrokenChestOfDrawersDeed)], 1076261, 1076610, 0x3F23, 0x0, 18, 8) },
        { 0x81, ([typeof(BrokenArmoireDeed)], 1076262, 1076610, 0x3F21, 0x0, 18, 8) },
        { 0x82, ([typeof(BrokenBedDeed)], 1076263, 1076610, 0x3F0B, 0x0, 18, 8) },
        { 0x83, ([typeof(BrokenFallenChairDeed)], 1076264, 1076610, 0xC19, 0x0, 13, 8) },

        // other
        { 0x84, ([typeof(SuitOfGoldArmorDeed)], 1076265, 1076611, 0x3DAA, 0x0, 20, -3) },
        { 0x85, ([typeof(SuitOfSilverArmorDeed)], 1076266, 1076612, 0x151C, 0x0, -20, -3) },
        { 0x86, ([typeof(BoilingCauldronDeed)], 1076267, 1076613, 0x3DB1, 0x0, 18, 8) },
        { 0x87, ([typeof(GuillotineDeed)], 1024656, 1076614, 0x3F27, 0x0, 18, 8) },
        { 0x88, ([typeof(CherryBlossomTreeDeed)], 1076268, 1076615, 0x3F0C, 0x0, 18, 8) },
        { 0x89, ([typeof(AppleTreeDeed)], 1076269, 1076616, 0x3F07, 0x0, 18, 8) },
        { 0x8A, ([typeof(PeachTreeDeed)], 1076270, 1076617, 0x3F16, 0x0, 18, 8) },
        { 0x8B, ([typeof(HangingAxesDeed)], 1076271, 1076618, 0x3F12, 0x0, 18, 8) },
        { 0x8C, ([typeof(HangingSwordsDeed)], 1076272, 1076619, 0x3F13, 0x0, 18, 8) },
        { 0x8D, ([typeof(BlueFancyRugDeed)], 1076273, 1076620, 0x3F09, 0x0, 18, 8) },
        { 0x8E, ([typeof(WoodenCoffinDeed)], 1076274, 1076621, 0x3F0E, 0x0, 18, 8) },
        { 0x8F, ([typeof(VanityDeed)], 1074027, 1076623, 0x3F1F, 0x0, 18, 8) },
        { 0x90, ([typeof(TableWithPurpleClothDeed)], 1076635, 1076624, 0x118B, 0x0, -4, -9) },
        { 0x91, ([typeof(TableWithBlueClothDeed)], 1076636, 1076624, 0x118C, 0x0, -4, -9) },
        { 0x92, ([typeof(TableWithRedClothDeed)], 1076637, 1076624, 0x118D, 0x0, -4, -9) },
        { 0x93, ([typeof(TableWithOrangeClothDeed)], 1076638, 1076624, 0x118E, 0x0, -4, -9) },
        { 0x94, ([typeof(UnmadeBedDeed)], 1076279, 1076625, 0x3F1E, 0x0, 18, 8) },
        { 0x95, ([typeof(CurtainsDeed)], 1076280, 1076626, 0x3F0F, 0x0, 18, 8) },
        { 0x96, ([typeof(ScarecrowDeed)], 1076281, 1076627, 0x1E34, 0x0, 18, -17) },
        { 0x97, ([typeof(WallTorchDeed)], 1076282, 1076628, 0xA0C, 0x0, 18, 8) },
        { 0x98, ([typeof(FountainDeed)], 1076283, 1076629, 0x3F10, 0x0, 18, 9) },
        { 0x99, ([typeof(StoneStatueDeed)], 1076284, 1076630, 0x3F19, 0x0, 18, 8) },
        { 0x9A, ([typeof(LargeFishingNetDeed)], 1076285, 1076631, 0x1EA5, 0x0, 5, -25) },
        { 0x9B, ([typeof(SmallFishingNetDeed)], 1076286, 1076632, 0x1EA3, 0x0, 18, -27) },
        { 0x9C, ([typeof(HouseLadderDeed)], 1076287, 1076633, 0x2FDF, 0x0, 18, -36) },
        { 0x9D, ([typeof(IronMaidenDeed)], 1076288, 1076622, 0x3F15, 0x0, 18, 8) },
        { 0x9E, ([typeof(BluePlainRugDeed)], 1076585, 1076620, 0x3F0A, 0x0, 18, 8) },
        { 0x9F, ([typeof(GoldenDecorativeRugDeed)], 1076586, 1076620, 0x3F11, 0x0, 18, 8) },
        { 0xA0, ([typeof(CinnamonFancyRugDeed)], 1076587, 1076620, 0x3F0D, 0x0, 18, 8) },
        { 0xA1, ([typeof(RedPlainRugDeed)], 1076588, 1076620, 0x3F18, 0x0, 18, 8) },
        { 0xA2, ([typeof(BlueDecorativeRugDeed)], 1076589, 1076620, 0x3F08, 0x0, 18, 8) },
        { 0xA3, ([typeof(PinkFancyRugDeed)], 1076590, 1076620, 0x3F17, 0x0, 18, 8) },
        { 0xA4, ([typeof(CherryBlossomTrunkDeed)], 1076784, 1076615, 0x312A, 0x0, 18, 8) },
        { 0xA5, ([typeof(AppleTrunkDeed)], 1076785, 1076616, 0x3128, 0x0, 18, 8) },
        { 0xA6, ([typeof(PeachTrunkDeed)], 1076786, 1076617, 0x3129, 0x0, 18, 8) }
    };

    private readonly HeritageToken _token;

    public override bool Singleton => true;

    private HeritageTokenGump(HeritageToken token) : base(60, 36) => _token = token;

    public static void DisplayTo(Mobile from, HeritageToken token)
    {
        if (from?.NetState == null || token?.Deleted != false)
        {
            return;
        }

        from.SendGump(new HeritageTokenGump(token));
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        // Standard static framing
        builder.AddBackground(0, 0, 520, 404, 0x13BE);
        builder.AddImageTiled(10, 10, 500, 20, 0xA40);
        builder.AddImageTiled(10, 40, 500, 324, 0xA40);
        builder.AddImageTiled(10, 374, 500, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 500, 384);
        builder.AddButton(10, 374, 0xFB1, 0xFB2, 0);
        builder.AddHtmlLocalized(45, 376, 450, 20, 1060051, 0x7FFF); // CANCEL
        builder.AddHtmlLocalized(14, 12, 500, 20, 1075576, 0x7FFF);  // Choose your item from the following pages

        const int itemsPerPage = 10;
        int totalItems = ButtonLookup.Count;
        int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

        int index = 0;
        int page = 1;

        foreach (var (buttonId, (_, cliloc, tooltip, itemId, hue, width, height)) in ButtonLookup)
        {
            // When index cleanly divides by 10, we are starting a new page
            if (index % itemsPerPage == 0)
            {
                builder.AddPage(page);

                // Add Next Button
                if (page < totalPages)
                {
                    builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page + 1);
                    builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next
                }

                // Add Back Button
                if (page > 1)
                {
                    builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, page - 1);
                    builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
                }
            }

            // Calculate dynamic grid placement (2 columns, 5 rows)
            int posOnPage = index % itemsPerPage;
            int col = posOnPage % 2; // 0 for left, 1 for right
            int row = posOnPage / 2; // 0 through 4

            int xBase = col == 0 ? 14 : 264;
            int yBase = 44 + (row * 64);
            int textX = col == 0 ? 98 : 348;

            // Render the UI entries
            builder.AddImageTiledButton(
                xBase,
                yBase,
                0x918,
                0x919,
                buttonId,
                GumpButtonType.Reply,
                0,
                itemId,
                hue,
                width,
                height
            );

            if (tooltip > 0)
            {
                builder.AddTooltip(tooltip);
            }

            builder.AddHtmlLocalized(textX, yBase, 250, 60, cliloc, 0x7FFF);

            index++;

            // Bump to the next page when we fill the current one
            if (index % itemsPerPage == 0)
            {
                page++;
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_token?.Deleted != false || info.ButtonID == 0)
        {
            return;
        }

        if (!ButtonLookup.TryGetValue(info.ButtonID, out var buttonInfo))
        {
            return;
        }

        var (types, cliloc, _, _, _, _, _) = buttonInfo;

        if (types?.Length > 0 && cliloc > 0)
        {
            ConfirmHeritageGump.DisplayTo(sender.Mobile, _token, types, cliloc);
        }
        else
        {
            // This option is currently disabled, while we evaluate it for game balance.
            sender.Mobile.SendLocalizedMessage(501311);
        }
    }
}
