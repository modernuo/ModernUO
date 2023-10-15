using System;
using Server.Items;
using Server.Targeting;
using Server.Utilities;

namespace Server.Engines.Harvest
{
    public abstract class HarvestSystem
    {
        public HarvestDefinition[] Definitions { get; init; }

        public virtual bool CheckTool(Mobile from, Item tool)
        {
            var wornOut = tool?.Deleted != false || (tool as IUsesRemaining)?.UsesRemaining <= 0;

            if (wornOut)
            {
                from.SendLocalizedMessage(1044038); // You have worn out your tool!
            }

            return !wornOut;
        }

        public virtual bool CheckHarvest(Mobile from, Item tool) => CheckTool(from, tool);

        public virtual bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest) =>
            CheckTool(from, tool);

        public virtual bool CheckRange(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            var inRange = from.Map == map && from.InRange(loc, def.MaxRange);

            if (!inRange)
            {
                def.SendMessageTo(from, timed ? def.TimedOutOfRangeMessage : def.OutOfRangeMessage);
            }

            return inRange;
        }

        public virtual bool CheckResources(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            var bank = def.GetBank(map, loc.X, loc.Y);
            var available = bank?.Current >= def.ConsumedPerHarvest;

            if (!available)
            {
                def.SendMessageTo(from, timed ? def.DoubleHarvestMessage : def.NoResourcesMessage);
            }

            return available;
        }

        public virtual void OnBadHarvestTarget(Mobile from, Item tool, object toHarvest)
        {
        }

        public virtual object GetLock(Mobile from, Item tool, HarvestDefinition def, object toHarvest) => tool;

