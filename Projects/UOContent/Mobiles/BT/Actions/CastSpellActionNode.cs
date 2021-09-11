using System;
using System.Collections.Generic;
using System.Linq;
using Server.Spells;

namespace Server.Mobiles.BT
{
    public class CastSpellActionNode : ActionNode
    {
        public delegate Spell GetSpellCallback(BaseCreature mob);
        private GetSpellCallback getSpellCallback;
        public CastSpellActionNode(BehaviorTree tree, GetSpellCallback callback) : base(tree)
        {
            getSpellCallback = callback;
        }

        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (mob.Spell == null)
            {
                Spell spell = getSpellCallback(mob);

                if (mob.Mana < spell.ScaleMana(spell.GetMana()))
                {
                    mob.DebugSay("Not enough mana...");
                    return Result.Failure;
                }

                if (Core.TickCount - mob.NextSpellTime < 0)
                {
                    mob.DebugSay("You have not recovered from casting a spell...");
                    return Result.Running;
                }

                mob.DebugSay("Casting...");
                if (!spell.Cast())
                {
                    mob.DebugSay("Failed to cast...");
                    return Result.Running;
                }

                if (!string.IsNullOrEmpty(spell.Mantra))
                    mob.PublicOverheadMessage(Network.MessageType.Spell, 0, false, spell.Mantra, false);

                return Result.Running;
            }
            else if (mob.Spell.IsCasting)
            {
                mob.DebugSay("Still casting...");
                return Result.Running;
            }
            else if (!mob.Spell.IsCasting)
            {
                mob.DebugSay("Finished casting...");
                return Result.Success;
            }

            return Result.Failure;
        }
    }
}
