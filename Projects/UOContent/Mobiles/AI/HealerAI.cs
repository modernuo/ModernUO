using System;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Targeting;

namespace Server.Mobiles;

public class HealerAI : BaseAI
{
    private static readonly NeedDelegate[] Cure = [NeedCure];
    private static readonly NeedDelegate[] GHeal = [NeedGHeal];
    private static readonly NeedDelegate[] LHeal = [NeedLHeal];
    private static readonly NeedDelegate[] AllHeal = [NeedCure, NeedGHeal, NeedLHeal];

    public HealerAI(BaseCreature m) : base(m)
    {
    }

    public override bool Think()
    {
        if (Mobile.Deleted)
        {
            return false;
        }

        var targ = Mobile.Target;

        if (targ != null)
        {
            var spellTarg = targ as ISpellTarget<Mobile>;

            var funcs = spellTarg?.Spell switch
            {
                CureSpell        => Cure,
                GreaterHealSpell => GHeal,
                HealSpell        => LHeal,
                _                => null
            };

            ProcessTarget(targ, funcs);
            return true;
        }

        var toHelp = Find(AllHeal);

        if (toHelp != null)
        {
            if (NeedCure(toHelp))
            {
                this.DebugSayFormatted($"{toHelp.Name} needs a cure");

                if (!new CureSpell(Mobile).Cast())
                {
                    new CureSpell(Mobile).Cast();
                }
            }
            else if (NeedGHeal(toHelp))
            {
                this.DebugSayFormatted($"{toHelp.Name} needs a greater heal");

                if (!new GreaterHealSpell(Mobile).Cast())
                {
                    new HealSpell(Mobile).Cast();
                }
            }
            else if (NeedLHeal(toHelp))
            {
                this.DebugSayFormatted($"{toHelp.Name} needs a lesser heal");

                new HealSpell(Mobile).Cast();
            }

            return true;
        }

        if (!AcquireFocusMob(Mobile.RangePerception, FightMode.Weakest, false, true, false))
        {
            WalkRandomInHome(3, 2, 1);
            return true;
        }

        WalkMobileRange(Mobile.FocusMob, 1, false, 4, 7);

        // TODO: Should it be able to do this?
        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, Mobile.Combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {Mobile.Combatant.Name}!");
        }

        return true;
    }

    private void ProcessTarget(Target targ, NeedDelegate[] func)
    {
        if (func == null || func.Length == 0)
        {
            targ.Cancel(Mobile, TargetCancelType.Canceled);
            return;
        }

        var toHelp = Find(func);

        if (toHelp == null)
        {
            targ.Cancel(Mobile, TargetCancelType.Canceled);
        }
        else if (targ.Range != -1 && !Mobile.InRange(toHelp, targ.Range))
        {
            DoMove(Mobile.GetDirectionTo(toHelp) | Direction.Running);
        }
        else
        {
            targ.Invoke(Mobile, toHelp);
        }
    }

    private Mobile Find(params ReadOnlySpan<NeedDelegate> funcs)
    {
        if (Mobile.Deleted)
        {
            return null;
        }

        var map = Mobile.Map;

        if (map == null)
        {
            return null;
        }

        var prio = 0.0;
        Mobile found = null;

        foreach (var m in Mobile.GetMobilesInRange(Mobile.RangePerception))
        {
            if (!Mobile.CanSee(m) || m is not BaseCreature bc || bc.Team != Mobile.Team)
            {
                continue;
            }

            for (var i = 0; i < funcs.Length; ++i)
            {
                if (funcs[i](bc))
                {
                    var val = -Mobile.GetDistanceToSqrt(bc);

                    if (found == null || val > prio)
                    {
                        prio = val;
                        found = bc;
                    }

                    break;
                }
            }
        }

        return found;
    }

    private static bool NeedCure(Mobile m) => m.Poisoned;

    private static bool NeedGHeal(Mobile m) => m.Hits < m.HitsMax - 40;

    private static bool NeedLHeal(Mobile m) => m.Hits < m.HitsMax - 10;

    private delegate bool NeedDelegate(Mobile m);
}
