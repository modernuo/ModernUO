using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Engines.MLQuests.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseCraftmansSatchel : Backpack
{
    protected static readonly Type[] m_TalismanType = { typeof(RandomTalisman) };

    public BaseCraftmansSatchel() => Hue = Utility.RandomBrightHue();

    protected void AddBaseLoot(params Type[][] lootSets)
    {
        var loot = Loot.Construct(lootSets.RandomElement());

        if (loot == null)
        {
            return;
        }

        RewardBag.Enhance(loot);
        DropItem(loot);
    }

    protected void AddRecipe(CraftSystem system)
    {
        // TODO: change craftable artifact recipes to a rarer drop
        var recipeID = system.RandomRecipe();

        if (recipeID != -1)
        {
            DropItem(new RecipeScroll(recipeID));
        }
    }
}

[SerializationGenerator(0, false)]
public partial class TailorSatchel : BaseCraftmansSatchel
{
    [Constructible]
    public TailorSatchel()
    {
        AddBaseLoot(Loot.MLArmorTypes, Loot.JewelryTypes, m_TalismanType);

        if (Utility.RandomBool())
        {
            AddRecipe(DefTailoring.CraftSystem);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class BlacksmithSatchel : BaseCraftmansSatchel
{
    [Constructible]
    public BlacksmithSatchel()
    {
        AddBaseLoot(Loot.MLWeaponTypes, Loot.JewelryTypes, m_TalismanType);

        if (Utility.RandomBool())
        {
            AddRecipe(DefBlacksmithy.CraftSystem);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class TinkerSatchel : BaseCraftmansSatchel
{
    [Constructible]
    public TinkerSatchel()
    {
        AddBaseLoot(Loot.MLArmorTypes, Loot.MLWeaponTypes, Loot.MLRangedWeaponTypes, Loot.JewelryTypes, m_TalismanType);

        if (Utility.RandomBool())
        {
            switch (Utility.Random(6))
            {
                case 0:
                    AddRecipe(DefInscription.CraftSystem);
                    break;
                case 1:
                    AddRecipe(DefAlchemy.CraftSystem);
                    break;
                // TODO
                // case 2: AddNonArtifactRecipe( DefTailoring.CraftSystem ); break;
                // case 3: AddNonArtifactRecipe( DefBlacksmithy.CraftSystem ); break;
                // case 4: AddNonArtifactRecipe( DefCarpentry.CraftSystem ); break;
                // case 5: AddNonArtifactRecipe( DefBowFletching.CraftSystem ); break;
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class FletchingSatchel : BaseCraftmansSatchel
{
    [Constructible]
    public FletchingSatchel()
    {
        AddBaseLoot(Loot.MLRangedWeaponTypes, Loot.JewelryTypes, m_TalismanType);

        if (Utility.RandomBool())
        {
            AddRecipe(DefBowFletching.CraftSystem);
        }

        // TODO: runic fletching kit
    }
}

[SerializationGenerator(0, false)]
public partial class CarpentrySatchel : BaseCraftmansSatchel
{
    [Constructible]
    public CarpentrySatchel()
    {
        AddBaseLoot(Loot.MLArmorTypes, Loot.MLWeaponTypes, Loot.MLRangedWeaponTypes, Loot.JewelryTypes, m_TalismanType);

        if (Utility.RandomBool())
        {
            AddRecipe(DefCarpentry.CraftSystem);
        }

        // TODO: Add runic dovetail saw
    }
}
