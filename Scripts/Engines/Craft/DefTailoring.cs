using System;
using Server.Items;

namespace Server.Engines.Craft
{
  public class DefTailoring : CraftSystem
  {
    private static CraftSystem m_CraftSystem;

    private static Type[] m_TailorColorables =
    {
      typeof(GozaMatEastDeed), typeof(GozaMatSouthDeed),
      typeof(SquareGozaMatEastDeed), typeof(SquareGozaMatSouthDeed),
      typeof(BrocadeGozaMatEastDeed), typeof(BrocadeGozaMatSouthDeed),
      typeof(BrocadeSquareGozaMatEastDeed), typeof(BrocadeSquareGozaMatSouthDeed)
    };

    private DefTailoring() : base(1, 1, 1.25) // base( 1, 1, 4.5 )
    {
    }

    public override SkillName MainSkill => SkillName.Tailoring;

    public override int GumpTitleNumber => 1044005;

    public static CraftSystem CraftSystem => m_CraftSystem ?? (m_CraftSystem = new DefTailoring());

    public override CraftECA ECA => CraftECA.ChanceMinusSixtyToFourtyFive;

    public override double GetChanceAtMin(CraftItem item)
    {
      return 0.5; // 50%
    }

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
      if (tool?.Deleted != false || tool.UsesRemaining < 0)
        return 1044038; // You have worn out your tool!
      if (!BaseTool.CheckAccessible(tool, from))
        return 1044263; // The tool must be on your person to use.

      return 0;
    }

    public override bool RetainsColorFrom(CraftItem item, Type type)
    {
      if (type != typeof(Cloth) && type != typeof(UncutCloth))
        return false;

      type = item.ItemType;

      bool contains = false;

      for (int i = 0; !contains && i < m_TailorColorables.Length; ++i)
        contains = m_TailorColorables[i] == type;

      return contains;
    }

