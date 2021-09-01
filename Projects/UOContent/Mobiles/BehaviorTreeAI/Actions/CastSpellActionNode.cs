using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class CastSpellActionNode : ActionNode
    {
        private Func<BaseCreature, Spell> getSpellCallback;
        public CastSpellActionNode(BehaviorTree tree, Func<BaseCreature, Spell> callback) : base(tree)
        {
            object nextCastTime;
            if (!Tree.Blackboard.TryGetValue("nextCastTime", out nextCastTime))
            {
                tree.Blackboard.Add("nextCastTime", DateTime.Now);
            }
            getSpellCallback = callback;
        }

        public override Result Execute()
        {
            object nextCastTime;

            if (Tree.Blackboard.TryGetValue("nextCastTime", out nextCastTime) && DateTime.Now < (DateTime)nextCastTime)
            {
                Tree.Mobile.DebugSay("Spells are on cooldown...");
                return Result.Failure;
            }

            if (Tree.Mobile.Spell == null)
            {
                Spell spell = getSpellCallback(Tree.Mobile);

                Tree.Mobile.DebugSay("Casting...");
                if (!spell.Cast())
                {
                    Tree.Mobile.DebugSay("Failed to cast...");
                    return Result.Failure;
                }

                if (!string.IsNullOrEmpty(spell.Mantra))
                    Tree.Mobile.PublicOverheadMessage(Network.MessageType.Spell, 0, false, spell.Mantra, false);

                return Result.Running;
            }
            else if (Tree.Mobile.Spell.IsCasting)
            {
                Tree.Mobile.DebugSay("Still casting...");
                return Result.Running;
            }
            else if (!Tree.Mobile.Spell.IsCasting)
            {
                Tree.Mobile.DebugSay("Finished casting...");
                return Result.Success;
            }

            return Result.Failure;
        }
    }
}
