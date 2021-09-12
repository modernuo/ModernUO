using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells;

namespace Server.Mobiles.BehaviorAI
{
    public delegate Spell CastSpellCallback(BehaviorTreeContext context);
    public class CastSpell : Behavior
    {
        public CastSpellCallback Callback { get; }
        public CastSpell(BehaviorTree tree, CastSpellCallback callback) : base(tree)
        {
            Callback = callback;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            BaseCreature owner = context.Mobile;

            if (owner.Spell == null)
            {
                Spell spell = Callback(context);

                if (owner.Mana < spell.GetMana())
                {
                    SetResult(context, Result.Failure);
                    owner.DebugSay("Not enough mana...");
                    return;
                }

                if (Core.TickCount < owner.NextSpellTime)
                {
                    SetResult(context, Result.Running);
                    owner.DebugSay("On cooldown...");
                    return;
                }

                if (!spell.Cast())
                {
                    SetResult(context, Result.Running);
                    owner.DebugSay("Failed to cast...");
                    return;
                }

                owner.DebugSay("Casting {0}...", spell.Name);
                if (!string.IsNullOrEmpty(spell.Mantra))
                    owner.PublicOverheadMessage(Network.MessageType.Spell, owner.SpeechHue, false, spell.Mantra, false);

                SetResult(context, Result.Running);
                return;
            }
            else if (owner.Spell.IsCasting)
            {
                SetResult(context, Result.Running);
                owner.DebugSay("Already casting {0}...", ((Spell)owner.Spell).Name);
                return;
            }
            else if (!owner.Spell.IsCasting)
            {
                SetResult(context, Result.Success);
                owner.DebugSay("Finished casting {0}...", ((Spell)owner.Spell).Name);
                return;
            }

            SetResult(context, Result.Failure);
        }
    }
}