    public override void PlayCraftEffect(Mobile from)
    {
      from.PlaySound(0x248);
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
      int index;

      #region Hats

      AddCraft(typeof(SkullCap), 1011375, 1025444, 0, 250, typeof(Cloth), 1044286, 2, 1044287);
      AddCraft(typeof(Bandana), 1011375, 1025440, 0, 250, typeof(Cloth), 1044286, 2, 1044287);
      AddCraft(typeof(FloppyHat), 1011375, 1025907, 62, 312, typeof(Cloth), 1044286, 11, 1044287);
      AddCraft(typeof(Cap), 1011375, 1025909, 62, 312, typeof(Cloth), 1044286, 11, 1044287);
      AddCraft(typeof(WideBrimHat), 1011375, 1025908, 62, 312, typeof(Cloth), 1044286, 12, 1044287);
      AddCraft(typeof(StrawHat), 1011375, 1025911, 62, 312, typeof(Cloth), 1044286, 10, 1044287);
      AddCraft(typeof(TallStrawHat), 1011375, 1025910, 67, 317, typeof(Cloth), 1044286, 13, 1044287);
      AddCraft(typeof(WizardsHat), 1011375, 1025912, 72, 322, typeof(Cloth), 1044286, 15, 1044287);
      AddCraft(typeof(Bonnet), 1011375, 1025913, 62, 312, typeof(Cloth), 1044286, 11, 1044287);
      AddCraft(typeof(FeatheredHat), 1011375, 1025914, 62, 312, typeof(Cloth), 1044286, 12, 1044287);
      AddCraft(typeof(TricorneHat), 1011375, 1025915, 62, 312, typeof(Cloth), 1044286, 12, 1044287);
      AddCraft(typeof(JesterHat), 1011375, 1025916, 72, 322, typeof(Cloth), 1044286, 15, 1044287);

      if (Core.AOS)
        AddCraft(typeof(FlowerGarland), 1011375, 1028965, 100, 350, typeof(Cloth), 1044286, 5, 1044287);

      if (Core.SE)
      {
        index = AddCraft(typeof(ClothNinjaHood), 1011375, 1030202, 800, 1050, typeof(Cloth), 1044286, 13, 1044287);
        SetNeededExpansion(index, Expansion.SE);

        index = AddCraft(typeof(Kasa), 1011375, 1030211, 600, 850, typeof(Cloth), 1044286, 12, 1044287);
        SetNeededExpansion(index, Expansion.SE);
      }

      #endregion

      #region Shirts

      AddCraft(typeof(Doublet), 1015269, 1028059, 0, 250, typeof(Cloth), 1044286, 8, 1044287);
      AddCraft(typeof(Shirt), 1015269, 1025399, 207, 457, typeof(Cloth), 1044286, 8, 1044287);
      AddCraft(typeof(FancyShirt), 1015269, 1027933, 248, 498, typeof(Cloth), 1044286, 8, 1044287);
      AddCraft(typeof(Tunic), 1015269, 1028097, 000, 250, typeof(Cloth), 1044286, 12, 1044287);
      AddCraft(typeof(Surcoat), 1015269, 1028189, 82, 332, typeof(Cloth), 1044286, 14, 1044287);
      AddCraft(typeof(PlainDress), 1015269, 1027937, 124, 374, typeof(Cloth), 1044286, 10, 1044287);
      AddCraft(typeof(FancyDress), 1015269, 1027935, 331, 581, typeof(Cloth), 1044286, 12, 1044287);
      AddCraft(typeof(Cloak), 1015269, 1025397, 414, 664, typeof(Cloth), 1044286, 14, 1044287);
      AddCraft(typeof(Robe), 1015269, 1027939, 539, 789, typeof(Cloth), 1044286, 16, 1044287);
      AddCraft(typeof(JesterSuit), 1015269, 1028095, 82, 332, typeof(Cloth), 1044286, 24, 1044287);

      if (Core.AOS)
      {
        AddCraft(typeof(FurCape), 1015269, 1028969, 350, 600, typeof(Cloth), 1044286, 13, 1044287);
        AddCraft(typeof(GildedDress), 1015269, 1028973, 375, 625, typeof(Cloth), 1044286, 16, 1044287);
        AddCraft(typeof(FormalShirt), 1015269, 1028975, 260, 510, typeof(Cloth), 1044286, 16, 1044287);
      }

      if (Core.SE)
      {
        index = AddCraft(typeof(ClothNinjaJacket), 1015269, 1030207, 750, 1000, typeof(Cloth), 1044286, 12,
          1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(Kamishimo), 1015269, 1030212, 750, 1000, typeof(Cloth), 1044286, 15, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(HakamaShita), 1015269, 1030215, 400, 650, typeof(Cloth), 1044286, 14, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(MaleKimono), 1015269, 1030189, 500, 750, typeof(Cloth), 1044286, 16, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(FemaleKimono), 1015269, 1030190, 500, 750, typeof(Cloth), 1044286, 16, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(JinBaori), 1015269, 1030220, 300, 550, typeof(Cloth), 1044286, 12, 1044287);
        SetNeededExpansion(index, Expansion.SE);
      }

      #endregion

      #region Pants

      AddCraft(typeof(ShortPants), 1015279, 1025422, 248, 498, typeof(Cloth), 1044286, 6, 1044287);
      AddCraft(typeof(LongPants), 1015279, 1025433, 248, 498, typeof(Cloth), 1044286, 8, 1044287);
      AddCraft(typeof(Kilt), 1015279, 1025431, 207, 457, typeof(Cloth), 1044286, 8, 1044287);
      AddCraft(typeof(Skirt), 1015279, 1025398, 290, 540, typeof(Cloth), 1044286, 10, 1044287);

      if (Core.AOS)
        AddCraft(typeof(FurSarong), 1015279, 1028971, 350, 600, typeof(Cloth), 1044286, 12, 1044287);

      if (Core.SE)
      {
        index = AddCraft(typeof(Hakama), 1015279, 1030213, 500, 750, typeof(Cloth), 1044286, 16, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(TattsukeHakama), 1015279, 1030214, 500, 750, typeof(Cloth), 1044286, 16, 1044287);
        SetNeededExpansion(index, Expansion.SE);
      }

      #endregion

      #region Misc

      AddCraft(typeof(BodySash), 1015283, 1025441, 41, 291, typeof(Cloth), 1044286, 4, 1044287);
      AddCraft(typeof(HalfApron), 1015283, 1025435, 207, 457, typeof(Cloth), 1044286, 6, 1044287);
      AddCraft(typeof(FullApron), 1015283, 1025437, 290, 540, typeof(Cloth), 1044286, 10, 1044287);

      if (Core.SE)
      {
        index = AddCraft(typeof(Obi), 1015283, 1030219, 200, 450, typeof(Cloth), 1044286, 6, 1044287);
        SetNeededExpansion(index, Expansion.SE);
      }

      if (Core.ML)
      {
        index = AddCraft(typeof(ElvenQuiver), 1015283, 1032657, 650, 1150, typeof(Leather), 1044462, 28, 1044463);
        AddRecipe(index, 501);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(QuiverOfFire), 1015283, 1073109, 650, 1150, typeof(Leather), 1044462, 28, 1044463);
        AddRes(index, typeof(FireRuby), 1032695, 15, 1042081);
        AddRecipe(index, 502);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(QuiverOfIce), 1015283, 1073110, 650, 1150, typeof(Leather), 1044462, 28, 1044463);
        AddRes(index, typeof(WhitePearl), 1032694, 15, 1042081);
        AddRecipe(index, 503);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(QuiverOfBlight), 1015283, 1073111, 650, 1150, typeof(Leather), 1044462, 28,
          1044463);
        AddRes(index, typeof(Blight), 1032675, 10, 1042081);
        AddRecipe(index, 504);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(QuiverOfLightning), 1015283, 1073112, 650, 1150, typeof(Leather), 1044462, 28,
          1044463);
        AddRes(index, typeof(Corruption), 1032676, 10, 1042081);
        AddRecipe(index, 505);
        SetNeededExpansion(index, Expansion.ML);
      }

      AddCraft(typeof(OilCloth), 1015283, 1041498, 746, 996, typeof(Cloth), 1044286, 1, 1044287);

      if (Core.SE)
      {
        index = AddCraft(typeof(GozaMatEastDeed), 1015283, 1030404, 550, 800, typeof(Cloth), 1044286, 25, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(GozaMatSouthDeed), 1015283, 1030405, 550, 800, typeof(Cloth), 1044286, 25,
          1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(SquareGozaMatEastDeed), 1015283, 1030407, 550, 800, typeof(Cloth), 1044286, 25,
          1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(SquareGozaMatSouthDeed), 1015283, 1030406, 550, 800, typeof(Cloth), 1044286, 25,
          1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(BrocadeGozaMatEastDeed), 1015283, 1030408, 550, 800, typeof(Cloth), 1044286, 25,
          1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(BrocadeGozaMatSouthDeed), 1015283, 1030409, 550, 800, typeof(Cloth), 1044286, 25,
          1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(BrocadeSquareGozaMatEastDeed), 1015283, 1030411, 550, 800, typeof(Cloth), 1044286,
          25, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(BrocadeSquareGozaMatSouthDeed), 1015283, 1030410, 550, 800, typeof(Cloth), 1044286,
          25, 1044287);
        SetNeededExpansion(index, Expansion.SE);
      }

      #endregion

      #region Footwear

      if (Core.AOS)
        AddCraft(typeof(FurBoots), 1015288, 1028967, 500, 750, typeof(Cloth), 1044286, 12, 1044287);

      if (Core.SE)
      {
        index = AddCraft(typeof(NinjaTabi), 1015288, 1030210, 700, 950, typeof(Cloth), 1044286, 10, 1044287);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(SamuraiTabi), 1015288, 1030209, 200, 450, typeof(Cloth), 1044286, 6, 1044287);
        SetNeededExpansion(index, Expansion.SE);
      }

      AddCraft(typeof(Sandals), 1015288, 1025901, 124, 374, typeof(Leather), 1044462, 4, 1044463);
      AddCraft(typeof(Shoes), 1015288, 1025904, 165, 415, typeof(Leather), 1044462, 6, 1044463);
      AddCraft(typeof(Boots), 1015288, 1025899, 331, 581, typeof(Leather), 1044462, 8, 1044463);
      AddCraft(typeof(ThighBoots), 1015288, 1025906, 414, 664, typeof(Leather), 1044462, 10, 1044463);

      #endregion

      #region Leather Armor

      if (Core.ML)
      {
        index = AddCraft(typeof(SpellWovenBritches), 1015293, 1072929, 925, 1175, typeof(Leather), 1044462, 15,
          1044463);
        AddRes(index, typeof(EyeOfTheTravesty), 1032685, 1, 1044253);
        AddRes(index, typeof(Putrefication), 1032678, 10, 1044253);
        AddRes(index, typeof(Scourge), 1032677, 10, 1044253);
        AddRareRecipe(index, 506);
        ForceNonExceptional(index);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(SongWovenMantle), 1015293, 1072931, 925, 1175, typeof(Leather), 1044462, 15,
          1044463);
        AddRes(index, typeof(EyeOfTheTravesty), 1032685, 1, 1044253);
        AddRes(index, typeof(Blight), 1032675, 10, 1044253);
        AddRes(index, typeof(Muculent), 1032680, 10, 1044253);
        AddRareRecipe(index, 507);
        ForceNonExceptional(index);
        SetNeededExpansion(index, Expansion.ML);

        index = AddCraft(typeof(StitchersMittens), 1015293, 1072932, 925, 1175, typeof(Leather), 1044462, 15,
          1044463);
        AddRes(index, typeof(CapturedEssence), 1032686, 1, 1044253);
        AddRes(index, typeof(Corruption), 1032676, 10, 1044253);
        AddRes(index, typeof(Taint), 1032679, 10, 1044253);
        AddRareRecipe(index, 508);
        ForceNonExceptional(index);
        SetNeededExpansion(index, Expansion.ML);
      }

      AddCraft(typeof(LeatherGorget), 1015293, 1025063, 539, 789, typeof(Leather), 1044462, 4, 1044463);
      AddCraft(typeof(LeatherCap), 1015293, 1027609, 62, 312, typeof(Leather), 1044462, 2, 1044463);
      AddCraft(typeof(LeatherGloves), 1015293, 1025062, 518, 768, typeof(Leather), 1044462, 3, 1044463);
      AddCraft(typeof(LeatherArms), 1015293, 1025061, 539, 789, typeof(Leather), 1044462, 4, 1044463);
      AddCraft(typeof(LeatherLegs), 1015293, 1025067, 663, 913, typeof(Leather), 1044462, 10, 1044463);
      AddCraft(typeof(LeatherChest), 1015293, 1025068, 705, 955, typeof(Leather), 1044462, 12, 1044463);

      if (Core.SE)
      {
        index = AddCraft(typeof(LeatherJingasa), 1015293, 1030177, 450, 700, typeof(Leather), 1044462, 4, 1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherMempo), 1015293, 1030181, 800, 1050, typeof(Leather), 1044462, 8, 1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherDo), 1015293, 1030182, 750, 1000, typeof(Leather), 1044462, 12, 1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherHiroSode), 1015293, 1030185, 550, 800, typeof(Leather), 1044462, 5,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherSuneate), 1015293, 1030193, 680, 930, typeof(Leather), 1044462, 12,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherHaidate), 1015293, 1030197, 680, 930, typeof(Leather), 1044462, 12,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherNinjaPants), 1015293, 1030204, 800, 1050, typeof(Leather), 1044462, 13,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherNinjaJacket), 1015293, 1030206, 850, 1100, typeof(Leather), 1044462, 13,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherNinjaBelt), 1015293, 1030203, 500, 750, typeof(Leather), 1044462, 5,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherNinjaMitts), 1015293, 1030205, 650, 900, typeof(Leather), 1044462, 12,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(LeatherNinjaHood), 1015293, 1030201, 900, 1150, typeof(Leather), 1044462, 14,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
      }

      #endregion

      #region Studded Armor

      AddCraft(typeof(StuddedGorget), 1015300, 1025078, 788, 1038, typeof(Leather), 1044462, 6, 1044463);
      AddCraft(typeof(StuddedGloves), 1015300, 1025077, 829, 1079, typeof(Leather), 1044462, 8, 1044463);
      AddCraft(typeof(StuddedArms), 1015300, 1025076, 871, 1121, typeof(Leather), 1044462, 10, 1044463);
      AddCraft(typeof(StuddedLegs), 1015300, 1025082, 912, 1162, typeof(Leather), 1044462, 12, 1044463);
      AddCraft(typeof(StuddedChest), 1015300, 1025083, 940, 1190, typeof(Leather), 1044462, 14, 1044463);

      if (Core.SE)
      {
        index = AddCraft(typeof(StuddedMempo), 1015300, 1030216, 800, 1050, typeof(Leather), 1044462, 8, 1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(StuddedDo), 1015300, 1030183, 950, 1200, typeof(Leather), 1044462, 14, 1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(StuddedHiroSode), 1015300, 1030186, 850, 1100, typeof(Leather), 1044462, 8,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(StuddedSuneate), 1015300, 1030194, 920, 1170, typeof(Leather), 1044462, 14,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
        index = AddCraft(typeof(StuddedHaidate), 1015300, 1030198, 920, 1170, typeof(Leather), 1044462, 14,
          1044463);
        SetNeededExpansion(index, Expansion.SE);
      }

      #endregion

      #region Female Armor

      AddCraft(typeof(LeatherShorts), 1015306, 1027168, 622, 872, typeof(Leather), 1044462, 8, 1044463);
      AddCraft(typeof(LeatherSkirt), 1015306, 1027176, 580, 830, typeof(Leather), 1044462, 6, 1044463);
      AddCraft(typeof(LeatherBustierArms), 1015306, 1027178, 580, 830, typeof(Leather), 1044462, 6, 1044463);
      AddCraft(typeof(StuddedBustierArms), 1015306, 1027180, 829, 1079, typeof(Leather), 1044462, 8, 1044463);
      AddCraft(typeof(FemaleLeatherChest), 1015306, 1027174, 622, 872, typeof(Leather), 1044462, 8, 1044463);
      AddCraft(typeof(FemaleStuddedChest), 1015306, 1027170, 871, 1121, typeof(Leather), 1044462, 10, 1044463);

      #endregion

      #region Bone Armor

      index = AddCraft(typeof(BoneHelm), 1049149, 1025206, 850, 1100, typeof(Leather), 1044462, 4, 1044463);
      AddRes(index, typeof(Bone), 1049064, 2, 1049063);

      index = AddCraft(typeof(BoneGloves), 1049149, 1025205, 890, 1140, typeof(Leather), 1044462, 6, 1044463);
      AddRes(index, typeof(Bone), 1049064, 2, 1049063);

      index = AddCraft(typeof(BoneArms), 1049149, 1025203, 920, 1170, typeof(Leather), 1044462, 8, 1044463);
      AddRes(index, typeof(Bone), 1049064, 4, 1049063);

      index = AddCraft(typeof(BoneLegs), 1049149, 1025202, 950, 1200, typeof(Leather), 1044462, 10, 1044463);
      AddRes(index, typeof(Bone), 1049064, 6, 1049063);

      index = AddCraft(typeof(BoneChest), 1049149, 1025199, 960, 1210, typeof(Leather), 1044462, 12, 1044463);
      AddRes(index, typeof(Bone), 1049064, 10, 1049063);

      index = AddCraft(typeof(OrcHelm), 1049149, 1027947, 900, 1150, typeof(Leather), 1044462, 6, 1044463);
      AddRes(index, typeof(Bone), 1049064, 4, 1049063);

      #endregion

      // Set the overridable material
      SetSubRes(typeof(Leather), 1049150);

      // Add every material you want the player to be able to choose from
      // This will override the overridable material
      AddSubRes(typeof(Leather), 1049150, 0, 1044462, 1049311);
      AddSubRes(typeof(SpinedLeather), 1049151, 650, 1044462, 1049311);
      AddSubRes(typeof(HornedLeather), 1049152, 800, 1044462, 1049311);
      AddSubRes(typeof(BarbedLeather), 1049153, 990, 1044462, 1049311);

      MarkOption = true;
      Repair = Core.AOS;
      CanEnhance = Core.AOS;
    }
  }
}
