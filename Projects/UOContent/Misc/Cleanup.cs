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
            Timer.DelayCall(TimeSpan.FromSeconds(2.5), Run);
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
                else if (item.Layer == Layer.Hair || item.Layer == Layer.FacialHair)
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
                        "Cleanup: Detected {0} inaccessible items, including {1} bank boxes, removing..",
                        items.Count,
                        boxes
                    );
                }
                else
                {
                    logger.Information("Cleanup: Detected {0} inaccessible items, removing..", items.Count);
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

            if (item is ICommodity || item is BaseBoat
                                   || item is Fish || item is BigFish || item is Food || item is CookableFood
                                   || item is SpecialFishingNet || item is BaseMagicFish
                                   || item is Shoes || item is Sandals
                                   || item is Boots || item is ThighBoots
                                   || item is TreasureMap || item is MessageInABottle
                                   || item is BaseArmor || item is BaseWeapon
                                   || item is BaseClothing
                                   || item is BaseJewel && Core.AOS || item is SkullPole
                                   || item is EvilIdolSkull
                                   || item is MonsterStatuette
                                   || item is Pier
                                   || item is ArtifactLargeVase
                                   || item is ArtifactVase
                                   || item is MinotaurStatueDeed
                                   || item is SwampTile
                                   || item is WallBlood
                                   || item is TatteredAncientMummyWrapping
                                   || item is LavaTile
                                   || item is DemonSkull
                                   || item is Web
                                   || item is WaterTile
                                   || item is WindSpirit
                                   || item is DirtPatch
                                   || item is Futon)
            {
                return true;
            }

            return false;
        }
    }
}
