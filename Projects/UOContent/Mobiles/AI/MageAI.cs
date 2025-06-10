using System;
using Server.Items;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Necromancy;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Targeting;

namespace Server.Mobiles;

public class MageAI : BaseAI
{
    private const double HealChance = 0.10;     // 10% chance to heal at gm magery
    private const double TeleportChance = 0.05; // 5% chance to teleport at gm magery
    private const double DispelChance = 0.75;   // 75% chance to dispel at gm magery
    private const double InvisChance = 0.50; // 50% chance to invis at gm magery

    private static readonly int[] m_Offsets =
    {
        -1, -1,
        -1, 0,
        -1, 1,
        0, -1,
        0, 1,
        1, -1,
        1, 0,
        1, 1,

        -2, -2,
        -2, -1,
        -2, 0,
        -2, 1,
        -2, 2,
        -1, -2,
        -1, 2,
        0, -2,
        0, 2,
        1, -2,
        1, 2,
        2, -2,
        2, -1,
        2, 0,
        2, 1,
        2, 2
    };

    protected int m_Combo = -1;

    private Mobile m_LastTarget;
    private Point3D m_LastTargetLoc;
    private long m_NextCastTime;
    private long m_NextHealTime;

    private LandTarget m_RevealTarget;

    public MageAI(BaseCreature m)
        : base(m)
    {
    }

    public virtual bool SmartAI => m_Mobile is BaseVendor or BaseEscortable or Changeling;

    public virtual bool IsNecromancer => Core.AOS && m_Mobile.Skills.Necromancy.Value > 50;

    public override bool Think()
    {
        if (m_Mobile.Deleted)
        {
            return false;
        }

        return ProcessTarget() || base.Think();
    }

    public virtual double ScaleBySkill(double v, SkillName skill) => v * m_Mobile.Skills[skill].Value / 100;

    public override bool DoActionWander()
    {
        if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
        {
            DebugSay($"I am going to attack {m_Mobile.FocusMob.Name}");

            m_Mobile.Combatant = m_Mobile.FocusMob;
            Action = ActionType.Combat;
            m_NextCastTime = Core.TickCount;
        }
        else if (SmartAI && m_Mobile.Mana < m_Mobile.ManaMax && !m_Mobile.Meditating)
        {
            DebugSay("I am going to meditate");

            m_Mobile.UseSkill(SkillName.Meditation);
        }
        else
        {
            DebugSay("I am wandering");

            m_Mobile.Warmode = false;

            base.DoActionWander();

            if (Utility.RandomDouble() < 0.05)
            {
                var spell = CheckCastHealingSpell();

                spell?.Cast();
            }
        }

        return true;
    }

    private Spell CheckCastHealingSpell()
    {
        // If I'm poisoned, always attempt to cure.
        if (m_Mobile.Poisoned)
        {
            return new CureSpell(m_Mobile);
        }

        // Summoned creatures never heal themselves.
        if (m_Mobile.Summoned || m_Mobile.Controlled && Core.TickCount - m_NextHealTime < 0)
        {
            return null;
        }

        if (!SmartAI)
        {
            if (HealChance > 0 && ScaleBySkill(HealChance, SkillName.Magery) < Utility.RandomDouble())
            {
                return null;
            }
        }
        else if (Utility.Random(0, 4 + (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : m_Mobile.HitsMax / m_Mobile.Hits)) < 3)
        {
            return null;
        }

        Spell spell = null;

        if (m_Mobile.Hits < m_Mobile.HitsMax - 50)
        {
            if (UseNecromancy())
            {
                m_Mobile.UseSkill(SkillName.SpiritSpeak);
            }
            else
            {
                spell = new GreaterHealSpell(m_Mobile);
            }
        }
        else if (m_Mobile.Hits < m_Mobile.HitsMax - 10)
        {
            spell = new HealSpell(m_Mobile);
        }

        var delay = m_Mobile.Int >= 500 ? Utility.RandomMinMax(7, 10) : Math.Sqrt(600 - m_Mobile.Int);

        m_NextHealTime = Core.TickCount + (int)TimeSpan.FromSeconds(delay).TotalMilliseconds;

        return spell;
    }

