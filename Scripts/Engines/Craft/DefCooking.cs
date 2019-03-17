using System;
using Server.Items;

namespace Server.Engines.Craft
{
  public class DefCooking : CraftSystem
  {
    private static CraftSystem m_CraftSystem;

    private DefCooking() : base(1, 1, 1.25) // base( 1, 1, 1.5 )
    {
    }

    public override SkillName MainSkill => SkillName.Cooking;

    public override int GumpTitleNumber => 1044003;

    public static CraftSystem CraftSystem => m_CraftSystem ?? (m_CraftSystem = new DefCooking());

    public override CraftECA ECA => CraftECA.ChanceMinusSixtyToFourtyFive;

    public override double GetChanceAtMin(CraftItem item)
    {
      return 0.0; // 0%
    }

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
      if (tool?.Deleted != false || tool.UsesRemaining < 0)
        return 1044038; // You have worn out your tool!
      if (!BaseTool.CheckAccessible(tool, from))
        return 1044263; // The tool must be on your person to use.

      return 0;
    }

    public override void PlayCraftEffect(Mobile from)
    {
    }

    public override int PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality,
      bool makersMark, CraftItem item)
    {
      if (toolBroken)
        from.SendLocalizedMessage(1044038); // You have worn out your tool

      if (failed)
      {
        if (lostMaterial)
          return 1044043; // You failed to create the item, and some of your materials are lost.
        return 1044157; // You failed to create the item, but no materials were lost.
      }

      if (quality == 0)
        return 502785; // You were barely able to make this item.  It's quality is below average.
      if (makersMark && quality == 2)
        return 1044156; // You create an exceptional quality item and affix your maker's mark.
      if (quality == 2)
        return 1044155; // You create an exceptional quality item.
      return 1044154; // You create the item.
    }

    public override void InitCraftList()
    {
      /* Begin Ingredients */
      int index = AddCraft(typeof(SackFlour), 1044495, 1024153, 0, 1000, typeof(WheatSheaf), 1044489, 2, 1044490);
      SetNeedMill(index, true);

      index = AddCraft(typeof(Dough), 1044495, 1024157, 0, 1000, typeof(SackFlour), 1044468, 1, 1044253);
      AddRes(index, typeof(BaseBeverage), 1046458, 1, 1044253);

      index = AddCraft(typeof(SweetDough), 1044495, 1041340, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(JarHoney), 1044472, 1, 1044253);

      index = AddCraft(typeof(CakeMix), 1044495, 1041002, 0, 1000, typeof(SackFlour), 1044468, 1, 1044253);
      AddRes(index, typeof(SweetDough), 1044475, 1, 1044253);

      index = AddCraft(typeof(CookieMix), 1044495, 1024159, 0, 1000, typeof(JarHoney), 1044472, 1, 1044253);
      AddRes(index, typeof(SweetDough), 1044475, 1, 1044253);

      if (Core.ML)
      {
        index = AddCraft(typeof(CocoaButter), 1044495, 1079998, 0, 1000, typeof(CocoaPulp), 1080530, 1, 1044253);
        SetItemHue(index, 0x457);
        SetNeededExpansion(index, Expansion.ML);
        SetNeedOven(index, true);

        index = AddCraft(typeof(CocoaLiquor), 1044495, 1079999, 0, 1000, typeof(CocoaPulp), 1080530, 1, 1044253);
        AddRes(index, typeof(EmptyPewterBowl), 1025629, 1, 1044253);
        SetItemHue(index, 0x46A);
        SetNeededExpansion(index, Expansion.ML);
        SetNeedOven(index, true);
      }
      /* End Ingredients */

      /* Begin Preparations */
      index = AddCraft(typeof(UnbakedQuiche), 1044496, 1041339, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(Eggs), 1044477, 1, 1044253);

      // TODO: This must also support chicken and lamb legs
      index = AddCraft(typeof(UnbakedMeatPie), 1044496, 1041338, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(RawRibs), 1044482, 1, 1044253);

      index = AddCraft(typeof(UncookedSausagePizza), 1044496, 1041337, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(Sausage), 1044483, 1, 1044253);

      index = AddCraft(typeof(UncookedCheesePizza), 1044496, 1041341, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(CheeseWheel), 1044486, 1, 1044253);

      index = AddCraft(typeof(UnbakedFruitPie), 1044496, 1041334, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(Pear), 1044481, 1, 1044253);

      index = AddCraft(typeof(UnbakedPeachCobbler), 1044496, 1041335, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(Peach), 1044480, 1, 1044253);

      index = AddCraft(typeof(UnbakedApplePie), 1044496, 1041336, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(Apple), 1044479, 1, 1044253);

      index = AddCraft(typeof(UnbakedPumpkinPie), 1044496, 1041342, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      AddRes(index, typeof(Pumpkin), 1044484, 1, 1044253);

      if (Core.SE)
      {
        index = AddCraft(typeof(GreenTea), 1044496, 1030315, 800, 1300, typeof(GreenTeaBasket), 1030316, 1,
          1044253);
        AddRes(index, typeof(BaseBeverage), 1046458, 1, 1044253);
        SetNeededExpansion(index, Expansion.SE);
        SetNeedOven(index, true);

        index = AddCraft(typeof(WasabiClumps), 1044496, 1029451, 700, 1200, typeof(BaseBeverage), 1046458, 1,
          1044253);
        AddRes(index, typeof(WoodenBowlOfPeas), 1025633, 3, 1044253);
        SetNeededExpansion(index, Expansion.SE);

        index = AddCraft(typeof(SushiRolls), 1044496, 1030303, 900, 1200, typeof(BaseBeverage), 1046458, 1,
          1044253);
        AddRes(index, typeof(RawFishSteak), 1044476, 10, 1044253);
        SetNeededExpansion(index, Expansion.SE);

        index = AddCraft(typeof(SushiPlatter), 1044496, 1030305, 900, 1200, typeof(BaseBeverage), 1046458, 1,
          1044253);
        AddRes(index, typeof(RawFishSteak), 1044476, 10, 1044253);
        SetNeededExpansion(index, Expansion.SE);
      }

      index = AddCraft(typeof(TribalPaint), 1044496, 1040000, Core.ML ? 550 : 800, Core.ML ? 1050 : 800,
        typeof(SackFlour), 1044468, 1, 1044253);
      AddRes(index, typeof(TribalBerry), 1046460, 1, 1044253);

      if (Core.SE)
      {
        index = AddCraft(typeof(EggBomb), 1044496, 1030249, 900, 1200, typeof(Eggs), 1044477, 1, 1044253);
        AddRes(index, typeof(SackFlour), 1044468, 3, 1044253);
        SetNeededExpansion(index, Expansion.SE);
      }
      /* End Preparations */

      /* Begin Baking */
      index = AddCraft(typeof(BreadLoaf), 1044497, 1024156, 0, 1000, typeof(Dough), 1044469, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(Cookies), 1044497, 1025643, 0, 1000, typeof(CookieMix), 1044474, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(Cake), 1044497, 1022537, 0, 1000, typeof(CakeMix), 1044471, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(Muffins), 1044497, 1022539, 0, 1000, typeof(SweetDough), 1044475, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(Quiche), 1044497, 1041345, 0, 1000, typeof(UnbakedQuiche), 1044518, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(MeatPie), 1044497, 1041347, 0, 1000, typeof(UnbakedMeatPie), 1044519, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(SausagePizza), 1044497, 1044517, 0, 1000, typeof(UncookedSausagePizza), 1044520, 1,
        1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(CheesePizza), 1044497, 1044516, 0, 1000, typeof(UncookedCheesePizza), 1044521, 1,
        1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(FruitPie), 1044497, 1041346, 0, 1000, typeof(UnbakedFruitPie), 1044522, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(PeachCobbler), 1044497, 1041344, 0, 1000, typeof(UnbakedPeachCobbler), 1044523, 1,
        1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(ApplePie), 1044497, 1041343, 0, 1000, typeof(UnbakedApplePie), 1044524, 1, 1044253);
      SetNeedOven(index, true);

      index = AddCraft(typeof(PumpkinPie), 1044497, 1041348, 0, 1000, typeof(UnbakedPumpkinPie), 1046461, 1,
        1044253);
      SetNeedOven(index, true);

      if (Core.SE)
      {
        index = AddCraft(typeof(MisoSoup), 1044497, 1030317, 600, 1100, typeof(RawFishSteak), 1044476, 1, 1044253);
        AddRes(index, typeof(BaseBeverage), 1046458, 1, 1044253);
        SetNeededExpansion(index, Expansion.SE);
        SetNeedOven(index, true);

        index = AddCraft(typeof(WhiteMisoSoup), 1044497, 1030318, 600, 1100, typeof(RawFishSteak), 1044476, 1,
          1044253);
        AddRes(index, typeof(BaseBeverage), 1046458, 1, 1044253);
        SetNeededExpansion(index, Expansion.SE);
        SetNeedOven(index, true);

        index = AddCraft(typeof(RedMisoSoup), 1044497, 1030319, 600, 1100, typeof(RawFishSteak), 1044476, 1,
          1044253);
        AddRes(index, typeof(BaseBeverage), 1046458, 1, 1044253);
        SetNeededExpansion(index, Expansion.SE);
        SetNeedOven(index, true);

        index = AddCraft(typeof(AwaseMisoSoup), 1044497, 1030320, 600, 1100, typeof(RawFishSteak), 1044476, 1,
          1044253);
        AddRes(index, typeof(BaseBeverage), 1046458, 1, 1044253);
        SetNeededExpansion(index, Expansion.SE);
        SetNeedOven(index, true);
      }
      /* End Baking */

      /* Begin Barbecue */
      index = AddCraft(typeof(CookedBird), 1044498, 1022487, 0, 1000, typeof(RawBird), 1044470, 1, 1044253);
      SetNeedHeat(index, true);
      SetUseAllRes(index, true);

      index = AddCraft(typeof(ChickenLeg), 1044498, 1025640, 0, 1000, typeof(RawChickenLeg), 1044473, 1, 1044253);
      SetNeedHeat(index, true);
      SetUseAllRes(index, true);

      index = AddCraft(typeof(FishSteak), 1044498, 1022427, 0, 1000, typeof(RawFishSteak), 1044476, 1, 1044253);
      SetNeedHeat(index, true);
      SetUseAllRes(index, true);

      index = AddCraft(typeof(FriedEggs), 1044498, 1022486, 0, 1000, typeof(Eggs), 1044477, 1, 1044253);
      SetNeedHeat(index, true);
      SetUseAllRes(index, true);

      index = AddCraft(typeof(LambLeg), 1044498, 1025642, 0, 1000, typeof(RawLambLeg), 1044478, 1, 1044253);
      SetNeedHeat(index, true);
      SetUseAllRes(index, true);

      index = AddCraft(typeof(Ribs), 1044498, 1022546, 0, 1000, typeof(RawRibs), 1044485, 1, 1044253);
      SetNeedHeat(index, true);
      SetUseAllRes(index, true);
      /* End Barbecue */

      /* Begin Chocolatiering */
      if (Core.ML)
      {
        index = AddCraft(typeof(DarkChocolate), 1080001, 1079994, 150, 1000, typeof(SackOfSugar), 1079997, 1,
          1044253);
        AddRes(index, typeof(CocoaButter), 1079998, 1, 1044253);
        AddRes(index, typeof(CocoaLiquor), 1079999, 1, 1044253);
        SetItemHue(index, 0x465);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(MilkChocolate), 1080001, 1079995, 325, 1075, typeof(SackOfSugar), 1079997, 1,
          1044253);
        AddRes(index, typeof(CocoaButter), 1079998, 1, 1044253);
        AddRes(index, typeof(CocoaLiquor), 1079999, 1, 1044253);
        AddRes(index, typeof(BaseBeverage), 1022544, 1, 1044253);
        SetBeverageType(index, BeverageType.Milk);
        SetItemHue(index, 0x461);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(WhiteChocolate), 1080001, 1079996, 525, 1275, typeof(SackOfSugar), 1079997, 1,
          1044253);
        AddRes(index, typeof(CocoaButter), 1079998, 1, 1044253);
        AddRes(index, typeof(Vanilla), 1080000, 1, 1044253);
        AddRes(index, typeof(BaseBeverage), 1022544, 1, 1044253);
        SetBeverageType(index, BeverageType.Milk);
        SetItemHue(index, 0x47E);
        SetNeededExpansion(index, Expansion.ML);
      }

      /* End Chocolatiering */
    }
  }
}
