using Server.Engines.Quests.Hag;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Harvest
{
    public class HarvestTarget : Target
    {
        private readonly HarvestSystem m_System;
        private readonly Item m_Tool;

        public HarvestTarget(Item tool, HarvestSystem system) : base(-1, true, TargetFlags.None)
        {
            m_Tool = tool;
            m_System = system;

            DisallowMultis = true;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_System is Mining && targeted is StaticTarget target)
            {
                var itemID = target.ItemID;

                // grave
                if (itemID == 0xED3 || itemID == 0xEDF || itemID == 0xEE0 || itemID == 0xEE1 || itemID == 0xEE2 ||
                    itemID == 0xEE8)
                {
                    if (from is PlayerMobile player)
                    {
                        var qs = player.Quest;
                        if (!(qs is WitchApprenticeQuest))
                        {
                            return;
                        }

                        var obj = qs.FindObjective<FindIngredientObjective>();

                        if (obj?.Completed == false && obj.Ingredient == Ingredient.Bones)
                        {
                            // You finish your grim work, finding some of the specific bones listed in the Hag's recipe.
                            player.SendLocalizedMessage(1055037);
                            obj.Complete();

                            return;
                        }
                    }
                }
            }

            if (m_System is Lumberjacking && targeted is IChoppable chopable)
            {
                chopable.OnChop(from);
            }
            else if (m_System is Lumberjacking && targeted is IAxe obj && m_Tool is BaseAxe axe)
            {
                var item = (Item)obj;

                if (!item.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                }
                else if (obj.Axe(from, axe))
                {
                    from.PlaySound(0x13E);
                }
            }
            else if (m_System is Lumberjacking && targeted is ICarvable carvable)
            {
                carvable.Carve(from, m_Tool);
            }
            else if (m_System is Lumberjacking && FurnitureAttribute.Check(targeted as Item))
            {
                DestroyFurniture(from, (Item)targeted);
            }
            else if (m_System is Mining && targeted is TreasureMap map)
            {
                map.OnBeginDig(from);
            }
            else
            {
                m_System.StartHarvesting(from, m_Tool, targeted);
            }
        }

        private void DestroyFurniture(Mobile from, Item item)
        {
            if (!from.InRange(item.GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (!item.IsChildOf(from.Backpack) && !item.Movable)
            {
                from.SendLocalizedMessage(500462); // You can't destroy that while it is here.
                return;
            }

            from.SendLocalizedMessage(500461); // You destroy the item.
            Effects.PlaySound(item.GetWorldLocation(), item.Map, 0x3B3);

            if (item is Container container)
            {
                if (container is TrappableContainer trappableContainer)
                {
                    trappableContainer.ExecuteTrap(from);
                }

                container.Destroy();
            }
            else
            {
                item.Delete();
            }
        }
    }
}