    public void RunTo(Mobile m)
    {
        if (!SmartAI)
        {
            if (!MoveTo(m, true, m_Mobile.RangeFight))
            {
                OnFailedMove();
            }

            return;
        }

        if (m.Paralyzed || m.Frozen)
        {
            if (m_Mobile.InRange(m, 1))
            {
                RunFrom(m);
            }
            else if (!m_Mobile.InRange(m, Math.Max(m_Mobile.RangeFight, 2)) && !MoveTo(m, true, 1))
            {
                OnFailedMove();
            }
        }
        else if (!m_Mobile.InRange(m, m_Mobile.RangeFight))
        {
            if (!MoveTo(m, true, 1))
            {
                OnFailedMove();
            }
        }
        else if (m_Mobile.InRange(m, m_Mobile.RangeFight - 1))
        {
            RunFrom(m);
        }
    }

    public void RunFrom(Mobile m)
    {
        Run((m_Mobile.GetDirectionTo(m) - 4) & Direction.Mask);
    }

    public void OnFailedMove()
    {
        if (!m_Mobile.DisallowAllMoves && (SmartAI
                ? Utility.Random(4) == 0
                : TeleportChance > 0 && ScaleBySkill(TeleportChance, SkillName.Magery) > Utility.RandomDouble()))
        {
            m_Mobile.Target?.Cancel(m_Mobile, TargetCancelType.Canceled);

            new TeleportSpell(m_Mobile).Cast();

            DebugSay("I am stuck, I'm going to try teleporting away");
        }
        else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
        {
            DebugSay($"My move is blocked, so I am going to attack {m_Mobile.FocusMob.Name}");

            m_Mobile.Combatant = m_Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            DebugSay("I am stuck");
        }
    }

    public void Run(Direction d)
    {
        if (m_Mobile.Spell?.IsCasting == true || m_Mobile.Paralyzed || m_Mobile.Frozen ||
            m_Mobile.DisallowAllMoves)
        {
            return;
        }

        m_Mobile.Direction = d | Direction.Running;

        if (!DoMove(m_Mobile.Direction, true))
        {
            OnFailedMove();
        }
    }

    public virtual bool UseNecromancy()
    {
        var magery = m_Mobile.Skills.Magery.BaseFixedPoint;
        var necro = m_Mobile.Skills.Necromancy.BaseFixedPoint;
        return IsNecromancer && Utility.Random(magery + necro) >= magery;
    }

    public virtual Spell GetRandomDamageSpell() =>
        UseNecromancy() ? GetRandomDamageSpellNecro() : GetRandomDamageSpellMage();

    public virtual Spell GetRandomDamageSpellNecro()
    {
        var bound = m_Mobile.Skills.Necromancy.Value >= 100 ? 5 : 3;

        return Utility.Random(bound) switch
        {
            0 => new PainSpikeSpell(m_Mobile),
            1 => new PoisonStrikeSpell(m_Mobile),
            2 => new StrangleSpell(m_Mobile),
            3 => new WitherSpell(m_Mobile),
            _ => new VengefulSpiritSpell(m_Mobile)
        };
    }

    public virtual Spell GetRandomDamageSpellMage()
    {
        var maxCircle = Math.Clamp((int)((m_Mobile.Skills.Magery.Value + 20.0) / (100.0 / 7.0)), 1, 8);

        return Utility.Random(maxCircle * 2) switch
        {
            0  => new MagicArrowSpell(m_Mobile),
            1  => new MagicArrowSpell(m_Mobile),
            2  => new HarmSpell(m_Mobile),
            3  => new HarmSpell(m_Mobile),
            4  => new FireballSpell(m_Mobile),
            5  => new FireballSpell(m_Mobile),
            6  => new LightningSpell(m_Mobile),
            7  => new LightningSpell(m_Mobile),
            8  => new MindBlastSpell(m_Mobile),
            9  => new MindBlastSpell(m_Mobile),
            10 => new EnergyBoltSpell(m_Mobile),
            11 => new ExplosionSpell(m_Mobile),
            _  => new FlameStrikeSpell(m_Mobile)
        };
    }

    public virtual Spell GetRandomCurseSpell() =>
        UseNecromancy() ? GetRandomCurseSpellNecro() : GetRandomCurseSpellMage();

    public virtual Spell GetRandomCurseSpellNecro()
    {
        return Utility.Random(4) switch
        {
            0 => new BloodOathSpell(m_Mobile),
            1 => new CorpseSkinSpell(m_Mobile),
            2 => new EvilOmenSpell(m_Mobile),
            _ => new MindRotSpell(m_Mobile)
        };
    }

