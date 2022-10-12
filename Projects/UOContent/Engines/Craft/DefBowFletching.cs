using System;
using Server.Items;

namespace Server.Engines.Craft;

public class DefBowFletching : CraftSystem
{
    public static void Initialize()
    {
        CraftSystem = new DefBowFletching();
    }

    private DefBowFletching() : base(1, 1, 1.25)
    {
    }

    public override SkillName MainSkill => SkillName.Fletching;

    public override TextDefinition GumpTitle => 1044006;

    public static CraftSystem CraftSystem { get; private set; }

    public override CraftECA ECA => CraftECA.FiftyPercentChanceMinusTenPercent;

    public override double GetChanceAtMin(CraftItem item) => 0.5;

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
        if (tool?.Deleted != false || tool.UsesRemaining < 0)
        {
            return 1044038; // You have worn out your tool!
        }

        if (!BaseTool.CheckAccessible(tool, from))
        {
            return 1044263; // The tool must be on your person to use.
        }

        return 0;
    }

    public override void PlayCraftEffect(Mobile from)
    {
        // no animation
        // if (from.Body.Type == BodyType.Human && !from.Mounted)
        // from.Animate( 33, 5, 1, true, false, 0 );

        from.PlaySound(0x55);
    }

    public override int PlayEndingEffect(
        Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality,
        bool makersMark, CraftItem item
    )
    {
        if (toolBroken)
        {
            from.SendLocalizedMessage(1044038); // You have worn out your tool
        }

        if (failed)
        {
            if (lostMaterial)
            {
                return 1044043; // You failed to create the item, and some of your materials are lost.
            }

            return 1044157; // You failed to create the item, but no materials were lost.
        }

        if (quality == 0)
        {
            return 502785; // You were barely able to make this item.  It's quality is below average.
        }

        if (makersMark && quality == 2)
        {
            return 1044156; // You create an exceptional quality item and affix your maker's mark.
        }

        if (quality == 2)
        {
            return 1044155; // You create an exceptional quality item.
        }

        return 1044154; // You create the item.
    }

    public override void InitCraftList()
    {
        int index;

        // Materials
        AddCraft(typeof(Kindling), 1044457, 1023553, 0.0, 00.0, typeof(Log), 1044041, 1, 1044351);

        index = AddCraft(typeof(Shaft), 1044457, 1027124, 0.0, 40.0, typeof(Log), 1044041, 1, 1044351);
        SetUseAllRes(index, true);

        // Ammunition
        index = AddCraft(typeof(Arrow), 1044565, 1023903, 0.0, 40.0, typeof(Shaft), 1044560, 1, 1044561);
        AddRes(index, typeof(Feather), 1044562, 1, 1044563);
        SetUseAllRes(index, true);

        index = AddCraft(typeof(Bolt), 1044565, 1027163, 0.0, 40.0, typeof(Shaft), 1044560, 1, 1044561);
        AddRes(index, typeof(Feather), 1044562, 1, 1044563);
        SetUseAllRes(index, true);

        if (Core.SE)
        {
            index = AddCraft(typeof(FukiyaDarts), 1044565, 1030246, 50.0, 90.0, typeof(Log), 1044041, 1, 1044351);
            SetUseAllRes(index, true);
            SetNeededExpansion(index, Expansion.SE);
        }

        // Weapons
        AddCraft(typeof(Bow), 1044566, 1025042, 30.0, 70.0, typeof(Log), 1044041, 7, 1044351);
        AddCraft(typeof(Crossbow), 1044566, 1023919, 60.0, 100.0, typeof(Log), 1044041, 7, 1044351);
        AddCraft(typeof(HeavyCrossbow), 1044566, 1025117, 80.0, 120.0, typeof(Log), 1044041, 10, 1044351);

        if (Core.AOS)
        {
            AddCraft(typeof(CompositeBow), 1044566, 1029922, 70.0, 110.0, typeof(Log), 1044041, 7, 1044351);
            AddCraft(typeof(RepeatingCrossbow), 1044566, 1029923, 90.0, 130.0, typeof(Log), 1044041, 10, 1044351);
        }

        if (Core.SE)
        {
            index = AddCraft(typeof(Yumi), 1044566, 1030224, 90.0, 130.0, typeof(Log), 1044041, 10, 1044351);
            SetNeededExpansion(index, Expansion.SE);
        }

        if (Core.ML)
        {
            index = AddCraft(
                typeof(BlightGrippedLongbow),
                1044566,
                1072907,
                75.0,
                125.0,
                typeof(Log),
                1044041,
                20,
                1044351
            );
            AddRes(index, typeof(LardOfParoxysmus), 1032681, 1, 1053098);
            AddRes(index, typeof(Blight), 1032675, 10, 1053098);
            AddRes(index, typeof(Corruption), 1032676, 10, 1053098);
            AddRareRecipe(index, 200);
            ForceNonExceptional(index);
            SetNeededExpansion(index, Expansion.ML);

            /* TODO
            index = AddCraft( typeof( FaerieFire ), 1044566, 1072908, 75.0, 125.0, typeof( Log ), 1044041, 20, 1044351 );
            AddRes( index, typeof( LardOfParoxysmus ), 1032681, 1, 1053098 );
            AddRes( index, typeof( Putrefication ), 1032678, 10, 1053098 );
            AddRes( index, typeof( Taint ), 1032679, 10, 1053098 );
            AddRareRecipe( index, 201 );
            ForceNonExceptional( index );
            SetNeededExpansion( index, Expansion.ML );
            */

            index = AddCraft(
                typeof(SilvanisFeywoodBow),
                1044566,
                1072955,
                75.0,
                125.0,
                typeof(Log),
                1044041,
                20,
                1044351
            );
            AddRes(index, typeof(LardOfParoxysmus), 1032681, 1, 1053098);
            AddRes(index, typeof(Scourge), 1032677, 10, 1053098);
            AddRes(index, typeof(Muculent), 1032680, 10, 1053098);
            AddRareRecipe(index, 202);
            ForceNonExceptional(index);
            SetNeededExpansion(index, Expansion.ML);

            /* TODO
            index = AddCraft( typeof( MischiefMaker ), 1044566, 1072910, 75.0, 125.0, typeof( Log ), 1044041, 15, 1044351 );
            AddRes( index, typeof( DreadHornMane ), 1032682, 1, 1053098 );
            AddRes( index, typeof( Corruption ), 1032676, 10, 1053098 );
            AddRes( index, typeof( Putrefication ), 1032678, 10, 1053098 );
            AddRareRecipe( index, 203 );
            ForceNonExceptional( index );
            SetNeededExpansion( index, Expansion.ML );
            */

            index = AddCraft(typeof(TheNightReaper), 1044566, 1072912, 75.0, 125.0, typeof(Log), 1044041, 10, 1044351);
            AddRes(index, typeof(DreadHornMane), 1032682, 1, 1053098);
            AddRes(index, typeof(Blight), 1032675, 10, 1053098);
            AddRes(index, typeof(Scourge), 1032677, 10, 1053098);
            AddRareRecipe(index, 204);
            ForceNonExceptional(index);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(BarbedLongbow), 1044566, 1073505, 75.0, 125.0, typeof(Log), 1044041, 20, 1044351);
            AddRes(index, typeof(FireRuby), 1026254, 1, 1053098);
            AddRecipe(index, 205);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(SlayerLongbow), 1044566, 1073506, 75.0, 125.0, typeof(Log), 1044041, 20, 1044351);
            AddRes(index, typeof(BrilliantAmber), 1026256, 1, 1053098);
            AddRecipe(index, 206);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(FrozenLongbow), 1044566, 1073507, 75.0, 125.0, typeof(Log), 1044041, 20, 1044351);
            AddRes(index, typeof(Turquoise), 1026250, 1, 1053098);
            AddRecipe(index, 207);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(LongbowOfMight), 1044566, 1073508, 75.0, 125.0, typeof(Log), 1044041, 10, 1044351);
            AddRes(index, typeof(BlueDiamond), 1026255, 1, 1053098);
            AddRecipe(index, 208);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(RangersShortbow), 1044566, 1073509, 75.0, 125.0, typeof(Log), 1044041, 15, 1044351);
            AddRes(index, typeof(PerfectEmerald), 1026251, 1, 1053098);
            AddRecipe(index, 209);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(
                typeof(LightweightShortbow),
                1044566,
                1073510,
                75.0,
                125.0,
                typeof(Log),
                1044041,
                15,
                1044351
            );
            AddRes(index, typeof(WhitePearl), 1026253, 1, 1053098);
            AddRecipe(index, 210);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(MysticalShortbow), 1044566, 1073511, 75.0, 125.0, typeof(Log), 1044041, 15, 1044351);
            AddRes(index, typeof(EcruCitrine), 1026252, 1, 1053098);
            AddRecipe(index, 211);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(
                typeof(AssassinsShortbow),
                1044566,
                1073512,
                75.0,
                125.0,
                typeof(Log),
                1044041,
                15,
                1044351
            );
            AddRes(index, typeof(DarkSapphire), 1026249, 1, 1053098);
            AddRecipe(index, 212);
            SetNeededExpansion(index, Expansion.ML);
        }

        MarkOption = true;
        Repair = Core.AOS;
    }
}
