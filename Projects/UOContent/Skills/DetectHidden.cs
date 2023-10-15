using System;
using Server.Factions;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class DetectHidden
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.DetectHidden].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile src)
        {
            src.SendLocalizedMessage(500819); // Where will you search?
            src.Target = new InternalTarget();

            return TimeSpan.FromSeconds(6.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget() : base(12, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile src, object targ)
            {
                var foundAnyone = false;

                Point3D p = targ switch
                {
                    Mobile mobile => mobile.Location,
                    Item item     => item.Location,
                    IPoint3D d    => new Point3D(d),
                    _             => src.Location
                };

                var srcSkill = src.Skills.DetectHidden.Value;
                var range = (int)(srcSkill / 10.0);

                if (!src.CheckSkill(SkillName.DetectHidden, 0.0, 100.0))
                {
                    range /= 2;
                }

                var house = BaseHouse.FindHouseAt(p, src.Map, 16);

                var inHouse = house?.IsFriend(src) == true;

                if (inHouse)
                {
                    range = 22;
                }

                if (range > 0)
                {
                    var inRange = src.Map.GetMobilesInRange(p, range);

                    foreach (var trg in inRange)
                    {
                        if (trg.Hidden && src != trg)
                        {
                            var ss = srcSkill + Utility.Random(21) - 10;
                            var ts = trg.Skills.Hiding.Value + Utility.Random(21) - 10;

                            if (src.AccessLevel >= trg.AccessLevel && (ss >= ts || inHouse && house.IsInside(trg)))
                            {
                                if (trg is ShadowKnight && (trg.X != p.X || trg.Y != p.Y))
                                {
                                    continue;
                                }

                                trg.RevealingAction();
                                trg.SendLocalizedMessage(500814); // You have been revealed!
                                foundAnyone = true;
                            }
                        }
                    }

                    if (Faction.Find(src) != null)
                    {
                        var itemsInRange = src.Map.GetItemsInRange<BaseFactionTrap>(p, range);

                        foreach (var trap in itemsInRange)
                        {
                            if (src.CheckTargetSkill(SkillName.DetectHidden, trap, 80.0, 100.0))
                            {
                                src.SendLocalizedMessage(
                                    1042712,
                                    true,
                                    $" {(trap.Faction == null ? "" : trap.Faction.Definition.FriendlyName)}"
                                ); // You reveal a trap placed by a faction:

                                trap.Visible = true;
                                trap.BeginConceal();

                                foundAnyone = true;
                            }
                        }
                    }
                }

                if (!foundAnyone)
                {
                    src.SendLocalizedMessage(500817); // You can see nothing hidden there.
                }
            }
        }
    }
}