    public virtual Spell GetRandomCurseSpellMage()
    {
        if (m_Mobile.Skills.Magery.Value >= 40.0 && Utility.Random(4) == 0)
        {
            return new CurseSpell(m_Mobile);
        }

        return Utility.Random(3) switch
        {
            0 => new WeakenSpell(m_Mobile),
            1 => new ClumsySpell(m_Mobile),
            _ => new FeeblemindSpell(m_Mobile)
        };
    }

    public virtual Spell GetRandomManaDrainSpell()
    {
        if (m_Mobile.Skills.Magery.Value >= 80.0 && Utility.RandomBool())
        {
            return new ManaVampireSpell(m_Mobile);
        }

        return new ManaDrainSpell(m_Mobile);
    }

    public virtual Spell DoDispel(Mobile toDispel)
    {
        if (!SmartAI)
        {
            if (DispelChance > 0 && ScaleBySkill(DispelChance, SkillName.Magery) > Utility.RandomDouble())
            {
                return new DispelSpell(m_Mobile);
            }

            return null;
        }

        var spell = CheckCastHealingSpell();

        if (spell != null)
        {
            return spell;
        }

        var distance = (int)m_Mobile.GetDistanceToSqrt(toDispel);
        if (!m_Mobile.DisallowAllMoves && distance > 0 && Utility.Random(distance) == 0)
        {
            return new TeleportSpell(m_Mobile);
        }

        if (Utility.Random(3) == 0 && !m_Mobile.InRange(toDispel, 3) && !toDispel.Paralyzed && !toDispel.Frozen)
        {
            return new ParalyzeSpell(m_Mobile);
        }

        return new DispelSpell(m_Mobile);
    }

    public virtual Spell ChooseSpell(Mobile c)
    {
        Spell spell;

        if (!SmartAI)
        {
            spell = CheckCastHealingSpell();

            if (spell != null)
            {
                return spell;
            }

            if (IsNecromancer)
            {
                var psDamage =
                    (m_Mobile.Skills.SpiritSpeak.Value - c.Skills.MagicResist.Value) / 10 +
                    (c.Player ? 18 : 30);

                if (psDamage > c.Hits)
                {
                    return new PainSpikeSpell(m_Mobile);
                }
            }

            switch (Utility.Random(16))
            {
                case 0:
                case 1: // Poison them
                    {
                        if (c.Poisoned)
                        {
                            goto default;
                        }

                        DebugSay("Attempting to poison");

                        spell = new PoisonSpell(m_Mobile);
                        break;
                    }
                case 2: // Bless ourselves
                    {
                        DebugSay("Blessing myself");

                        spell = new BlessSpell(m_Mobile);
                        break;
                    }
                case 3:
                case 4: // Curse them
                    {
                        DebugSay("Attempting to curse");

                        spell = GetRandomCurseSpell();
                        break;
                    }
                case 5: // Paralyze them
                    {
                        if (c.Paralyzed || m_Mobile.Skills.Magery.Value <= 50.0)
                        {
                            goto default;
                        }

                        DebugSay("Attempting to paralyze");

                        spell = new ParalyzeSpell(m_Mobile);
                        break;
                    }
                case 6: // Drain mana
                    {
                        DebugSay("Attempting to drain mana");

                        spell = GetRandomManaDrainSpell();
                        break;
                    }
                case 7: // Invis ourselves
                    {
                        if (InvisChance <= 0 || ScaleBySkill(InvisChance, SkillName.Magery) <= Utility.RandomDouble())
                        {
                            goto default;
                        }

                        DebugSay("Attempting to invis myself");

                        spell = new InvisibilitySpell(m_Mobile);
                        break;
                    }
                default: // Damage them
                    {
                        DebugSay("Just doing damage");

                        spell = GetRandomDamageSpell();
                        break;
                    }
            }
        }
        else
        {
            spell = CheckCastHealingSpell();

            if (spell == null)
            {
                switch (Utility.Random(3))
                {
                    case 0: // Poison them
                        {
                            if (c.Poisoned)
                            {
                                goto case 1;
                            }

                            spell = new PoisonSpell(m_Mobile);
                            break;
                        }
                    case 1: // Deal some damage
                        {
                            spell = GetRandomDamageSpell();
                            break;
                        }
                    default: // Set up a combo
                        {
                            if (m_Mobile.Mana is > 15 and < 40)
                            {
                                if (c.Paralyzed && !c.Poisoned && !m_Mobile.Meditating)
                                {
                                    DebugSay("I am going to meditate");

                                    m_Mobile.UseSkill(SkillName.Meditation);
                                }
                                else if (!c.Poisoned)
                                {
                                    spell = new ParalyzeSpell(m_Mobile);
                                }
                            }
                            else if (m_Mobile.Mana > 60)
                            {
                                if (Utility.RandomBool() && !c.Paralyzed && !c.Frozen && !c.Poisoned)
                                {
                                    m_Combo = 0;
                                    spell = new ParalyzeSpell(m_Mobile);
                                }
                                else
                                {
                                    m_Combo = 1;
                                    spell = new ExplosionSpell(m_Mobile);
                                }
                            }

                            break;
                        }
                }
            }
        }

        if (spell != null)
        {
            DebugSay($"Casting {spell.Name}");
        }
        else
        {
            DebugSay("I don't have a spell to use!");
        }

        return spell;
    }

