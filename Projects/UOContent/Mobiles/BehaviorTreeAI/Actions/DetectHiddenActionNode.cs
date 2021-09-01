using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    class DetectHiddenActionNode : BehaviorTreeNode
    {
        public TimeSpan Cooldown { get; private set; }
        public DetectHiddenActionNode(BehaviorTree tree, TimeSpan cooldown) : base(tree)
        {
            object nextDetectTime;
            if (!Tree.Blackboard.TryGetValue("nextDetectTime", out nextDetectTime))
            {
                tree.Blackboard.Add("nextDetectTime", DateTime.Now);
            }
            Cooldown = cooldown;
        }
        public override Result Execute()
        {
            /*
            if (m_Mobile.Deleted || m_Mobile.Map == null)
            {
                return;
            }

            m_Mobile.DebugSay("Checking for hidden players");

            var srcSkill = m_Mobile.Skills.DetectHidden.Value;

            if (srcSkill <= 0)
            {
                return;
            }

            var eable = m_Mobile.GetMobilesInRange(m_Mobile.RangePerception);

            foreach (var trg in eable)
            {
                if (trg != m_Mobile && trg.Player && trg.Alive && trg.Hidden && trg.AccessLevel == AccessLevel.Player &&
                    m_Mobile.InLOS(trg))
                {
                    m_Mobile.DebugSay("Trying to detect {0}", trg.Name);

                    var trgHiding = trg.Skills.Hiding.Value / 2.9;
                    var trgStealth = trg.Skills.Stealth.Value / 1.8;

                    var chance = srcSkill / 1.2 - Math.Min(trgHiding, trgStealth);

                    if (chance < srcSkill / 10)
                    {
                        chance = srcSkill / 10;
                    }

                    chance /= 100;

                    if (chance > Utility.RandomDouble())
                    {
                        trg.RevealingAction();
                        trg.SendLocalizedMessage(500814); // You have been revealed!
                    }
                }
            }

            eable.Free();
            */

            Tree.Mobile.DebugSay("DetectHiddenActionNode");

            object nextDetectTime;

            if (Tree.Blackboard.TryGetValue("nextDetectTime", out nextDetectTime))
            {
                if (DateTime.Now >= (DateTime)nextDetectTime)
                {
                    var srcSkill = Tree.Mobile.Skills.DetectHidden.Value;

                    var eable = Tree.Mobile.GetMobilesInRange(Tree.Mobile.RangePerception);

                    foreach (var trg in eable)
                    {
                        if (trg != Tree.Mobile && trg.Player && trg.Alive && trg.Hidden && trg.AccessLevel == AccessLevel.Player &&
            Tree.Mobile.InLOS(trg))
                        {
                            Tree.Mobile.DebugSay("DetectHiddenActionNode {0}", trg.Name);

                            var trgHiding = trg.Skills.Hiding.Value / 2.9;
                            var trgStealth = trg.Skills.Stealth.Value / 1.8;

                            var chance = srcSkill / 1.2 - Math.Min(trgHiding, trgStealth);

                            if (chance < srcSkill / 10)
                            {
                                chance = srcSkill / 10;
                            }

                            chance /= 100;

                            if (chance > Utility.RandomDouble())
                            {
                                trg.RevealingAction();
                                trg.SendLocalizedMessage(500814); // You have been revealed!
                            }
                        }
                    }

                    return Result.Success;
                }

                Tree.Mobile.DebugSay("DetectHiddenActionNode Cooldown");
            }

            return Result.Failure;
        }
    }
}