        public virtual void OnConcurrentHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
        }

        public virtual void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
        }

        public virtual bool BeginHarvesting(Mobile from, Item tool)
        {
            if (!CheckHarvest(from, tool))
            {
                return false;
            }

            from.Target = new HarvestTarget(tool, this);
            return true;
        }

        public virtual void FinishHarvesting(Mobile from, Item tool, HarvestDefinition def, object toHarvest, object locked)
        {
            from.EndAction(locked);

            if (!CheckHarvest(from, tool))
            {
                return;
            }

            if (!GetHarvestDetails(from, tool, toHarvest, out var tileID, out var map, out var loc, out var isLand))
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }

            if (!def.Validate(tileID, isLand))
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }

            if (!CheckRange(from, tool, def, map, loc, true))
            {
                return;
            }

            if (!CheckResources(from, tool, def, map, loc, true))
            {
                return;
            }

            if (!CheckHarvest(from, tool, def, toHarvest))
            {
                return;
            }

            if (SpecialHarvest(from, tool, def, map, loc))
            {
                return;
            }

            var bank = def.GetBank(map, loc.X, loc.Y);

            if (bank == null)
            {
                return;
            }

            var vein = bank.Vein;

            if (vein != null)
            {
                vein = MutateVein(from, tool, def, bank, toHarvest, vein);
            }

            if (vein == null)
            {
                return;
            }

            var primary = vein.PrimaryResource;
            var fallback = vein.FallbackResource;
            var resource = MutateResource(from, tool, def, map, loc, vein, primary, fallback);

            var skillBase = from.Skills[def.Skill].Base;
            // double skillValue = from.Skills[def.Skill].Value;

            Type type = null;

            if (skillBase >= resource.ReqSkill && from.CheckSkill(def.Skill, resource.MinSkill, resource.MaxSkill))
            {
                type = GetResourceType(from, tool, def, map, loc, resource);

                if (type != null)
                {
                    type = MutateType(type, from, tool, def, map, loc, resource);
                }

                if (type != null)
                {
                    var item = Construct(type, from);

                    if (item == null)
                    {
                        type = null;
                    }
                    else
                    {
                        // The whole harvest system is kludgy and I'm sure this is just adding to it.
                        if (item.Stackable)
                        {
                            var amount = def.ConsumedPerHarvest;
                            var feluccaAmount = def.ConsumedPerFeluccaHarvest;

                            var racialAmount = (int)Math.Ceiling(amount * 1.1);
                            var feluccaRacialAmount = (int)Math.Ceiling(feluccaAmount * 1.1);

                            var eligableForRacialBonus = def.RaceBonus && from.Race == Race.Human;
                            var inFelucca = map == Map.Felucca;

                            if (eligableForRacialBonus && inFelucca && bank.Current >= feluccaRacialAmount &&
                                Utility.RandomDouble() < 0.1)
                            {
                                item.Amount = feluccaRacialAmount;
                            }
                            else if (inFelucca && bank.Current >= feluccaAmount)
                            {
                                item.Amount = feluccaAmount;
                            }
                            else if (eligableForRacialBonus && bank.Current >= racialAmount && Utility.RandomDouble() < 0.1)
                            {
                                item.Amount = racialAmount;
                            }
                            else
                            {
                                item.Amount = amount;
                            }
                        }

                        bank.Consume(item.Amount, from);

                        if (Give(from, item, def.PlaceAtFeetIfFull))
                        {
                            SendSuccessTo(from, item, resource);
                        }
                        else
                        {
                            SendPackFullTo(from, item, def, resource);
                            item.Delete();
                        }

                        var bonus = def.GetBonusResource();

                        if (bonus?.Type != null && skillBase >= bonus.ReqSkill)
                        {
                            var bonusItem = Construct(bonus.Type, from);

                            if (Give(from, bonusItem, true)
                            ) // Bonuses always allow placing at feet, even if pack is full irregrdless of def
                            {
                                bonus.SendSuccessTo(from);
                            }
                            else
                            {
                                item.Delete();
                            }
                        }

                        if (tool is IUsesRemaining toolWithUses)
                        {
                            toolWithUses.ShowUsesRemaining = true;

                            if (toolWithUses.UsesRemaining > 0)
                            {
                                --toolWithUses.UsesRemaining;
                            }

                            if (toolWithUses.UsesRemaining < 1)
                            {
                                tool.Delete();
                                def.SendMessageTo(from, def.ToolBrokeMessage);
                            }
                        }
                    }
                }
            }

            if (type == null)
            {
                def.SendMessageTo(from, def.FailMessage);
            }

            OnHarvestFinished(from, tool, def, vein, bank, resource, toHarvest);
        }

        public virtual void OnHarvestFinished(
            Mobile from, Item tool, HarvestDefinition def, HarvestVein vein,
            HarvestBank bank, HarvestResource resource, object harvested
        )
        {
        }

        public virtual bool SpecialHarvest(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc) => false;

        public virtual Item Construct(Type type, Mobile from)
        {
            try
            {
                return type.CreateInstance<Item>();
            }
            catch
            {
                return null;
            }
        }

        public virtual HarvestVein MutateVein(
            Mobile from, Item tool, HarvestDefinition def, HarvestBank bank,
            object toHarvest, HarvestVein vein
        ) =>
            vein;

        public virtual void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
        {
            resource.SendSuccessTo(from);
        }

        public virtual void SendPackFullTo(Mobile from, Item item, HarvestDefinition def, HarvestResource resource)
        {
            def.SendMessageTo(from, def.PackFullMessage);
        }

        public virtual bool Give(Mobile m, Item item, bool placeAtFeet)
        {
            if (m.PlaceInBackpack(item))
            {
                return true;
            }

            if (!placeAtFeet)
            {
                return false;
            }

            var map = m.Map;

            if (map == null)
            {
                return false;
            }

            var eable = m.GetItemsInRange(0);
            foreach (var i in eable)
            {
                if (i.StackWith(m, i, false))
                {
                    return true;
                }
            }

            item.MoveToWorld(m.Location, map);
            return true;
        }

        public virtual Type MutateType(
            Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc,
            HarvestResource resource
        ) =>
            from.Region.GetResource(type);

        public virtual Type GetResourceType(
            Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc,
            HarvestResource resource
        ) => resource.Types.RandomElement();

        public virtual HarvestResource MutateResource(
            Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc,
            HarvestVein vein, HarvestResource primary, HarvestResource fallback
        )
        {
            var racialBonus = def.RaceBonus && from.Race == Race.Elf;

            if (vein.ChanceToFallback > Utility.RandomDouble() + (racialBonus ? .20 : 0))
            {
                return fallback;
            }

            var skillValue = from.Skills[def.Skill].Value;

            if (fallback != null && (skillValue < primary.ReqSkill || skillValue < primary.MinSkill))
            {
                return fallback;
            }

            return primary;
        }

        public virtual bool OnHarvesting(
            Mobile from, Item tool, HarvestDefinition def, object toHarvest, object locked,
            bool last
        )
        {
            if (!CheckHarvest(from, tool))
            {
                from.EndAction(locked);
                return false;
            }

            if (!GetHarvestDetails(from, tool, toHarvest, out var tileID, out var map, out var loc, out var isLand))
            {
                from.EndAction(locked);
                OnBadHarvestTarget(from, tool, toHarvest);
                return false;
            }

            if (!def.Validate(tileID, isLand))
            {
                from.EndAction(locked);
                OnBadHarvestTarget(from, tool, toHarvest);
                return false;
            }

            if (!CheckRange(from, tool, def, map, loc, true))
            {
                from.EndAction(locked);
                return false;
            }

            if (!CheckResources(from, tool, def, map, loc, true))
            {
                from.EndAction(locked);
                return false;
            }

            if (!CheckHarvest(from, tool, def, toHarvest))
            {
                from.EndAction(locked);
                return false;
            }

            DoHarvestingEffect(from, tool, def, map, loc);

            new HarvestSoundTimer(from, tool, this, def, toHarvest, locked, last).Start();

            return !last;
        }

        public virtual void DoHarvestingSound(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            from.PlaySound(def.EffectSounds.RandomElement(-1));
        }

        public virtual void DoHarvestingEffect(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc)
        {
            from.Direction = from.GetDirectionTo(loc);

            if (!from.Mounted)
            {
                from.Animate(def.EffectActions.RandomElement(), 5, 1, true, false, 0);
            }
        }

        public virtual HarvestDefinition GetDefinition() => Definitions[0];

        public virtual HarvestDefinition GetDefinition(int tileID, bool isLand)
        {
            for (var i = 0; i < Definitions.Length; i++)
            {
                var check = Definitions[i];

                if (check.Validate(tileID, isLand))
                {
                    return check;
                }
            }

            return null;
        }

        public virtual void StartHarvesting(Mobile from, Item tool, object toHarvest)
        {
            if (!CheckHarvest(from, tool))
            {
                return;
            }

            if (!GetHarvestDetails(from, tool, toHarvest, out var tileID, out var map, out var loc, out var isLand))
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }

            var def = GetDefinition(tileID, isLand);

            if (def == null)
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }

            if (!CheckRange(from, tool, def, map, loc, false))
            {
                return;
            }

            if (!CheckResources(from, tool, def, map, loc, false))
            {
                return;
            }

            if (!CheckHarvest(from, tool, def, toHarvest))
            {
                return;
            }

            var toLock = GetLock(from, tool, def, toHarvest);

            if (!from.BeginAction(toLock))
            {
                OnConcurrentHarvest(from, tool, def, toHarvest);
                return;
            }

            new HarvestTimer(from, tool, this, def, toHarvest, toLock).Start();
            OnHarvestStarted(from, tool, def, toHarvest);
        }

        public virtual bool GetHarvestDetails(
            Mobile from, Item tool, object toHarvest, out int tileID, out Map map, out Point3D loc, out bool isLand
        )
        {
            if (toHarvest is Static { Movable: false } staticObj)
            {
                tileID = staticObj.ItemID;
                map = staticObj.Map;
                loc = staticObj.GetWorldLocation();
                isLand = false;
            }
            else if (toHarvest is StaticTarget staticTarget)
            {
                tileID = staticTarget.ItemID;
                map = from.Map;
                loc = staticTarget.Location;
                isLand = false;
            }
            else if (toHarvest is LandTarget landTarget)
            {
                tileID = landTarget.TileID;
                map = from.Map;
                loc = landTarget.Location;
                isLand = true;
            }
            else
            {
                tileID = 0;
                map = null;
                loc = Point3D.Zero;
                isLand = false;
                return false;
            }

            return map != null && map != Map.Internal;
        }
    }
}