    public virtual Spell DoCombo(Mobile c)
    {
        Spell spell = null;

        if (m_Combo == 0)
        {
            spell = new ExplosionSpell(m_Mobile);
            ++m_Combo; // Move to next spell
        }
        else if (m_Combo == 1)
        {
            spell = new WeakenSpell(m_Mobile);
            ++m_Combo; // Move to next spell
        }
        else if (m_Combo == 2)
        {
            if (!c.Poisoned)
            {
                spell = new PoisonSpell(m_Mobile);
            }
            else if (IsNecromancer)
            {
                spell = new StrangleSpell(m_Mobile);
            }

            ++m_Combo; // Move to next spell
        }

        if (m_Combo == 3 && spell == null)
        {
            switch (Utility.Random(IsNecromancer ? 4 : 3))
            {
                case 0:
                    {
                        if (c.Int < c.Dex)
                        {
                            spell = new FeeblemindSpell(m_Mobile);
                        }
                        else
                        {
                            spell = new ClumsySpell(m_Mobile);
                        }

                        ++m_Combo; // Move to next spell

                        break;
                    }
                case 1:
                    {
                        spell = new EnergyBoltSpell(m_Mobile);
                        m_Combo = -1; // Reset combo state
                        break;
                    }
                case 2:
                    {
                        spell = new FlameStrikeSpell(m_Mobile);
                        m_Combo = -1; // Reset combo state
                        break;
                    }
                default:
                    {
                        spell = new PainSpikeSpell(m_Mobile);
                        m_Combo = -1; // Reset combo state
                        break;
                    }
            }
        }
        else if (m_Combo == 4 && spell == null)
        {
            spell = new MindBlastSpell(m_Mobile);
            m_Combo = -1;
        }

        return spell;
    }

    private TimeSpan GetDelay(Spell spell)
    {
        if (SmartAI || spell is DispelSpell)
        {
            return TimeSpan.FromSeconds(m_Mobile.ActiveSpeed);
        }

        var del = ScaleBySkill(3.0, SkillName.Magery);
        var min = 6.0 - del * 0.75;
        var max = 6.0 - del * 1.25;

        return TimeSpan.FromSeconds(min + (max - min) * Utility.RandomDouble());
    }

    public override bool DoActionCombat()
    {
        var c = m_Mobile.Combatant;
        m_Mobile.Warmode = true;

        if (c?.Deleted != false || !c.Alive || c.IsDeadBondedPet || !m_Mobile.CanSee(c) ||
            !m_Mobile.CanBeHarmful(c, false) || c.Map != m_Mobile.Map)
        {
            // Our combatant is deleted, dead, hidden, or we cannot hurt them
            // Try to find another combatant

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.Combatant = c = m_Mobile.FocusMob!;

                DebugSay($"Something happened to my combatant, so I am going to fight {c.Name}");

                m_Mobile.FocusMob = null;
            }
            else
            {
                DebugSay("Something happened to my combatant, and nothing is around. I am on guard.");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!m_Mobile.InLOS(c))
        {
            DebugSay("I can't see my target");

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.Combatant = c = m_Mobile.FocusMob!;

                DebugSay($"I will switch to {c.Name}");

                m_Mobile.FocusMob = null;
            }
        }

