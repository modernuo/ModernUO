using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps
{
    public class HeritageTokenGump : StaticGump<HeritageTokenGump>
    {
        private readonly HeritageToken _token;

        public override bool Singleton => true;

        private HeritageTokenGump(HeritageToken token) : base(60, 36) => _token = token;

        public static void DisplayTo(Mobile from, HeritageToken token)
        {
            if (from?.NetState == null || token?.Deleted != false)
            {
                return;
            }

            from.SendGump(new HeritageTokenGump(token), true);
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 520, 404, 0x13BE);
            builder.AddImageTiled(10, 10, 500, 20, 0xA40);
            builder.AddImageTiled(10, 40, 500, 324, 0xA40);
            builder.AddImageTiled(10, 374, 500, 20, 0xA40);
            builder.AddAlphaRegion(10, 10, 500, 384);
            builder.AddButton(10, 374, 0xFB1, 0xFB2, 0);
            builder.AddHtmlLocalized(45, 376, 450, 20, 1060051, 0x7FFF); // CANCEL
            builder.AddHtmlLocalized(14, 12, 500, 20, 1075576, 0x7FFF);  // Choose your item from the following pages

            builder.AddPage(1);

            builder.AddImageTiledButton(14, 44, 0x918, 0x919, 0x64, GumpButtonType.Reply, 0, 0x1411, 0x2C, 18, 8);
            builder.AddTooltip(1062912);
            builder.AddHtmlLocalized(98, 44, 250, 60, 1078147, 0x7FFF); // Royal Leggings of Embers
            builder.AddImageTiledButton(264, 44, 0x918, 0x919, 0x65, GumpButtonType.Reply, 0, 0x234D, 0x0, 18, 12);
            builder.AddTooltip(1062914);
            builder.AddHtmlLocalized(348, 44, 250, 60, 1062913, 0x7FFF); // Rose of Trinsic
            builder.AddImageTiledButton(14, 108, 0x918, 0x919, 0x66, GumpButtonType.Reply, 0, 0x26C3, 0x504, 18, 8);
            builder.AddTooltip(1062916);
            builder.AddHtmlLocalized(98, 108, 250, 60, 1062915, 0x7FFF); // Shamino’s Best Crossbow
            builder.AddImageTiledButton(264, 108, 0x918, 0x919, 0x67, GumpButtonType.Reply, 0, 0x3F1D, 0x0, 18, 8);
            builder.AddTooltip(1062918);
            builder.AddHtmlLocalized(348, 108, 250, 60, 1062917, 0x7FFF); // The Tapestry of Sosaria
            builder.AddImageTiledButton(14, 172, 0x918, 0x919, 0x68, GumpButtonType.Reply, 0, 0x3F14, 0x0, 18, 8);
            builder.AddTooltip(1062920);
            builder.AddHtmlLocalized(98, 172, 250, 60, 1062919, 0x7FFF); // Hearth of the Home Fire
            builder.AddImageTiledButton(264, 172, 0x918, 0x919, 0x69, GumpButtonType.Reply, 0, 0xF60, 0x482, -1, 10);
            builder.AddTooltip(1062922);
            builder.AddHtmlLocalized(348, 172, 250, 60, 1062921, 0x7FFF); // The Holy Sword
            builder.AddImageTiledButton(14, 236, 0x918, 0x919, 0x6A, GumpButtonType.Reply, 0, 0x236C, 0x0, 18, 6);
            builder.AddTooltip(1062924);
            builder.AddHtmlLocalized(98, 236, 250, 60, 1062923, 0x7FFF); // Ancient Samurai Helm
            builder.AddImageTiledButton(264, 236, 0x918, 0x919, 0x6B, GumpButtonType.Reply, 0, 0x2B10, 0x226, 18, 11);
            builder.AddTooltip(1075223);
            builder.AddHtmlLocalized(348, 236, 250, 60, 1075188, 0x7FFF); // Helm of Spirituality
            builder.AddImageTiledButton(14, 300, 0x918, 0x919, 0x6C, GumpButtonType.Reply, 0, 0x2B0C, 0x226, 18, 15);
            builder.AddTooltip(1075224);
            builder.AddHtmlLocalized(98, 300, 250, 60, 1075192, 0x7FFF); // Gauntlets of Valor
            builder.AddImageTiledButton(264, 300, 0x918, 0x919, 0x6D, GumpButtonType.Reply, 0, 0x2B01, 0x0, 18, 9);
            builder.AddTooltip(1075225);
            builder.AddHtmlLocalized(348, 300, 250, 60, 1075196, 0x7FFF); // Dupre’s Shield
            builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 2);
            builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next

            builder.AddPage(2);

            builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);
            builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
            builder.AddImageTiledButton(14, 44, 0x918, 0x919, 0x6E, GumpButtonType.Reply, 0, 0x2AC6, 0x0, 29, 0);
            builder.AddTooltip(1075226);
            builder.AddHtmlLocalized(98, 44, 250, 60, 1075197, 0x7FFF); // Fountain of Life
            builder.AddImageTiledButton(264, 44, 0x918, 0x919, 0x6F, GumpButtonType.Reply, 0, 0x2AF9, 0x0, -4, -5);
            builder.AddTooltip(1075227);
            builder.AddHtmlLocalized(348, 44, 250, 60, 1075198, 0x7FFF); // Dawn’s Music Box
            builder.AddImageTiledButton(14, 108, 0x918, 0x919, 0x70, GumpButtonType.Reply, 0, 0x2253, 0x0, 18, 12);
            builder.AddTooltip(1075228);
            builder.AddHtmlLocalized(98, 108, 250, 60, 1078148, 0x7FFF); // Ossian Grimoire
            builder.AddImageTiledButton(264, 108, 0x918, 0x919, 0x71, GumpButtonType.Reply, 0, 0x2D98, 0x0, 19, 13);
            builder.AddTooltip(1078527);
            builder.AddHtmlLocalized(348, 108, 250, 60, 1078142, 0x7FFF); // Talisman of the Fey:<br>Ferret
            builder.AddImageTiledButton(14, 172, 0x918, 0x919, 0x72, GumpButtonType.Reply, 0, 0x2D97, 0x0, 19, 13);
            builder.AddTooltip(1078528);
            builder.AddHtmlLocalized(98, 172, 250, 60, 1078143, 0x7FFF); // Talisman of the Fey:<br>Squirrel
            builder.AddImageTiledButton(264, 172, 0x918, 0x919, 0x73, GumpButtonType.Reply, 0, 0x2D96, 0x0, 19, 8);
            builder.AddTooltip(1078529);
            builder.AddHtmlLocalized(348, 172, 250, 60, 1078144, 0x7FFF); // Talisman of the Fey:<br>Cu Sidhe
            builder.AddImageTiledButton(14, 236, 0x918, 0x919, 0x74, GumpButtonType.Reply, 0, 0x2D95, 0x0, -4, 2);
            builder.AddTooltip(1078530);
            builder.AddHtmlLocalized(98, 236, 250, 60, 1078145, 0x7FFF); // Talisman of the Fey:<br>Reptalon
            builder.AddImageTiledButton(264, 236, 0x918, 0x919, 0x75, GumpButtonType.Reply, 0, 0x2B02, 0x0, -2, 9);
            builder.AddTooltip(1078526);
            builder.AddHtmlLocalized(348, 236, 250, 60, 1075201, 0x7FFF); // Quiver of Infinity
            builder.AddImageTiledButton(14, 300, 0x918, 0x919, 0x76, GumpButtonType.Reply, 0, 0x2A91, 0x0, 25, 5);
            builder.AddTooltip(1075986);
            builder.AddHtmlLocalized(98, 300, 250, 60, 1074797, 0x7FFF); // Bone Throne, Bone Couch<br>and Bone Table
            builder.AddImageTiledButton(264, 300, 0x918, 0x919, 0x77, GumpButtonType.Reply, 0, 0x2A99, 0x0, 18, 1);
            builder.AddTooltip(1075987);
            builder.AddHtmlLocalized(348, 300, 250, 60, 1078146, 0x7FFF); // Creepy Portraits
            builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 3);
            builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next

            builder.AddPage(3);

            builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 2);
            builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
            builder.AddImageTiledButton(14, 44, 0x918, 0x919, 0x78, GumpButtonType.Reply, 0, 0x2A71, 0x0, 13, 5);
            builder.AddTooltip(1075988);
            builder.AddHtmlLocalized(98, 44, 250, 60, 1074799, 0x7FFF); // Mounted Pixies (5)
            builder.AddImageTiledButton(264, 44, 0x918, 0x919, 0x79, GumpButtonType.Reply, 0, 0x2A98, 0x0, 26, 1);
            builder.AddTooltip(1075990);
            builder.AddHtmlLocalized(348, 44, 250, 60, 1074800, 0x7FFF); // Haunted Mirror
            builder.AddImageTiledButton(14, 108, 0x918, 0x919, 0x7A, GumpButtonType.Reply, 0, 0x2A92, 0x0, 18, 1);
            builder.AddTooltip(1075989);
            builder.AddHtmlLocalized(98, 108, 250, 60, 1074801, 0x7FFF); // Bed of Nails
            builder.AddImageTiledButton(264, 108, 0x918, 0x919, 0x7B, GumpButtonType.Reply, 0, 0x2AB8, 0x0, 18, 1);
            builder.AddTooltip(1075991);
            builder.AddHtmlLocalized(348, 108, 250, 60, 1074818, 0x7FFF); // Sacrificial Altar
            builder.AddImageTiledButton(14, 172, 0x918, 0x919, 0x7C, GumpButtonType.Reply, 0, 0x3F26, 0x0, 18, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(98, 172, 250, 60, 1076257, 0x7FFF); // Broken Covered Chair
            builder.AddImageTiledButton(264, 172, 0x918, 0x919, 0x7D, GumpButtonType.Reply, 0, 0x3F22, 0x0, 18, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(348, 172, 250, 60, 1076258, 0x7FFF); // Broken Bookcase
            builder.AddImageTiledButton(14, 236, 0x918, 0x919, 0x7E, GumpButtonType.Reply, 0, 0x3F24, 0x0, 18, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(98, 236, 250, 60, 1076259, 0x7FFF); // Standing Broken Chair
            builder.AddImageTiledButton(264, 236, 0x918, 0x919, 0x7F, GumpButtonType.Reply, 0, 0x3F25, 0x0, 18, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(348, 236, 250, 60, 1076260, 0x7FFF); // Broken Vanity
            builder.AddImageTiledButton(14, 300, 0x918, 0x919, 0x80, GumpButtonType.Reply, 0, 0x3F23, 0x0, 18, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(98, 300, 250, 60, 1076261, 0x7FFF); // Broken Chest of Drawers
            builder.AddImageTiledButton(264, 300, 0x918, 0x919, 0x81, GumpButtonType.Reply, 0, 0x3F21, 0x0, 18, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(348, 300, 250, 60, 1076262, 0x7FFF); // Broken Armoire
            builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 4);
            builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next

            builder.AddPage(4);

            builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 3);
            builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
            builder.AddImageTiledButton(14, 44, 0x918, 0x919, 0x82, GumpButtonType.Reply, 0, 0x3F0B, 0x0, 18, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(98, 44, 250, 60, 1076263, 0x7FFF); // Broken Bed
            builder.AddImageTiledButton(264, 44, 0x918, 0x919, 0x83, GumpButtonType.Reply, 0, 0xC19, 0x0, 13, 8);
            builder.AddTooltip(1076610);
            builder.AddHtmlLocalized(348, 44, 250, 60, 1076264, 0x7FFF); // Broken Fallen Chair
            builder.AddImageTiledButton(14, 108, 0x918, 0x919, 0x84, GumpButtonType.Reply, 0, 0x3DAA, 0x0, 20, -3);
            builder.AddTooltip(1076611);
            builder.AddHtmlLocalized(98, 108, 250, 60, 1076265, 0x7FFF); // Suit of Gold Armor
            builder.AddImageTiledButton(264, 108, 0x918, 0x919, 0x85, GumpButtonType.Reply, 0, 0x151C, 0x0, -20, -3);
            builder.AddTooltip(1076612);
            builder.AddHtmlLocalized(348, 108, 250, 60, 1076266, 0x7FFF); // Suit of Silver Armor
            builder.AddImageTiledButton(14, 172, 0x918, 0x919, 0x86, GumpButtonType.Reply, 0, 0x3DB1, 0x0, 18, 8);
            builder.AddTooltip(1076613);
            builder.AddHtmlLocalized(98, 172, 250, 60, 1076267, 0x7FFF); // Boiling Cauldron
            builder.AddImageTiledButton(264, 172, 0x918, 0x919, 0x87, GumpButtonType.Reply, 0, 0x3F27, 0x0, 18, 8);
            builder.AddTooltip(1076614);
            builder.AddHtmlLocalized(348, 172, 250, 60, 1024656, 0x7FFF); // Guillotine
            builder.AddImageTiledButton(14, 236, 0x918, 0x919, 0x88, GumpButtonType.Reply, 0, 0x3F0C, 0x0, 18, 8);
            builder.AddTooltip(1076615);
            builder.AddHtmlLocalized(98, 236, 250, 60, 1076268, 0x7FFF); // Cherry Blossom Tree
            builder.AddImageTiledButton(264, 236, 0x918, 0x919, 0x89, GumpButtonType.Reply, 0, 0x3F07, 0x0, 18, 8);
            builder.AddTooltip(1076616);
            builder.AddHtmlLocalized(348, 236, 250, 60, 1076269, 0x7FFF); // Apple Tree
            builder.AddImageTiledButton(14, 300, 0x918, 0x919, 0x8A, GumpButtonType.Reply, 0, 0x3F16, 0x0, 18, 8);
            builder.AddTooltip(1076617);
            builder.AddHtmlLocalized(98, 300, 250, 60, 1076270, 0x7FFF); // Peach Tree
            builder.AddImageTiledButton(264, 300, 0x918, 0x919, 0x8B, GumpButtonType.Reply, 0, 0x3F12, 0x0, 18, 8);
            builder.AddTooltip(1076618);
            builder.AddHtmlLocalized(348, 300, 250, 60, 1076271, 0x7FFF); // Hanging Axes
            builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 5);
            builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next

            builder.AddPage(5);

            builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 4);
            builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
            builder.AddImageTiledButton(14, 44, 0x918, 0x919, 0x8C, GumpButtonType.Reply, 0, 0x3F13, 0x0, 18, 8);
            builder.AddTooltip(1076619);
            builder.AddHtmlLocalized(98, 44, 250, 60, 1076272, 0x7FFF); // Hanging Swords
            builder.AddImageTiledButton(264, 44, 0x918, 0x919, 0x8D, GumpButtonType.Reply, 0, 0x3F09, 0x0, 18, 8);
            builder.AddTooltip(1076620);
            builder.AddHtmlLocalized(348, 44, 250, 60, 1076273, 0x7FFF); // Blue fancy rug
            builder.AddImageTiledButton(14, 108, 0x918, 0x919, 0x8E, GumpButtonType.Reply, 0, 0x3F0E, 0x0, 18, 8);
            builder.AddTooltip(1076621);
            builder.AddHtmlLocalized(98, 108, 250, 60, 1076274, 0x7FFF); // Coffin
            builder.AddImageTiledButton(264, 108, 0x918, 0x919, 0x8F, GumpButtonType.Reply, 0, 0x3F1F, 0x0, 18, 8);
            builder.AddTooltip(1076623);
            builder.AddHtmlLocalized(348, 108, 250, 60, 1074027, 0x7FFF); // Vanity
            builder.AddImageTiledButton(14, 172, 0x918, 0x919, 0x90, GumpButtonType.Reply, 0, 0x118B, 0x0, -4, -9);
            builder.AddTooltip(1076624);
            builder.AddHtmlLocalized(98, 172, 250, 60, 1076635, 0x7FFF); // Table With A Purple<br>Tablecloth
            builder.AddImageTiledButton(264, 172, 0x918, 0x919, 0x91, GumpButtonType.Reply, 0, 0x118C, 0x0, -4, -9);
            builder.AddTooltip(1076624);
            builder.AddHtmlLocalized(348, 172, 250, 60, 1076636, 0x7FFF); // Table With A Blue<br>Tablecloth
            builder.AddImageTiledButton(14, 236, 0x918, 0x919, 0x92, GumpButtonType.Reply, 0, 0x118D, 0x0, -4, -9);
            builder.AddTooltip(1076624);
            builder.AddHtmlLocalized(98, 236, 250, 60, 1076637, 0x7FFF); // Table With A Red<br>Tablecloth
            builder.AddImageTiledButton(264, 236, 0x918, 0x919, 0x93, GumpButtonType.Reply, 0, 0x118E, 0x0, -4, -9);
            builder.AddTooltip(1076624);
            builder.AddHtmlLocalized(348, 236, 250, 60, 1076638, 0x7FFF); // Table With An Orange<br>Tablecloth
            builder.AddImageTiledButton(14, 300, 0x918, 0x919, 0x94, GumpButtonType.Reply, 0, 0x3F1E, 0x0, 18, 8);
            builder.AddTooltip(1076625);
            builder.AddHtmlLocalized(98, 300, 250, 60, 1076279, 0x7FFF); // Unmade Bed
            builder.AddImageTiledButton(264, 300, 0x918, 0x919, 0x95, GumpButtonType.Reply, 0, 0x3F0F, 0x0, 18, 8);
            builder.AddTooltip(1076626);
            builder.AddHtmlLocalized(348, 300, 250, 60, 1076280, 0x7FFF); // Curtains
            builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 6);
            builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next

            builder.AddPage(6);

            builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 5);
            builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
            builder.AddImageTiledButton(14, 44, 0x918, 0x919, 0x96, GumpButtonType.Reply, 0, 0x1E34, 0x0, 18, -17);
            builder.AddTooltip(1076627);
            builder.AddHtmlLocalized(98, 44, 250, 60, 1076281, 0x7FFF); // Scarecrow
            builder.AddImageTiledButton(264, 44, 0x918, 0x919, 0x97, GumpButtonType.Reply, 0, 0xA0C, 0x0, 18, 8);
            builder.AddTooltip(1076628);
            builder.AddHtmlLocalized(348, 44, 250, 60, 1076282, 0x7FFF); // Wall Torch
            builder.AddImageTiledButton(14, 108, 0x918, 0x919, 0x98, GumpButtonType.Reply, 0, 0x3F10, 0x0, 18, 9);
            builder.AddTooltip(1076629);
            builder.AddHtmlLocalized(98, 108, 250, 60, 1076283, 0x7FFF); // Fountain
            builder.AddImageTiledButton(264, 108, 0x918, 0x919, 0x99, GumpButtonType.Reply, 0, 0x3F19, 0x0, 18, 8);
            builder.AddTooltip(1076630);
            builder.AddHtmlLocalized(348, 108, 250, 60, 1076284, 0x7FFF); // Statue
            builder.AddImageTiledButton(14, 172, 0x918, 0x919, 0x9A, GumpButtonType.Reply, 0, 0x1EA5, 0x0, 5, -25);
            builder.AddTooltip(1076631);
            builder.AddHtmlLocalized(98, 172, 250, 60, 1076285, 0x7FFF); // Large Fish Net
            builder.AddImageTiledButton(264, 172, 0x918, 0x919, 0x9B, GumpButtonType.Reply, 0, 0x1EA3, 0x0, 18, -27);
            builder.AddTooltip(1076632);
            builder.AddHtmlLocalized(348, 172, 250, 60, 1076286, 0x7FFF); // Small Fish Net
            builder.AddImageTiledButton(14, 236, 0x918, 0x919, 0x9C, GumpButtonType.Reply, 0, 0x2FDF, 0x0, 18, -36);
            builder.AddTooltip(1076633);
            builder.AddHtmlLocalized(98, 236, 250, 60, 1076287, 0x7FFF); // Ladder
            builder.AddImageTiledButton(264, 236, 0x918, 0x919, 0x9D, GumpButtonType.Reply, 0, 0x3F15, 0x0, 18, 8);
            builder.AddTooltip(1076622);
            builder.AddHtmlLocalized(348, 236, 250, 60, 1076288, 0x7FFF); // Iron Maiden
            builder.AddImageTiledButton(14, 300, 0x918, 0x919, 0x9E, GumpButtonType.Reply, 0, 0x3F0A, 0x0, 18, 8);
            builder.AddTooltip(1076620);
            builder.AddHtmlLocalized(98, 300, 250, 60, 1076585, 0x7FFF); // Blue plain rug
            builder.AddImageTiledButton(264, 300, 0x918, 0x919, 0x9F, GumpButtonType.Reply, 0, 0x3F11, 0x0, 18, 8);
            builder.AddTooltip(1076620);
            builder.AddHtmlLocalized(348, 300, 250, 60, 1076586, 0x7FFF); // Golden decorative rug
            builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 7);
            builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next

            builder.AddPage(7);

            builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 6);
            builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
            builder.AddImageTiledButton(14, 44, 0x918, 0x919, 0xA0, GumpButtonType.Reply, 0, 0x3F0D, 0x0, 18, 8);
            builder.AddTooltip(1076620);
            builder.AddHtmlLocalized(98, 44, 250, 60, 1076587, 0x7FFF); // Cinnamon fancy rug
            builder.AddImageTiledButton(264, 44, 0x918, 0x919, 0xA1, GumpButtonType.Reply, 0, 0x3F18, 0x0, 18, 8);
            builder.AddTooltip(1076620);
            builder.AddHtmlLocalized(348, 44, 250, 60, 1076588, 0x7FFF); // Red plain rug
            builder.AddImageTiledButton(14, 108, 0x918, 0x919, 0xA2, GumpButtonType.Reply, 0, 0x3F08, 0x0, 18, 8);
            builder.AddTooltip(1076620);
            builder.AddHtmlLocalized(98, 108, 250, 60, 1076589, 0x7FFF); // Blue decorative rug
            builder.AddImageTiledButton(264, 108, 0x918, 0x919, 0xA3, GumpButtonType.Reply, 0, 0x3F17, 0x0, 18, 8);
            builder.AddTooltip(1076620);
            builder.AddHtmlLocalized(348, 108, 250, 60, 1076590, 0x7FFF); // Pink fancy rug
            builder.AddImageTiledButton(14, 172, 0x918, 0x919, 0xA4, GumpButtonType.Reply, 0, 0x312A, 0x0, 18, 8);
            builder.AddTooltip(1076615);
            builder.AddHtmlLocalized(98, 172, 250, 60, 1076784, 0x7FFF); // Cherry Blossom Trunk
            builder.AddImageTiledButton(264, 172, 0x918, 0x919, 0xA5, GumpButtonType.Reply, 0, 0x3128, 0x0, 18, 8);
            builder.AddTooltip(1076616);
            builder.AddHtmlLocalized(348, 172, 250, 60, 1076785, 0x7FFF); // Apple Trunk
            builder.AddImageTiledButton(14, 236, 0x918, 0x919, 0xA6, GumpButtonType.Reply, 0, 0x3129, 0x0, 18, 8);
            builder.AddTooltip(1076617);
            builder.AddHtmlLocalized(98, 236, 250, 60, 1076786, 0x7FFF); // Peach Trunk
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_token?.Deleted != false || info.ButtonID == 0)
            {
                return;
            }

            Type[] types;
            int cliloc;

            switch (info.ButtonID)
            {
                default:
                    {
                        types = null;
                        cliloc = 0;
                        break;
                    }
                // 7th anniversary
                case 0x64:
                    {
                        types = [typeof(LeggingsOfEmbers)];
                        cliloc = 1078147;
                        break;
                    }
                case 0x65:
                    {
                        types = [typeof(RoseOfTrinsic)];
                        cliloc = 1062913;
                        break;
                    }
                case 0x66:
                    {
                        types = [typeof(ShaminoCrossbow)];
                        cliloc = 1062915;
                        break;
                    }
                case 0x67:
                    {
                        types = [typeof(TapestryOfSosaria)];
                        cliloc = 1062917;
                        break;
                    }
                case 0x68:
                    {
                        types = [typeof(HearthOfHomeFireDeed)];
                        cliloc = 1062919;
                        break;
                    }
                case 0x69:
                    {
                        types = [typeof(HolySword)];
                        cliloc = 1062921;
                        break;
                    }
                case 0x6A:
                    {
                        types = [typeof(SamuraiHelm)];
                        cliloc = 1062923;
                        break;
                    }

                // 8th anniversary
                /*case 0x6B: types = new [] {  typeof( SpiritualityHelm )  }; cliloc = 1075188; break;
                case 0x6C: types = new [] {  typeof( ValorGauntlets )  }; cliloc = 1075192; break;*/
                case 0x6D:
                    {
                        types = [typeof(DupresShield)];
                        cliloc = 1075196;
                        break;
                    }
                case 0x6E:
                    {
                        types = [typeof(FountainOfLifeDeed)];
                        cliloc = 1075197;
                        break;
                    }
                case 0x6F:
                    {
                        types = [typeof(DawnsMusicBox)];
                        cliloc = 1075198;
                        break;
                    }
                case 0x70:
                    {
                        types = [typeof(OssianGrimoire)];
                        cliloc = 1078148;
                        break;
                    }
                case 0x71:
                    {
                        types = [typeof(FerretFormTalisman)];
                        cliloc = 1078142;
                        break;
                    }
                case 0x72:
                    {
                        types = [typeof(SquirrelFormTalisman)];
                        cliloc = 1078143;
                        break;
                    }
                case 0x73:
                    {
                        types = [typeof(CuSidheFormTalisman)];
                        cliloc = 1078144;
                        break;
                    }
                case 0x74:
                    {
                        types = [typeof(ReptalonFormTalisman)];
                        cliloc = 1078145;
                        break;
                    }
                case 0x75:
                    {
                        types = [typeof(QuiverOfInfinity)];
                        cliloc = 1075201;
                        break;
                    }

                // evil home decor
                case 0x76:
                    {
                        types = [typeof(BoneThroneDeed), typeof(BoneCouchDeed), typeof(BoneTableDeed)];
                        cliloc = 1074797;
                        break;
                    }
                case 0x77:
                    {
                        types =
                        [
                            typeof(CreepyPortraitDeed),
                            typeof(DisturbingPortraitDeed),
                            typeof(UnsettlingPortraitDeed)
                        ];
                        cliloc = 1078146;
                        break;
                    }
                case 0x78:
                    {
                        types =
                        [
                            typeof(MountedPixieBlueDeed),
                            typeof(MountedPixieGreenDeed),
                            typeof(MountedPixieLimeDeed),
                            typeof(MountedPixieOrangeDeed),
                            typeof(MountedPixieWhiteDeed)
                        ];
                        cliloc = 1074799;
                        break;
                    }
                case 0x79:
                    {
                        types = [typeof(HaunterMirrorDeed)];
                        cliloc = 1074800;
                        break;
                    }
                case 0x7A:
                    {
                        types = [typeof(BedOfNailsDeed)];
                        cliloc = 1074801;
                        break;
                    }
                case 0x7B:
                    {
                        types = [typeof(SacrificialAltarDeed)];
                        cliloc = 1074818;
                        break;
                    }

                // broken furniture
                case 0x7C:
                    {
                        types = [typeof(BrokenCoveredChairDeed)];
                        cliloc = 1076257;
                        break;
                    }
                case 0x7D:
                    {
                        types = [typeof(BrokenBookcaseDeed)];
                        cliloc = 1076258;
                        break;
                    }
                case 0x7E:
                    {
                        types = [typeof(StandingBrokenChairDeed)];
                        cliloc = 1076259;
                        break;
                    }
                case 0x7F:
                    {
                        types = [typeof(BrokenVanityDeed)];
                        cliloc = 1076260;
                        break;
                    }
                case 0x80:
                    {
                        types = [typeof(BrokenChestOfDrawersDeed)];
                        cliloc = 1076261;
                        break;
                    }
                case 0x81:
                    {
                        types = [typeof(BrokenArmoireDeed)];
                        cliloc = 1076262;
                        break;
                    }
                case 0x82:
                    {
                        types = [typeof(BrokenBedDeed)];
                        cliloc = 1076263;
                        break;
                    }
                case 0x83:
                    {
                        types = [typeof(BrokenFallenChairDeed)];
                        cliloc = 1076264;
                        break;
                    }

                // other
                case 0x84:
                    {
                        types = [typeof(SuitOfGoldArmorDeed)];
                        cliloc = 1076265;
                        break;
                    }
                case 0x85:
                    {
                        types = [typeof(SuitOfSilverArmorDeed)];
                        cliloc = 1076266;
                        break;
                    }
                case 0x86:
                    {
                        types = [typeof(BoilingCauldronDeed)];
                        cliloc = 1076267;
                        break;
                    }
                case 0x87:
                    {
                        types = [typeof(GuillotineDeed)];
                        cliloc = 1024656;
                        break;
                    }
                case 0x88:
                    {
                        types = [typeof(CherryBlossomTreeDeed)];
                        cliloc = 1076268;
                        break;
                    }
                case 0x89:
                    {
                        types = [typeof(AppleTreeDeed)];
                        cliloc = 1076269;
                        break;
                    }
                case 0x8A:
                    {
                        types = [typeof(PeachTreeDeed)];
                        cliloc = 1076270;
                        break;
                    }
                case 0x8B:
                    {
                        types = [typeof(HangingAxesDeed)];
                        cliloc = 1076271;
                        break;
                    }
                case 0x8C:
                    {
                        types = [typeof(HangingSwordsDeed)];
                        cliloc = 1076272;
                        break;
                    }
                case 0x8D:
                    {
                        types = [typeof(BlueFancyRugDeed)];
                        cliloc = 1076273;
                        break;
                    }
                case 0x8E:
                    {
                        types = [typeof(WoodenCoffinDeed)];
                        cliloc = 1076274;
                        break;
                    }
                case 0x8F:
                    {
                        types = [typeof(VanityDeed)];
                        cliloc = 1074027;
                        break;
                    }
                case 0x90:
                    {
                        types = [typeof(TableWithPurpleClothDeed)];
                        cliloc = 1076635;
                        break;
                    }
                case 0x91:
                    {
                        types = [typeof(TableWithBlueClothDeed)];
                        cliloc = 1076636;
                        break;
                    }
                case 0x92:
                    {
                        types = [typeof(TableWithRedClothDeed)];
                        cliloc = 1076637;
                        break;
                    }
                case 0x93:
                    {
                        types = [typeof(TableWithOrangeClothDeed)];
                        cliloc = 1076638;
                        break;
                    }
                case 0x94:
                    {
                        types = [typeof(UnmadeBedDeed)];
                        cliloc = 1076279;
                        break;
                    }
                case 0x95:
                    {
                        types = [typeof(CurtainsDeed)];
                        cliloc = 1076280;
                        break;
                    }
                case 0x96:
                    {
                        types = [typeof(ScarecrowDeed)];
                        cliloc = 1076281;
                        break;
                    }
                case 0x97:
                    {
                        types = [typeof(WallTorchDeed)];
                        cliloc = 1076282;
                        break;
                    }
                case 0x98:
                    {
                        types = [typeof(FountainDeed)];
                        cliloc = 1076283;
                        break;
                    }
                case 0x99:
                    {
                        types = [typeof(StoneStatueDeed)];
                        cliloc = 1076284;
                        break;
                    }
                case 0x9A:
                    {
                        types = [typeof(LargeFishingNetDeed)];
                        cliloc = 1076285;
                        break;
                    }
                case 0x9B:
                    {
                        types = [typeof(SmallFishingNetDeed)];
                        cliloc = 1076286;
                        break;
                    }
                case 0x9C:
                    {
                        types = [typeof(HouseLadderDeed)];
                        cliloc = 1076287;
                        break;
                    }
                case 0x9D:
                    {
                        types = [typeof(IronMaidenDeed)];
                        cliloc = 1076288;
                        break;
                    }
                case 0x9E:
                    {
                        types = [typeof(BluePlainRugDeed)];
                        cliloc = 1076585;
                        break;
                    }
                case 0x9F:
                    {
                        types = [typeof(GoldenDecorativeRugDeed)];
                        cliloc = 1076586;
                        break;
                    }
                case 0xA0:
                    {
                        types = [typeof(CinnamonFancyRugDeed)];
                        cliloc = 1076587;
                        break;
                    }
                case 0xA1:
                    {
                        types = [typeof(RedPlainRugDeed)];
                        cliloc = 1076588;
                        break;
                    }
                case 0xA2:
                    {
                        types = [typeof(BlueDecorativeRugDeed)];
                        cliloc = 1076589;
                        break;
                    }
                case 0xA3:
                    {
                        types = [typeof(PinkFancyRugDeed)];
                        cliloc = 1076590;
                        break;
                    }
                case 0xA4:
                    {
                        types = [typeof(CherryBlossomTrunkDeed)];
                        cliloc = 1076784;
                        break;
                    }
                case 0xA5:
                    {
                        types = [typeof(AppleTrunkDeed)];
                        cliloc = 1076785;
                        break;
                    }
                case 0xA6:
                    {
                        types = [typeof(PeachTrunkDeed)];
                        cliloc = 1076786;
                        break;
                    }
            }

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
}
