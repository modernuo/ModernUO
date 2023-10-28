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
    private static readonly NeedDelegate[] m_ACure = { m_Cure };
    private static readonly NeedDelegate[] m_AGHeal = { m_GHeal };
    private static readonly NeedDelegate[] m_ALHeal = { m_LHeal };
    private static readonly NeedDelegate[] m_All = { m_Cure, m_GHeal, m_LHeal };

    public HealerAI(BaseCreature m) : base(m)
    {
    }

    public override bool Think()
    {
        if (m_Mobile.Deleted)
        {
            return false;
        }

        var targ = m_Mobile.Target;

        if (targ != null)
        {
            var spellTarg = targ as ISpellTarget;

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
                targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }

            return true;
        }

        var toHelp = Find(m_All);

        if (toHelp != null)
        {
            if (NeedCure(toHelp))
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"{toHelp.Name} needs a cure");
                }

                if (!new CureSpell(m_Mobile).Cast())
                {
                    new CureSpell(m_Mobile).Cast();
                }
            }
            else if (NeedGHeal(toHelp))
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"{toHelp.Name} needs a greater heal");
                }

                if (!new GreaterHealSpell(m_Mobile).Cast())
                {
                    new HealSpell(m_Mobile).Cast();
                }
            }
            else if (NeedLHeal(toHelp))
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"{toHelp.Name} needs a lesser heal");
                }

                new HealSpell(m_Mobile).Cast();
            }

            return true;
        }

        if (!AcquireFocusMob(m_Mobile.RangePerception, FightMode.Weakest, false, true, false))
        {
            WalkRandomInHome(3, 2, 1);
            return true;
        }

        WalkMobileRange(m_Mobile.FocusMob, 1, false, 4, 7);

        // TODO: Should it be able to do this?
        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, m_Mobile.Combatant))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I used my abilities on {m_Mobile.Combatant.Name}!");
            }
        }

        return true;
    }

    private void ProcessTarget(Target targ, NeedDelegate[] func)
    {
        var toHelp = Find(func);

        if (toHelp == null)
        {
            targ.Cancel(m_Mobile, TargetCancelType.Canceled);
        }
        else if (targ.Range != -1 && !m_Mobile.InRange(toHelp, targ.Range))
        {
            DoMove(m_Mobile.GetDirectionTo(toHelp) | Direction.Running);
        }
        else
        {
            targ.Invoke(m_Mobile, toHelp);
        }
    }

    private Mobile Find(params NeedDelegate[] funcs)
    {
        if (m_Mobile.Deleted)
        {
            return null;
        }

        var map = m_Mobile.Map;

        if (map == null)
        {
            return null;
        }

        var prio = 0.0;
        Mobile found = null;

        foreach (var m in m_Mobile.GetMobilesInRange(m_Mobile.RangePerception))
        {
            if (!m_Mobile.CanSee(m) || m is not BaseCreature bc || bc.Team != m_Mobile.Team)
            {
                continue;
            }

            for (var i = 0; i < funcs.Length; ++i)
            {
                if (funcs[i](bc))
                {
                    var val = -m_Mobile.GetDistanceToSqrt(bc);

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