        if (!Core.AOS && SmartAI && !m_Mobile.StunReady && m_Mobile.Skills.Wrestling.Value >= 80.0 &&
            m_Mobile.Skills.Anatomy.Value >= 80.0)
        {
            Fists.StunRequest(m_Mobile);
        }

        if (!m_Mobile.InRange(c, m_Mobile.RangePerception))
        {
            // They are somewhat far away, can we find something else?

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.Combatant = m_Mobile.FocusMob;
                m_Mobile.FocusMob = null;
            }
            else if (!m_Mobile.InRange(c, m_Mobile.RangePerception * 3))
            {
                m_Mobile.Combatant = null;
            }

            c = m_Mobile.Combatant;

            if (c == null)
            {
                DebugSay("My combatant has fled, so I am on guard");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!m_Mobile.Controlled && !m_Mobile.Summoned && m_Mobile.CanFlee && m_Mobile.Hits < m_Mobile.HitsMax * 20 / 100)
        {
            // We are low on health, should we flee?
            // (10 + diff)% chance to flee
            var fleeChance = 10 + Math.Max(0, c.Hits - m_Mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                DebugSay($"I am going to flee from {c.Name}");

                Action = ActionType.Flee;
                return true;
            }
        }

        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, c))
        {
            DebugSay("I used my abilities!");
        }
        else if (m_Mobile.Spell == null && Core.TickCount - m_NextCastTime >= 0)
        {
            // We are ready to cast a spell
            Spell spell;
            var toDispel = FindDispelTarget(true);

            if (m_Mobile.Poisoned) // Top cast priority is cure
            {
                DebugSay("I am going to cure myself");

                spell = new CureSpell(m_Mobile);
            }
            else if (toDispel != null) // Something dispellable is attacking us
            {
                DebugSay($"I am going to dispel {toDispel}");

                spell = DoDispel(toDispel); // May return null if dumb AI and doesn't have enough skill
            }
            else
            {
                spell = null;
            }

            spell ??= SmartAI switch
            {
                // We are doing a spell combo
                true when m_Combo != -1 => DoCombo(c),
                // They have a heal spell out
                true when !c.Poisoned && c.Spell is HealSpell or GreaterHealSpell => new PoisonSpell(m_Mobile),
                _ => ChooseSpell(c)
            };

            // Now we have a spell picked
            var range = (spell as IRangedSpell)?.TargetRange ?? m_Mobile.RangePerception;

            // Move first before casting
            if (!SmartAI || toDispel == null)
            {
                RunTo(c);
            }
            else if (m_Mobile.InRange(toDispel, Math.Min(10, range)))
            {
                RunFrom(toDispel);
            }
            else if (!m_Mobile.InRange(toDispel, range))
            {
                RunTo(toDispel);
            }

            // After running, make sure we are still in range
            if (spell == null || m_Mobile.InRange(c, range))
            {
                spell?.Cast();

                // Even if we don't have a spell, delay the next potential cast
                m_NextCastTime = Core.TickCount + (int)GetDelay(spell).TotalMilliseconds;
            }
        }
        else if (m_Mobile.Spell?.IsCasting != true)
        {
            RunTo(c);
        }

        if (m_Mobile.Spell != null || !m_Mobile.InRange(c, 1) || Core.TickCount - m_Mobile.LastMoveTime > 800)
        {
            m_Mobile.Direction = m_Mobile.GetDirectionTo(c);
        }

        m_LastTarget = c;
        m_LastTargetLoc = c.Location;

        return true;
    }

    public override bool DoActionGuard()
    {
        if (m_LastTarget?.Hidden == true)
        {
            var map = m_Mobile.Map;

            if (map == null || !m_Mobile.InRange(m_LastTargetLoc, m_Mobile.RangePerception))
            {
                m_LastTarget = null;
            }
            else if (m_Mobile.Spell == null && Core.TickCount - m_NextCastTime >= 0)
            {
                DebugSay("I am going to reveal my last target");

                m_RevealTarget = new LandTarget(m_LastTargetLoc, map);
                Spell spell = new RevealSpell(m_Mobile);

                if (spell.Cast())
                {
                    m_LastTarget = null; // only do it once
                }

                m_NextCastTime = Core.TickCount + (int)GetDelay(spell).TotalMilliseconds;
            }
        }

        if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
        {
            DebugSay($"I am going to attack {m_Mobile.FocusMob.Name}");

            m_Mobile.Combatant = m_Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            if (!m_Mobile.Controlled)
            {
                ProcessTarget();

                var spell = CheckCastHealingSpell();

                spell?.Cast();
            }

            base.DoActionGuard();
        }

        return true;
    }

    public override bool DoActionFlee()
    {
        // Mobile c = m_Mobile.Combatant;

        if ((m_Mobile.Mana > 20 || m_Mobile.Mana == m_Mobile.ManaMax) && m_Mobile.Hits > m_Mobile.HitsMax / 2)
        {
            DebugSay("I am stronger now, my guard is up");

            Action = ActionType.Guard;
        }
        else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
        {
            DebugSay($"I am scared of {m_Mobile.FocusMob.Name}");

            RunFrom(m_Mobile.FocusMob);
            m_Mobile.FocusMob = null;

            if (m_Mobile.Poisoned && Utility.Random(0, 5) == 0)
            {
                new CureSpell(m_Mobile).Cast();
            }
        }
        else
        {
            DebugSay("Area seems clear, but my guard is up");

            Action = ActionType.Guard;
            m_Mobile.Warmode = true;
        }

        return true;
    }

    public Mobile FindDispelTarget(bool activeOnly)
    {
        if (m_Mobile.Deleted || m_Mobile.Int < 95 || CanDispel(m_Mobile) || m_Mobile.AutoDispel)
        {
            return null;
        }

        if (activeOnly)
        {
            var aggressed = m_Mobile.Aggressed;
            var aggressors = m_Mobile.Aggressors;

            Mobile active = null;
            var activePrio = 0.0;

            var comb = m_Mobile.Combatant;

            if (comb?.Deleted == false && comb.Alive && !comb.IsDeadBondedPet &&
                m_Mobile.InRange(comb, m_Mobile.RangePerception) && CanDispel(comb))
            {
                active = comb;
                activePrio = m_Mobile.GetDistanceToSqrt(comb);

                if (activePrio <= 2)
                {
                    return active;
                }
            }

            for (var i = 0; i < aggressed.Count; ++i)
            {
                var info = aggressed[i];
                var m = info.Defender;

                if (m != comb && m.Combatant == m_Mobile && m_Mobile.InRange(m, m_Mobile.RangePerception) && CanDispel(m))
                {
                    var prio = m_Mobile.GetDistanceToSqrt(m);

                    if (active == null || prio < activePrio)
                    {
                        active = m;
                        activePrio = prio;

                        if (activePrio <= 2)
                        {
                            return active;
                        }
                    }
                }
            }

            for (var i = 0; i < aggressors.Count; ++i)
            {
                var info = aggressors[i];
                var m = info.Attacker;

                if (m != comb && m.Combatant == m_Mobile && m_Mobile.InRange(m, m_Mobile.RangePerception) && CanDispel(m))
                {
                    var prio = m_Mobile.GetDistanceToSqrt(m);

                    if (active == null || prio < activePrio)
                    {
                        active = m;
                        activePrio = prio;

                        if (activePrio <= 2)
                        {
                            return active;
                        }
                    }
                }
            }

            return active;
        }

        var map = m_Mobile.Map;

        if (map != null)
        {
            Mobile active = null, inactive = null;
            double actPrio = 0.0, inactPrio = 0.0;

            var comb = m_Mobile.Combatant;

            if (comb?.Deleted == false && comb.Alive && !comb.IsDeadBondedPet && CanDispel(comb))
            {
                active = inactive = comb;
                actPrio = inactPrio = m_Mobile.GetDistanceToSqrt(comb);
            }

            foreach (var m in m_Mobile.GetMobilesInRange(m_Mobile.RangePerception))
            {
                if (m != m_Mobile && CanDispel(m))
                {
                    var prio = m_Mobile.GetDistanceToSqrt(m);

                    if (inactive == null || prio < inactPrio)
                    {
                        inactive = m;
                        inactPrio = prio;
                    }

                    if ((m_Mobile.Combatant == m || m.Combatant == m_Mobile) && (active == null || prio < actPrio))
                    {
                        active = m;
                        actPrio = prio;
                    }
                }
            }

            return active ?? inactive;
        }

        return null;
    }

    public bool CanDispel(Mobile m) =>
        m is BaseCreature creature && creature.Summoned && creature.SummonMaster != m_Mobile &&
        m_Mobile.CanBeHarmful(creature, false) && !creature.IsAnimatedDead;

    private bool ProcessTarget()
    {
        var targ = m_Mobile.Target;

        if (targ == null)
        {
            return false;
        }

        var spellTarg = targ as ISpellTarget<Mobile>;

        var isReveal = spellTarg?.Spell is RevealSpell;
        var isDispel = spellTarg?.Spell is DispelSpell;
        var isParalyze = spellTarg?.Spell is ParalyzeSpell;
        var isTeleport = spellTarg?.Spell is TeleportSpell;
        var isInvisible = spellTarg?.Spell is InvisibilitySpell;
        var teleportAway = false;

        Mobile toTarget;

        if (isInvisible)
        {
            toTarget = m_Mobile;
        }
        else if (isDispel)
        {
            toTarget = FindDispelTarget(false);

            if (!SmartAI && toTarget != null)
            {
                RunTo(toTarget);
            }
            else if (toTarget != null && m_Mobile.InRange(toTarget, 10))
            {
                RunFrom(toTarget);
            }
        }
        else if (SmartAI && (isParalyze || isTeleport))
        {
            toTarget = FindDispelTarget(true);

            if (toTarget == null)
            {
                toTarget = m_Mobile.Combatant;

                if (toTarget != null)
                {
                    RunTo(toTarget);
                }
            }
            else if (m_Mobile.InRange(toTarget, 10))
            {
                RunFrom(toTarget);
                teleportAway = true;
            }
            else
            {
                teleportAway = true;
            }
        }
        else
        {
            toTarget = m_Mobile.Combatant;

            if (toTarget != null)
            {
                RunTo(toTarget);
            }
        }

        if ((targ.Flags & TargetFlags.Harmful) != 0 && toTarget != null)
        {
            if ((targ.Range == -1 || m_Mobile.InRange(toTarget, targ.Range)) && m_Mobile.CanSee(toTarget) &&
                m_Mobile.InLOS(toTarget))
            {
                targ.Invoke(m_Mobile, toTarget);
            }
            else if (isDispel)
            {
                targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }
        }
        else if ((targ.Flags & TargetFlags.Beneficial) != 0)
        {
            targ.Invoke(m_Mobile, m_Mobile);
        }
        else if (isReveal && m_RevealTarget != null)
        {
            targ.Invoke(m_Mobile, m_RevealTarget);
        }
        else
        {
            var map = m_Mobile.Map;

            if (map != null && isTeleport && toTarget != null)
            {
                var teleRange = targ.Range >= 0 ? targ.Range :
                    Core.ML ? 11 : 12;

                int px, py;

                if (teleportAway)
                {
                    var rx = m_Mobile.X - toTarget.X;
                    var ry = m_Mobile.Y - toTarget.Y;

                    var d = m_Mobile.GetDistanceToSqrt(toTarget);

                    px = toTarget.X + (int)(rx * (10 / d));
                    py = toTarget.Y + (int)(ry * (10 / d));
                }
                else
                {
                    px = toTarget.X;
                    py = toTarget.Y;
                }

                for (var i = 0; i < m_Offsets.Length; i += 2)
                {
                    int x = m_Offsets[i], y = m_Offsets[i + 1];

                    var p = new Point3D(px + x, py + y, 0);

                    var lt = new LandTarget(p, map);

                    if ((targ.Range == -1 || m_Mobile.InRange(p, targ.Range)) && m_Mobile.InLOS(lt) &&
                        map.CanSpawnMobile(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, map))
                    {
                        targ.Invoke(m_Mobile, lt);
                        return true;
                    }
                }

                for (var i = 0; i < 10; ++i)
                {
                    var randomPoint = new Point3D(
                        m_Mobile.X - teleRange + Utility.Random(teleRange * 2 + 1),
                        m_Mobile.Y - teleRange + Utility.Random(teleRange * 2 + 1),
                        0
                    );

                    var lt = new LandTarget(randomPoint, map);

                    if (m_Mobile.InLOS(lt) && map.CanSpawnMobile(lt.X, lt.Y, lt.Z) &&
                        !SpellHelper.CheckMulti(randomPoint, map))
                    {
                        targ.Invoke(m_Mobile, new LandTarget(randomPoint, map));
                        return true;
                    }
                }
            }

            targ.Cancel(m_Mobile, TargetCancelType.Canceled);
        }

        return true;
    }
}
