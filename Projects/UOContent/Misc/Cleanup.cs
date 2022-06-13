using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Multis;

namespace Server.Misc
{
    public static class Cleanup
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(Cleanup));

        public static void Initialize()
        {
            Timer.StartTimer(TimeSpan.FromSeconds(2.5), Run);
        }

        public static void Run()
        {
            var items = new List<Item>();
            var validItems = new List<Item>();
            var hairCleanup = new List<Mobile>();

            var boxes = 0;

            foreach (var item in World.Items.Values)
            {
                if (item.Map == null)
                {
                    items.Add(item);
                    continue;
                }

                if (item is CommodityDeed deed)
                {
                    if (deed.Commodity != null)
                    {
                        validItems.Add(deed.Commodity);
                    }

                    continue;
                }

                if (item is BaseHouse house)
                {
                    foreach (var relEntity in house.RelocatedEntities)
                    {
                        if (relEntity.Entity is Item item1)
                        {
                            validItems.Add(item1);
                        }
                    }

                    foreach (var inventory in house.VendorInventories)
                    {
                        foreach (var subItem in inventory.Items)
                        {
                            validItems.Add(subItem);
                        }
                    }
                }
                else if (item is BankBox box)
                {
                    var owner = box.Owner;

                    if (owner == null)
                    {
                        items.Add(box);
                        ++boxes;
                    }
                    else if (box.Items.Count == 0)
                    {
                        items.Add(box);
                        ++boxes;
                    }

                    continue;
                }
                else if (item.Layer is Layer.Hair or Layer.FacialHair)
                {
                    if (item.RootParent is Mobile rootMobile)
                    {
                        if (item.Parent != rootMobile && rootMobile.AccessLevel == AccessLevel.Player)
                        {
                            items.Add(item);
                            continue;
                        }

                        if (item.Parent == rootMobile)
                        {
                            hairCleanup.Add(rootMobile);
                            continue;
                        }
                    }
                }

                if (item.Parent != null || item.Map != Map.Internal || item.HeldBy != null)
                {
                    continue;
                }

                if (item.Location != Point3D.Zero)
                {
                    continue;
                }

                if (!IsBuggable(item))
                {
                    continue;
                }

                items.Add(item);
            }

            for (var i = 0; i < validItems.Count; ++i)
            {
                items.Remove(validItems[i]);
            }

            if (items.Count > 0)
            {
                if (boxes > 0)
                {
                    logger.Information(
                        "Cleanup: Detected {Count} inaccessible items, including {BankBoxes} bank boxes, removing..",
                        items.Count,
                        boxes
                    );
                }
                else
                {
                    logger.Information("Cleanup: Detected {Count} inaccessible items, removing..", items.Count);
                }

                for (var i = 0; i < items.Count; ++i)
                {
                    items[i].Delete();
                }
            }

            if (hairCleanup.Count > 0)
            {
                logger.Information(
                    "Cleanup: Detected {0} hair and facial hair items being worn, converting to their virtual counterparts..",
                    hairCleanup.Count
                );

                for (var i = 0; i < hairCleanup.Count; i++)
                {
                    hairCleanup[i].ConvertHair();
                }
            }
        }

        public static bool IsBuggable(Item item)
        {
            if (item is Fists)
            {
                return false;
            }

            return item is BaseJewel && Core.AOS ||
                   item is ICommodity or BaseBoat or Fish or BigFish or Food or CookableFood or SpecialFishingNet or
                       BaseMagicFish or Shoes or Sandals or Boots or ThighBoots or TreasureMap or MessageInABottle or
                       BaseArmor or BaseWeapon or BaseClothing or SkullPole or EvilIdolSkull or MonsterStatuette or Pier or
                       ArtifactLargeVase or ArtifactVase or MinotaurStatueDeed or SwampTile or WallBlood or
                       TatteredAncientMummyWrapping or LavaTile or DemonSkull or Web or WaterTile or WindSpirit or DirtPatch
                       or Futon;
        }
    }
}
