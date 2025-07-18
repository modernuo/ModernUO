using System;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Targeting;

namespace Server.Mobiles;

public class HealerAI : BaseAI
{
    private static readonly NeedDelegate m_Cure = NeedCure;
    private static readonly NeedDelegate m_GHeal = NeedGHeal;
    private static readonly NeedDelegate m_LHeal = NeedLHeal;
    private static readonly NeedDelegate[] m_ACure = [m_Cure];
    private static readonly NeedDelegate[] m_AGHeal = [m_GHeal];
    private static readonly NeedDelegate[] m_ALHeal = [m_LHeal];
    private static readonly NeedDelegate[] m_All = [m_Cure, m_GHeal, m_LHeal];

    public HealerAI(BaseCreature m) : base(m)
    {
    }

    public override bool Think()
    {
        if (_mobile.Deleted)
        {
            return false;
        }

        var targ = _mobile.Target;

        if (targ != null)
        {
            var spellTarg = targ as ISpellTarget<Mobile>;

            if (spellTarg?.Spell is CureSpell)
            {
                ProcessTarget(targ, m_ACure);
            }
            else if (spellTarg?.Spell is GreaterHealSpell)
            {
                ProcessTarget(targ, m_AGHeal);
            }
            else if (spellTarg?.Spell is HealSpell)
            {
                ProcessTarget(targ, m_ALHeal);
            }
            else
            {
                targ.Cancel(_mobile, TargetCancelType.Canceled);
            }

            return true;
        }

        var toHelp = Find(m_All);

        if (toHelp != null)
        {
            if (NeedCure(toHelp))
            {
                DebugSay($"{toHelp.Name} needs a cure");

                if (!new CureSpell(_mobile).Cast())
                {
                    new CureSpell(_mobile).Cast();
                }
            }
            else if (NeedGHeal(toHelp))
            {
                DebugSay($"{toHelp.Name} needs a greater heal");

                if (!new GreaterHealSpell(_mobile).Cast())
                {
                    new HealSpell(_mobile).Cast();
                }
            }
            else if (NeedLHeal(toHelp))
            {
                DebugSay($"{toHelp.Name} needs a lesser heal");

                new HealSpell(_mobile).Cast();
            }

            return true;
        }

        if (!AcquireFocusMob(_mobile.RangePerception, FightMode.Weakest, false, true, false))
        {
            WalkRandomInHome(3, 2, 1);
            return true;
        }

        WalkMobileRange(_mobile.FocusMob, 1, false, 4, 7);

        // TODO: Should it be able to do this?
        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, _mobile.Combatant))
        {
            DebugSay($"I used my abilities on {_mobile.Combatant.Name}!");
        }

        return true;
    }

    private void ProcessTarget(Target targ, NeedDelegate[] func)
    {
        var toHelp = Find(func);

        if (toHelp == null)
        {
            targ.Cancel(_mobile, TargetCancelType.Canceled);
        }
        else if (targ.Range != -1 && !_mobile.InRange(toHelp, targ.Range))
        {
            DoMove(_mobile.GetDirectionTo(toHelp) | Direction.Running);
        }
        else
        {
            targ.Invoke(_mobile, toHelp);
        }
    }

    private Mobile Find(params ReadOnlySpan<NeedDelegate> funcs)
    {
        if (_mobile.Deleted)
        {
            return null;
        }

        var map = _mobile.Map;

        if (map == null)
        {
            return null;
        }

        var prio = 0.0;
        Mobile found = null;

        foreach (var m in _mobile.GetMobilesInRange(_mobile.RangePerception))
        {
            if (!_mobile.CanSee(m) || m is not BaseCreature bc || bc.Team != _mobile.Team)
            {
                continue;
            }

            for (var i = 0; i < funcs.Length; ++i)
            {
                if (funcs[i](bc))
                {
                    var val = -_mobile.GetDistanceToSqrt(bc);

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
