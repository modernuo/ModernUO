using System;
using Server.Items;

namespace Server.Engines.Craft;

public class DefBlacksmithy : CraftSystem
{
    private static readonly Type typeofAnvil = typeof(AnvilAttribute);
    private static readonly Type typeofForge = typeof(ForgeAttribute);

    public static void Initialize()
    {
        CraftSystem = new DefBlacksmithy();
    }

    private DefBlacksmithy() : base(1, 1, 1.25)
    {
    }

    public override SkillName MainSkill => SkillName.Blacksmith;

    public override TextDefinition GumpTitle => 1044002;

    public static CraftSystem CraftSystem { get; private set; }

    public override CraftECA ECA => CraftECA.ChanceMinusSixtyToFortyFive;

    public override double GetChanceAtMin(CraftItem item) => 0.0;

    public static void CheckAnvilAndForge(Mobile from, int range, out bool anvil, out bool forge)
    {
        anvil = false;
        forge = false;

        var map = from.Map;

        if (map == null)
        {
            return;
        }

        var eable = map.GetItemsInRange(from.Location, range);

        foreach (var item in eable)
        {
            var type = item.GetType();

            var isAnvil = type.IsDefined(typeofAnvil, false) || item.ItemID is 4015 or 4016 or 11733 or 11734;
            var isForge = type.IsDefined(typeofForge, false) || item.ItemID is 4017 or >= 6522 and <= 6569 or 11736;

            if (isAnvil || isForge)
            {
                if (from.Z + 16 < item.Z || item.Z + 16 < from.Z || !from.InLOS(item))
                {
                    continue;
                }

                anvil = anvil || isAnvil;
                forge = forge || isForge;

                if (anvil && forge)
                {
                    break;
                }
            }
        }

        eable.Free();

        for (var x = -range; (!anvil || !forge) && x <= range; ++x)
        {
            for (var y = -range; (!anvil || !forge) && y <= range; ++y)
            {
                var tiles = map.Tiles.GetStaticTiles(from.X + x, from.Y + y, true);

                for (var i = 0; (!anvil || !forge) && i < tiles.Length; ++i)
                {
                    var id = tiles[i].ID;

                    var isAnvil = id is 4015 or 4016 or 11733 or 11734;
                    var isForge = id is 4017 or >= 6522 and <= 6569 or 11736;

                    if (isAnvil || isForge)
                    {
                        if (from.Z + 16 < tiles[i].Z || tiles[i].Z + 16 < from.Z ||
                            !from.InLOS(new Point3D(from.X + x, from.Y + y, tiles[i].Z + tiles[i].Height / 2 + 1)))
                        {
                            continue;
                        }

                        anvil = anvil || isAnvil;
                        forge = forge || isForge;
                    }
                }
            }
        }
    }

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
        if (tool?.Deleted != false || tool.UsesRemaining < 0)
        {
            return 1044038; // You have worn out your tool!
        }

        if (!BaseTool.CheckTool(tool, from))
        {
            return 1048146; // If you have a tool equipped, you must use that tool.
        }

        if (!BaseTool.CheckAccessible(tool, from))
        {
            return 1044263; // The tool must be on your person to use.
        }

        CheckAnvilAndForge(from, 2, out var anvil, out var forge);

        if (anvil && forge)
        {
            return 0;
        }

        return 1044267; // You must be near an anvil and a forge to smith items.
    }

    public override void PlayCraftEffect(Mobile from)
    {
        // To animate and synchronize with sound
        // if (from.Body.Type == BodyType.Human && !from.Mounted)
        // from.Animate( 9, 5, 1, true, false, 0 );
        // 0.7 second delay
        from.PlaySound(0x2A);
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
        /*
        Syntax for a SIMPLE craft item
        AddCraft( ObjectType, Group, MinSkill, MaxSkill, ResourceType, Amount, Message )

        ObjectType : The type of the object you want to add to the build list.
        Group : The group in which the object will be showed in the craft menu.
        MinSkill : The minimum of skill value
        MaxSkill : The maximum of skill value
        ResourceType : The type of the resource the mobile need to create the item
        Amount : The amount of the ResourceType it need to create the item
        Message : String or Int for Localized.  The message that will be sent to the mobile, if the specified resource is missing.

        Syntax for a COMPLEX craft item.  A complex item is an item that need either more than
        only one skill, or more than only one resource.

        Coming soon....
        */

        AddCraft(typeof(RingmailGloves), 1011076, 1025099, 12.0, 62.0, typeof(IronIngot), 1044036, 10, 1044037);
        AddCraft(typeof(RingmailLegs), 1011076, 1025104, 19.4, 69.4, typeof(IronIngot), 1044036, 16, 1044037);
        AddCraft(typeof(RingmailArms), 1011076, 1025103, 16.9, 66.9, typeof(IronIngot), 1044036, 14, 1044037);
        AddCraft(typeof(RingmailChest), 1011076, 1025100, 21.9, 71.9, typeof(IronIngot), 1044036, 18, 1044037);

        AddCraft(typeof(ChainCoif), 1011077, 1025051, 14.5, 64.5, typeof(IronIngot), 1044036, 10, 1044037);
        AddCraft(typeof(ChainLegs), 1011077, 1025054, 36.7, 86.7, typeof(IronIngot), 1044036, 18, 1044037);
        AddCraft(typeof(ChainChest), 1011077, 1025055, 39.1, 89.1, typeof(IronIngot), 1044036, 20, 1044037);

        int index;

        AddCraft(typeof(PlateArms), 1011078, 1025136, 66.3, 116.3, typeof(IronIngot), 1044036, 18, 1044037);
        AddCraft(typeof(PlateGloves), 1011078, 1025140, 58.9, 108.9, typeof(IronIngot), 1044036, 12, 1044037);
        AddCraft(typeof(PlateGorget), 1011078, 1025139, 56.4, 106.4, typeof(IronIngot), 1044036, 10, 1044037);
        AddCraft(typeof(PlateLegs), 1011078, 1025137, 68.8, 118.8, typeof(IronIngot), 1044036, 20, 1044037);
        AddCraft(typeof(PlateChest), 1011078, 1046431, 75.0, 125.0, typeof(IronIngot), 1044036, 25, 1044037);
        AddCraft(typeof(FemalePlateChest), 1011078, 1046430, 44.1, 94.1, typeof(IronIngot), 1044036, 20, 1044037);

        if (Core.AOS) // exact pre-aos functionality unknown
        {
            AddCraft(typeof(DragonBardingDeed), 1011078, 1053012, 72.5, 122.5, typeof(IronIngot), 1044036, 750, 1044037);
        }

        if (Core.SE)
        {
            index = AddCraft(typeof(PlateMempo), 1011078, 1030180, 80.0, 130.0, typeof(IronIngot), 1044036, 18, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(PlateDo), 1011078, 1030184, 87.0, 137.0, typeof(IronIngot), 1044036, 28, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(PlateHiroSode), 1011078, 1030187, 80.0, 130.0, typeof(IronIngot), 1044036, 16, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(PlateSuneate), 1011078, 1030195, 65.0, 115.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(PlateHaidate), 1011078, 1030200, 65.0, 115.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.SE);
        }

        AddCraft(typeof(Bascinet), 1011079, 1025132, 8.3, 58.3, typeof(IronIngot), 1044036, 15, 1044037);
        AddCraft(typeof(CloseHelm), 1011079, 1025128, 37.9, 87.9, typeof(IronIngot), 1044036, 15, 1044037);
        AddCraft(typeof(Helmet), 1011079, 1025130, 37.9, 87.9, typeof(IronIngot), 1044036, 15, 1044037);
        AddCraft(typeof(NorseHelm), 1011079, 1025134, 37.9, 87.9, typeof(IronIngot), 1044036, 15, 1044037);
        AddCraft(typeof(PlateHelm), 1011079, 1025138, 62.6, 112.6, typeof(IronIngot), 1044036, 15, 1044037);

        if (Core.SE)
        {
            index = AddCraft(typeof(ChainHatsuburi), 1011079, 1030175, 30.0, 80.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(PlateHatsuburi), 1011079, 1030176, 45.0, 95.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(HeavyPlateJingasa), 1011079, 1030178, 45.0, 95.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(LightPlateJingasa), 1011079, 1030188, 45.0, 95.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(SmallPlateJingasa), 1011079, 1030191, 45.0, 95.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(DecorativePlateKabuto), 1011079, 1030179, 90.0, 140.0, typeof(IronIngot), 1044036, 25, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(PlateBattleKabuto), 1011079, 1030192, 90.0, 140.0, typeof(IronIngot), 1044036, 25, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            index = AddCraft(typeof(StandardPlateKabuto), 1011079, 1030196, 90.0, 140.0, typeof(IronIngot), 1044036, 25, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            if (Core.ML)
            {
                index = AddCraft(typeof(Circlet), 1011079, 1032645, 62.1, 112.1, typeof(IronIngot), 1044036, 6, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(RoyalCirclet), 1011079, 1032646, 70.0, 120.0, typeof(IronIngot), 1044036, 6, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(GemmedCirclet), 1011079, 1032647, 75.0, 125.0, typeof(IronIngot), 1044036, 6, 1044037);
                AddRes(index, typeof(Tourmaline), 1044237, 1, 1044240);
                AddRes(index, typeof(Amethyst), 1044236, 1, 1044240);
                AddRes(index, typeof(BlueDiamond), 1032696, 1, 1044240);
                SetNeededExpansion(index, Expansion.ML);
            }
        }

        AddCraft(typeof(Buckler), 1011080, 1027027, -25.0, 25.0, typeof(IronIngot), 1044036, 10, 1044037);
        AddCraft(typeof(BronzeShield), 1011080, 1027026, -15.2, 34.8, typeof(IronIngot), 1044036, 12, 1044037);
        AddCraft(typeof(HeaterShield), 1011080, 1027030, 24.3, 74.3, typeof(IronIngot), 1044036, 18, 1044037);
        AddCraft(typeof(MetalShield), 1011080, 1027035, -10.2, 39.8, typeof(IronIngot), 1044036, 14, 1044037);
        AddCraft(typeof(MetalKiteShield), 1011080, 1027028, 4.6, 54.6, typeof(IronIngot), 1044036, 16, 1044037);
        AddCraft(typeof(WoodenKiteShield), 1011080, 1027032, -15.2, 34.8, typeof(IronIngot), 1044036, 8, 1044037);

        if (Core.AOS)
        {
            AddCraft(typeof(ChaosShield), 1011080, 1027107, 85.0, 135.0, typeof(IronIngot), 1044036, 25, 1044037);
            AddCraft(typeof(OrderShield), 1011080, 1027108, 85.0, 135.0, typeof(IronIngot), 1044036, 25, 1044037);
        }

        if (Core.AOS)
        {
            AddCraft(typeof(BoneHarvester), 1011081, 1029915, 33.0, 83.0, typeof(IronIngot), 1044036, 10, 1044037);
        }

        AddCraft(typeof(Broadsword), 1011081, 1023934, 35.4, 85.4, typeof(IronIngot), 1044036, 10, 1044037);

        if (Core.AOS)
        {
            AddCraft(typeof(CrescentBlade), 1011081, 1029921, 45.0, 95.0, typeof(IronIngot), 1044036, 14, 1044037);
        }

        AddCraft(typeof(Cutlass), 1011081, 1025185, 24.3, 74.3, typeof(IronIngot), 1044036, 8, 1044037);
        AddCraft(typeof(Dagger), 1011081, 1023921, -0.4, 49.6, typeof(IronIngot), 1044036, 3, 1044037);
        AddCraft(typeof(Katana), 1011081, 1025119, 44.1, 94.1, typeof(IronIngot), 1044036, 8, 1044037);
        AddCraft(typeof(Kryss), 1011081, 1025121, 36.7, 86.7, typeof(IronIngot), 1044036, 8, 1044037);
        AddCraft(typeof(Longsword), 1011081, 1023937, 28.0, 78.0, typeof(IronIngot), 1044036, 12, 1044037);
        AddCraft(typeof(Scimitar), 1011081, 1025046, 31.7, 81.7, typeof(IronIngot), 1044036, 10, 1044037);
        AddCraft(typeof(VikingSword), 1011081, 1025049, 24.3, 74.3, typeof(IronIngot), 1044036, 14, 1044037);

        if (Core.SE)
        {
            index = AddCraft(typeof(NoDachi), 1011081, 1030221, 75.0, 125.0, typeof(IronIngot), 1044036, 18, 1044037);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(typeof(Wakizashi), 1011081, 1030223, 50.0, 100.0, typeof(IronIngot), 1044036, 8, 1044037);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(typeof(Lajatang), 1011081, 1030226, 80.0, 130.0, typeof(IronIngot), 1044036, 25, 1044037);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(typeof(Daisho), 1011081, 1030228, 60.0, 110.0, typeof(IronIngot), 1044036, 15, 1044037);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(typeof(Tekagi), 1011081, 1030230, 55.0, 105.0, typeof(IronIngot), 1044036, 12, 1044037);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(typeof(Shuriken), 1011081, 1030231, 45.0, 95.0, typeof(IronIngot), 1044036, 5, 1044037);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(typeof(Kama), 1011081, 1030232, 40.0, 90.0, typeof(IronIngot), 1044036, 14, 1044037);
            SetNeededExpansion(index, Expansion.SE);
            index = AddCraft(typeof(Sai), 1011081, 1030234, 50.0, 100.0, typeof(IronIngot), 1044036, 12, 1044037);
            SetNeededExpansion(index, Expansion.SE);

            if (Core.ML)
            {
                index = AddCraft(typeof(RadiantScimitar), 1011081, 1031571, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(WarCleaver), 1011081, 1031567, 70.0, 120.0, typeof(IronIngot), 1044036, 18, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(ElvenSpellblade), 1011081, 1031564, 70.0, 120.0, typeof(IronIngot), 1044036, 14, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(AssassinSpike), 1011081, 1031565, 70.0, 120.0, typeof(IronIngot), 1044036, 9, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(Leafblade), 1011081, 1031566, 70.0, 120.0, typeof(IronIngot), 1044036, 12, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(RuneBlade), 1011081, 1031570, 70.0, 120.0, typeof(IronIngot), 1044036, 15, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(ElvenMachete), 1011081, 1031573, 70.0, 120.0, typeof(IronIngot), 1044036, 14, 1044037);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(RuneCarvingKnife), 1011081, 1072915, 70.0, 120.0, typeof(IronIngot), 1044036, 9, 1044037);
                AddRes(index, typeof(DreadHornMane), 1032682, 1, 1053098);
                AddRes(index, typeof(Putrefication), 1032678, 10, 1053098);
                AddRes(index, typeof(Muculent), 1032680, 10, 1053098);
                AddRareRecipe(index, 0);
                ForceNonExceptional(index);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(ColdForgedBlade), 1011081, 1072916, 70.0, 120.0, typeof(IronIngot), 1044036, 18, 1044037);
                AddRes(index, typeof(GrizzledBones), 1032684, 1, 1053098);
                AddRes(index, typeof(Taint), 1032684, 10, 1053098);
                AddRes(index, typeof(Blight), 1032675, 10, 1053098);
                AddRareRecipe(index, 1);
                ForceNonExceptional(index);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(OverseerSunderedBlade), 1011081, 1072920, 70.0, 120.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(GrizzledBones), 1032684, 1, 1053098);
                AddRes(index, typeof(Blight), 1032675, 10, 1053098);
                AddRes(index, typeof(Scourge), 1032677, 10, 1053098);
                AddRareRecipe(index, 2);
                ForceNonExceptional(index);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(LuminousRuneBlade), 1011081, 1072922, 70.0, 120.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(GrizzledBones), 1032684, 1, 1053098);
                AddRes(index, typeof(Corruption), 1032676, 10, 1053098);
                AddRes(index, typeof(Putrefication), 1032678, 10, 1053098);
                AddRareRecipe(index, 3);
                ForceNonExceptional(index);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(TrueSpellblade), 1011081, 1073513, 75.0, 125.0, typeof(IronIngot), 1044036, 14, 1044037);
                AddRes(index, typeof(BlueDiamond), 1032696, 1, 1044240);
                AddRecipe(index, 4);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(IcySpellblade), 1011081, 1073514, 75.0, 125.0, typeof(IronIngot), 1044036, 14, 1044037);
                AddRes(index, typeof(Turquoise), 1032691, 1, 1044240);
                AddRecipe(index, 5);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(FierySpellblade), 1011081, 1073515, 75.0, 125.0, typeof(IronIngot), 1044036, 14, 1044037);
                AddRes(index, typeof(FireRuby), 1032695, 1, 1044240);
                AddRecipe(index, 6);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(SpellbladeOfDefense), 1011081, 1073516, 75.0, 125.0, typeof(IronIngot), 1044036, 18, 1044037);
                AddRes(index, typeof(WhitePearl), 1032694, 1, 1044240);
                AddRecipe(index, 7);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(TrueAssassinSpike), 1011081, 1073517, 75.0, 125.0, typeof(IronIngot), 1044036, 9, 1044037);
                AddRes(index, typeof(DarkSapphire), 1032690, 1, 1044240);
                AddRecipe(index, 8);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(ChargedAssassinSpike), 1011081, 1073518, 75.0, 125.0, typeof(IronIngot), 1044036, 9, 1044037);
                AddRes(index, typeof(EcruCitrine), 1032693, 1, 1044240);
                AddRecipe(index, 9);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(MagekillerAssassinSpike), 1011081, 1073519, 75.0, 125.0, typeof(IronIngot), 1044036, 9, 1044037);
                AddRes(index, typeof(BrilliantAmber), 1032697, 1, 1044240);
                AddRecipe(index, 10);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(WoundingAssassinSpike), 1011081, 1073520, 75.0, 125.0, typeof(IronIngot), 1044036, 9, 1044037);
                AddRes(index, typeof(PerfectEmerald), 1032692, 1, 1044240);
                AddRecipe(index, 11);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(TrueLeafblade), 1011081, 1073521, 75.0, 125.0, typeof(IronIngot), 1044036, 12, 1044037);
                AddRes(index, typeof(BlueDiamond), 1032696, 1, 1044240);
                AddRecipe(index, 12);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(Luckblade), 1011081, 1073522, 75.0, 125.0, typeof(IronIngot), 1044036, 12, 1044037);
                AddRes(index, typeof(WhitePearl), 1032694, 1, 1044240);
                AddRecipe(index, 13);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(MagekillerLeafblade), 1011081, 1073523, 75.0, 125.0, typeof(IronIngot), 1044036, 12, 1044037);
                AddRes(index, typeof(FireRuby), 1032695, 1, 1044240);
                AddRecipe(index, 14);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(LeafbladeOfEase), 1011081, 1073524, 75.0, 125.0, typeof(IronIngot), 1044036, 12, 1044037);
                AddRes(index, typeof(PerfectEmerald), 1032692, 1, 1044240);
                AddRecipe(index, 15);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(KnightsWarCleaver), 1011081, 1073525, 75.0, 125.0, typeof(IronIngot), 1044036, 18, 1044037);
                AddRes(index, typeof(PerfectEmerald), 1032692, 1, 1044240);
                AddRecipe(index, 16);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(ButchersWarCleaver), 1011081, 1073526, 75.0, 125.0, typeof(IronIngot), 1044036, 18, 1044037);
                AddRes(index, typeof(Turquoise), 1032691, 1, 1044240);
                AddRecipe(index, 17);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(SerratedWarCleaver), 1011081, 1073527, 75.0, 125.0, typeof(IronIngot), 1044036, 18, 1044037);
                AddRes(index, typeof(EcruCitrine), 1032693, 1, 1044240);
                AddRecipe(index, 18);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(TrueWarCleaver), 1011081, 1073528, 75.0, 125.0, typeof(IronIngot), 1044036, 18, 1044037);
                AddRes(index, typeof(BrilliantAmber), 1032697, 1, 1044240);
                AddRecipe(index, 19);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(AdventurersMachete), 1011081, 1073533, 75.0, 125.0, typeof(IronIngot), 1044036, 14, 1044037);
                AddRes(index, typeof(WhitePearl), 1032694, 1, 1044240);
                AddRecipe(index, 20);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(OrcishMachete), 1011081, 1073534, 75.0, 125.0, typeof(IronIngot), 1044036, 14, 1044037);
                AddRes(index, typeof(Scourge), 1072136, 1, 1042081);
                AddRecipe(index, 21);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(MacheteOfDefense), 1011081, 1073535, 75.0, 125.0, typeof(IronIngot), 1044036, 14, 1044037);
                AddRes(index, typeof(BrilliantAmber), 1032697, 1, 1044240);
                AddRecipe(index, 22);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(DiseasedMachete), 1011081, 1073536, 75.0, 125.0, typeof(IronIngot), 1044036, 14, 1044037);
                AddRes(index, typeof(Blight), 1072134, 1, 1042081);
                AddRecipe(index, 23);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(Runesabre), 1011081, 1073537, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(Turquoise), 1032691, 1, 1044240);
                AddRecipe(index, 24);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(MagesRuneBlade), 1011081, 1073538, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(BlueDiamond), 1032696, 1, 1044240);
                AddRecipe(index, 25);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(RuneBladeOfKnowledge), 1011081, 1073539, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(EcruCitrine), 1032693, 1, 1044240);
                AddRecipe(index, 26);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(CorruptedRuneBlade), 1011081, 1073540, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(Corruption), 1072135, 1, 1042081);
                AddRecipe(index, 27);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(TrueRadiantScimitar), 1011081, 1073541, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(BrilliantAmber), 1032697, 1, 1044240);
                AddRecipe(index, 28);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(DarkglowScimitar), 1011081, 1073542, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(DarkSapphire), 1032690, 1, 1044240);
                AddRecipe(index, 29);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(IcyScimitar), 1011081, 1073543, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(DarkSapphire), 1032690, 1, 1044240);
                AddRecipe(index, 30);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(TwinklingScimitar), 1011081, 1073544, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
                AddRes(index, typeof(DarkSapphire), 1032690, 1, 1044240);
                AddRecipe(index, 31);
                SetNeededExpansion(index, Expansion.ML);

                index = AddCraft(typeof(BoneMachete), 1011081, 1020526, 45.0, 95.0, typeof(IronIngot), 1044036, 20, 1044037);
                AddRes(index, typeof(Bone), 1049064, 6, 1049063);
                AddQuestRecipe(index, 32);
                SetNeededExpansion(index, Expansion.ML);
            }
        }

        AddCraft(typeof(Axe), 1011082, 1023913, 34.2, 84.2, typeof(IronIngot), 1044036, 14, 1044037);
        AddCraft(typeof(BattleAxe), 1011082, 1023911, 30.5, 80.5, typeof(IronIngot), 1044036, 14, 1044037);
        AddCraft(typeof(DoubleAxe), 1011082, 1023915, 29.3, 79.3, typeof(IronIngot), 1044036, 12, 1044037);
        AddCraft(typeof(ExecutionersAxe), 1011082, 1023909, 34.2, 84.2, typeof(IronIngot), 1044036, 14, 1044037);
        AddCraft(typeof(LargeBattleAxe), 1011082, 1025115, 28.0, 78.0, typeof(IronIngot), 1044036, 12, 1044037);
        AddCraft(typeof(TwoHandedAxe), 1011082, 1025187, 33.0, 83.0, typeof(IronIngot), 1044036, 16, 1044037);
        AddCraft(typeof(WarAxe), 1011082, 1025040, 39.1, 89.1, typeof(IronIngot), 1044036, 16, 1044037);

        if (Core.ML)
        {
            index = AddCraft(typeof(OrnateAxe), 1011082, 1031572, 70.0, 120.0, typeof(IronIngot), 1044036, 18, 1044037);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(GuardianAxe), 1011082, 1073545, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
            AddRes(index, typeof(BlueDiamond), 1032696, 1, 1044240);
            AddRecipe(index, 33);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(SingingAxe), 1011082, 1073546, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
            AddRes(index, typeof(BrilliantAmber), 1032697, 1, 1044240);
            AddRecipe(index, 34);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(ThunderingAxe), 1011082, 1073547, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
            AddRes(index, typeof(EcruCitrine), 1032693, 1, 1044240);
            AddRecipe(index, 35);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(HeavyOrnateAxe), 1011082, 1073548, 75.0, 125.0, typeof(IronIngot), 1044036, 15, 1044037);
            AddRes(index, typeof(Turquoise), 1032691, 1, 1044240);
            AddRecipe(index, 36);
            SetNeededExpansion(index, Expansion.ML);
        }

        AddCraft(typeof(Bardiche), 1011083, 1023917, 31.7, 81.7, typeof(IronIngot), 1044036, 18, 1044037);

        if (Core.AOS)
        {
            AddCraft(typeof(BladedStaff), 1011083, 1029917, 40.0, 90.0, typeof(IronIngot), 1044036, 12, 1044037);
        }

        if (Core.AOS)
        {
            AddCraft(typeof(DoubleBladedStaff), 1011083, 1029919, 45.0, 95.0, typeof(IronIngot), 1044036, 16, 1044037);
        }

        AddCraft(typeof(Halberd), 1011083, 1025183, 39.1, 89.1, typeof(IronIngot), 1044036, 20, 1044037);

        if (Core.AOS)
        {
            AddCraft(typeof(Lance), 1011083, 1029920, 48.0, 98.0, typeof(IronIngot), 1044036, 20, 1044037);
        }

        if (Core.AOS)
        {
            AddCraft(typeof(Pike), 1011083, 1029918, 47.0, 97.0, typeof(IronIngot), 1044036, 12, 1044037);
        }

        AddCraft(typeof(ShortSpear), 1011083, 1025123, 45.3, 95.3, typeof(IronIngot), 1044036, 6, 1044037);

        if (Core.AOS)
        {
            AddCraft(typeof(Scythe), 1011083, 1029914, 39.0, 89.0, typeof(IronIngot), 1044036, 14, 1044037);
        }

        AddCraft(typeof(Spear), 1011083, 1023938, 49.0, 99.0, typeof(IronIngot), 1044036, 12, 1044037);
        AddCraft(typeof(WarFork), 1011083, 1025125, 42.9, 92.9, typeof(IronIngot), 1044036, 12, 1044037);

        // Not craftable (is this an AOS change ??)
        // AddCraft( typeof( Pitchfork ), 1011083, 1023720, 36.1, 86.1, typeof( IronIngot ), 1044036, 12, 1044037 );

        AddCraft(typeof(HammerPick), 1011084, 1025181, 34.2, 84.2, typeof(IronIngot), 1044036, 16, 1044037);
        AddCraft(typeof(Mace), 1011084, 1023932, 14.5, 64.5, typeof(IronIngot), 1044036, 6, 1044037);
        AddCraft(typeof(Maul), 1011084, 1025179, 19.4, 69.4, typeof(IronIngot), 1044036, 10, 1044037);

        if (Core.AOS)
        {
            AddCraft(typeof(Scepter), 1011084, 1029916, 21.4, 71.4, typeof(IronIngot), 1044036, 10, 1044037);
        }

        AddCraft(typeof(WarMace), 1011084, 1025127, 28.0, 78.0, typeof(IronIngot), 1044036, 14, 1044037);
        AddCraft(typeof(WarHammer), 1011084, 1025177, 34.2, 84.2, typeof(IronIngot), 1044036, 16, 1044037);

        if (Core.SE)
        {
            index = AddCraft(typeof(Tessen), 1011084, 1030222, 85.0, 135.0, typeof(IronIngot), 1044036, 16, 1044037);
            AddSkill(index, SkillName.Tailoring, 50.0, 55.0);
            AddRes(index, typeof(Cloth), 1044286, 10, 1044287);
            SetNeededExpansion(index, Expansion.SE);
        }

        if (Core.ML)
        {
            index = AddCraft(typeof(DiamondMace), 1011084, 1031556, 70.0, 120.0, typeof(IronIngot), 1044036, 20, 1044037);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(ShardThrasher), 1011084, 1072918, 70.0, 120.0, typeof(IronIngot), 1044036, 20, 1044037);
            AddRes(index, typeof(EyeOfTheTravesty), 1073126, 1, 1042081);
            AddRes(index, typeof(Muculent), 1072139, 10, 1042081);
            AddRes(index, typeof(Corruption), 1072135, 10, 1042081);
            AddRareRecipe(index, 37);
            ForceNonExceptional(index);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(RubyMace), 1011084, 1073529, 75.0, 125.0, typeof(IronIngot), 1044036, 20, 1044037);
            AddRes(index, typeof(FireRuby), 1032695, 1, 1044240);
            AddRecipe(index, 38);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(EmeraldMace), 1011084, 1073530, 75.0, 125.0, typeof(IronIngot), 1044036, 20, 1044037);
            AddRes(index, typeof(PerfectEmerald), 1032692, 1, 1044240);
            AddRecipe(index, 39);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(SapphireMace), 1011084, 1073531, 75.0, 125.0, typeof(IronIngot), 1044036, 20, 1044037);
            AddRes(index, typeof(DarkSapphire), 1032690, 1, 1044240);
            AddRecipe(index, 40);
            SetNeededExpansion(index, Expansion.ML);

            index = AddCraft(typeof(SilverEtchedMace), 1011084, 1073532, 75.0, 125.0, typeof(IronIngot), 1044036, 20, 1044037);
            AddRes(index, typeof(BlueDiamond), 1032696, 1, 1044240);
            AddRecipe(index, 41);
            SetNeededExpansion(index, Expansion.ML);
        }

        index = AddCraft(typeof(DragonGloves), 1053114, 1029795, 68.9, 118.9, typeof(RedScales), 1060883, 16, 1060884);
        SetUseSubRes2(index, true);

        index = AddCraft(typeof(DragonHelm), 1053114, 1029797, 72.6, 122.6, typeof(RedScales), 1060883, 20, 1060884);
        SetUseSubRes2(index, true);

        index = AddCraft(typeof(DragonLegs), 1053114, 1029799, 78.8, 128.8, typeof(RedScales), 1060883, 28, 1060884);
        SetUseSubRes2(index, true);

        index = AddCraft(typeof(DragonArms), 1053114, 1029815, 76.3, 126.3, typeof(RedScales), 1060883, 24, 1060884);
        SetUseSubRes2(index, true);

        index = AddCraft(typeof(DragonChest), 1053114, 1029793, 85.0, 135.0, typeof(RedScales), 1060883, 36, 1060884);
        SetUseSubRes2(index, true);

        // Set the overridable material
        SetSubRes(typeof(IronIngot), 1044022);

        // Add every material you want the player to be able to choose from
        // This will override the overridable material
        AddSubRes(typeof(IronIngot), 1044022, 00.0, 1044036, 1044267);
        AddSubRes(typeof(DullCopperIngot), 1044023, 65.0, 1044036, 1044268);
        AddSubRes(typeof(ShadowIronIngot), 1044024, 70.0, 1044036, 1044268);
        AddSubRes(typeof(CopperIngot), 1044025, 75.0, 1044036, 1044268);
        AddSubRes(typeof(BronzeIngot), 1044026, 80.0, 1044036, 1044268);
        AddSubRes(typeof(GoldIngot), 1044027, 85.0, 1044036, 1044268);
        AddSubRes(typeof(AgapiteIngot), 1044028, 90.0, 1044036, 1044268);
        AddSubRes(typeof(VeriteIngot), 1044029, 95.0, 1044036, 1044268);
        AddSubRes(typeof(ValoriteIngot), 1044030, 99.0, 1044036, 1044268);

        SetSubRes2(typeof(RedScales), 1060875);

        AddSubRes2(typeof(RedScales), 1060875, 0.0, 1053137, 1044268);
        AddSubRes2(typeof(YellowScales), 1060876, 0.0, 1053137, 1044268);
        AddSubRes2(typeof(BlackScales), 1060877, 0.0, 1053137, 1044268);
        AddSubRes2(typeof(GreenScales), 1060878, 0.0, 1053137, 1044268);
        AddSubRes2(typeof(WhiteScales), 1060879, 0.0, 1053137, 1044268);
        AddSubRes2(typeof(BlueScales), 1060880, 0.0, 1053137, 1044268);

        Resmelt = true;
        Repair = true;
        MarkOption = true;
        CanEnhance = Core.AOS;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class ForgeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class AnvilAttribute : Attribute
{
}
