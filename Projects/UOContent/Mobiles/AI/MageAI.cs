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

    private static readonly int[] Offsets =
    [
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
    ];

    protected int _combo = -1;

    private Mobile _lastTarget;
    private Point3D _lastTargetLoc;
    private long _nextCastTime;
    private long _nextHealTime;

    private LandTarget _revealTarget;

    public MageAI(BaseCreature m) : base(m)
    {
    }

    public virtual bool SmartAI => Mobile is BaseVendor or BaseEscortable or Changeling;

    public virtual bool IsNecromancer => Core.AOS && Mobile.Skills.Necromancy.Value > 50;

    public override bool Think()
    {
        if (Mobile.Deleted)
        {
            return false;
        }

        return ProcessTarget() || base.Think();
    }

    public virtual double ScaleBySkill(double v, SkillName skill) => v * Mobile.Skills[skill].Value / 100;

    public override bool DoActionWander()
    {
        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I am going to attack {Mobile.FocusMob.Name}");

            Mobile.Combatant = Mobile.FocusMob;
            Action = ActionType.Combat;
            _nextCastTime = Core.TickCount;
        }
        else if (SmartAI && Mobile.Mana < Mobile.ManaMax && !Mobile.Meditating)
        {
            DebugSay("I am going to meditate");

            Mobile.UseSkill(SkillName.Meditation);
        }
        else
        {
            DebugSay("I am wandering");

            Mobile.Warmode = false;

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
        if (Mobile.Poisoned)
        {
            return new CureSpell(Mobile);
        }

        // Summoned creatures never heal themselves.
        if (Mobile.Summoned || Mobile.Controlled && Core.TickCount - _nextHealTime < 0)
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
        else if (Utility.Random(0, 4 + (Mobile.Hits == 0 ? Mobile.HitsMax : Mobile.HitsMax / Mobile.Hits)) < 3)
        {
            return null;
        }

        Spell spell = null;

        if (Mobile.Hits < Mobile.HitsMax - 50)
        {
            if (UseNecromancy())
            {
                Mobile.UseSkill(SkillName.SpiritSpeak);
            }
            else
            {
                spell = new GreaterHealSpell(Mobile);
            }
        }
        else if (Mobile.Hits < Mobile.HitsMax - 10)
        {
            spell = new HealSpell(Mobile);
        }

        var delay = Mobile.Int >= 500 ? Utility.RandomMinMax(7, 10) : Math.Sqrt(600 - Mobile.Int);

        _nextHealTime = Core.TickCount + (int)TimeSpan.FromSeconds(delay).TotalMilliseconds;

        return spell;
    }

    public void RunTo(Mobile m)
    {
        if (!SmartAI)
        {
            if (!MoveTo(m, false, Mobile.RangeFight))
            {
                OnFailedMove();
            }

            return;
        }

        if (m.Paralyzed || m.Frozen)
        {
            if (Mobile.InRange(m, 1))
            {
                RunFrom(m);
            }
            else if (!Mobile.InRange(m, Math.Max(Mobile.RangeFight, 2)) && !MoveTo(m, false, 1))
            {
                OnFailedMove();
            }
        }
        else if (!Mobile.InRange(m, Mobile.RangeFight))
        {
            if (!MoveTo(m, false, 1))
            {
                OnFailedMove();
            }
        }
        else if (Mobile.InRange(m, Mobile.RangeFight - 1))
        {
            RunFrom(m);
        }
    }

    public void RunFrom(Mobile m)
    {
        Run((Mobile.GetDirectionTo(m) - 4) & Direction.Mask);
    }

    public void OnFailedMove()
    {
        if (!Mobile.DisallowAllMoves && (SmartAI
                ? Utility.Random(4) == 0
                : TeleportChance > 0 && ScaleBySkill(TeleportChance, SkillName.Magery) > Utility.RandomDouble()))
        {
            Mobile.Target?.Cancel(Mobile, TargetCancelType.Canceled);

            new TeleportSpell(Mobile).Cast();

            DebugSay("I am stuck, I'm going to try teleporting away");
        }
        else if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"My move is blocked, so I am going to attack {Mobile.FocusMob.Name}");

            Mobile.Combatant = Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            DebugSay("I am stuck");
        }
    }

    public void Run(Direction d)
    {
        if (Mobile.Spell?.IsCasting == true || Mobile.Paralyzed || Mobile.Frozen ||
            Mobile.DisallowAllMoves)
        {
            return;
        }

        Mobile.Direction = d | Direction.Running;

        if (!DoMove(Mobile.Direction, true))
        {
            OnFailedMove();
        }
    }

    public virtual bool UseNecromancy()
    {
        var magery = Mobile.Skills.Magery.BaseFixedPoint;
        var necro = Mobile.Skills.Necromancy.BaseFixedPoint;
        return IsNecromancer && Utility.Random(magery + necro) >= magery;
    }

    public virtual Spell GetRandomDamageSpell() =>
        UseNecromancy() ? GetRandomDamageSpellNecro() : GetRandomDamageSpellMage();

    public virtual Spell GetRandomDamageSpellNecro()
    {
        var bound = Mobile.Skills.Necromancy.Value >= 100 ? 5 : 3;

        return Utility.Random(bound) switch
        {
            0 => new PainSpikeSpell(Mobile),
            1 => new PoisonStrikeSpell(Mobile),
            2 => new StrangleSpell(Mobile),
            3 => new WitherSpell(Mobile),
            _ => new VengefulSpiritSpell(Mobile)
        };
    }

    public virtual Spell GetRandomDamageSpellMage()
    {
        var maxCircle = Math.Clamp((int)((Mobile.Skills.Magery.Value + 20.0) / (100.0 / 7.0)), 1, 8);

        return Utility.Random(maxCircle * 2) switch
        {
            0  => new MagicArrowSpell(Mobile),
            1  => new MagicArrowSpell(Mobile),
            2  => new HarmSpell(Mobile),
            3  => new HarmSpell(Mobile),
            4  => new FireballSpell(Mobile),
            5  => new FireballSpell(Mobile),
            6  => new LightningSpell(Mobile),
            7  => new LightningSpell(Mobile),
            8  => new MindBlastSpell(Mobile),
            9  => new MindBlastSpell(Mobile),
            10 => new EnergyBoltSpell(Mobile),
            11 => new ExplosionSpell(Mobile),
            _  => new FlameStrikeSpell(Mobile)
        };
    }

    public virtual Spell GetRandomCurseSpell() =>
        UseNecromancy() ? GetRandomCurseSpellNecro() : GetRandomCurseSpellMage();

    public virtual Spell GetRandomCurseSpellNecro()
    {
        return Utility.Random(4) switch
        {
            0 => new BloodOathSpell(Mobile),
            1 => new CorpseSkinSpell(Mobile),
            2 => new EvilOmenSpell(Mobile),
            _ => new MindRotSpell(Mobile)
        };
    }

    public virtual Spell GetRandomCurseSpellMage()
    {
        if (Mobile.Skills.Magery.Value >= 40.0 && Utility.Random(4) == 0)
        {
            return new CurseSpell(Mobile);
        }

        return Utility.Random(3) switch
        {
            0 => new WeakenSpell(Mobile),
            1 => new ClumsySpell(Mobile),
            _ => new FeeblemindSpell(Mobile)
        };
    }

    public virtual Spell GetRandomManaDrainSpell()
    {
        if (Mobile.Skills.Magery.Value >= 80.0 && Utility.RandomBool())
        {
            return new ManaVampireSpell(Mobile);
        }

        return new ManaDrainSpell(Mobile);
    }

    public virtual Spell DoDispel(Mobile toDispel)
    {
        if (!SmartAI)
        {
            if (DispelChance > 0 && ScaleBySkill(DispelChance, SkillName.Magery) > Utility.RandomDouble())
            {
                return new DispelSpell(Mobile);
            }

            return null;
        }

        var spell = CheckCastHealingSpell();

        if (spell != null)
        {
            return spell;
        }

        var distance = (int)Mobile.GetDistanceToSqrt(toDispel);
        if (!Mobile.DisallowAllMoves && distance > 0 && Utility.Random(distance) == 0)
        {
            return new TeleportSpell(Mobile);
        }

        if (Utility.Random(3) == 0 && !Mobile.InRange(toDispel, 3) && !toDispel.Paralyzed && !toDispel.Frozen)
        {
            return new ParalyzeSpell(Mobile);
        }

        return new DispelSpell(Mobile);
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
                    (Mobile.Skills.SpiritSpeak.Value - c.Skills.MagicResist.Value) / 10 +
                    (c.Player ? 18 : 30);

                if (psDamage > c.Hits)
                {
                    return new PainSpikeSpell(Mobile);
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

                        spell = new PoisonSpell(Mobile);
                        break;
                    }
                case 2: // Bless ourselves
                    {
                        DebugSay("Blessing myself");

                        spell = new BlessSpell(Mobile);
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
                        if (c.Paralyzed || Mobile.Skills.Magery.Value <= 50.0)
                        {
                            goto default;
                        }

                        DebugSay("Attempting to paralyze");

                        spell = new ParalyzeSpell(Mobile);
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

                        spell = new InvisibilitySpell(Mobile);
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

                            spell = new PoisonSpell(Mobile);
                            break;
                        }
                    case 1: // Deal some damage
                        {
                            spell = GetRandomDamageSpell();
                            break;
                        }
                    default: // Set up a combo
                        {
                            if (Mobile.Mana is > 15 and < 40)
                            {
                                if (c.Paralyzed && !c.Poisoned && !Mobile.Meditating)
                                {
                                    DebugSay("I am going to meditate");

                                    Mobile.UseSkill(SkillName.Meditation);
                                }
                                else if (!c.Poisoned)
                                {
                                    spell = new ParalyzeSpell(Mobile);
                                }
                            }
                            else if (Mobile.Mana > 60)
                            {
                                if (Utility.RandomBool() && !c.Paralyzed && !c.Frozen && !c.Poisoned)
                                {
                                    _combo = 0;
                                    spell = new ParalyzeSpell(Mobile);
                                }
                                else
                                {
                                    _combo = 1;
                                    spell = new ExplosionSpell(Mobile);
                                }
                            }

                            break;
                        }
                }
            }
        }

        if (spell != null)
        {
            this.DebugSayFormatted($"Casting {spell.Name}");
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

        if (_combo == 0)
        {
            spell = new ExplosionSpell(Mobile);
            ++_combo; // Move to next spell
        }
        else if (_combo == 1)
        {
            spell = new WeakenSpell(Mobile);
            ++_combo; // Move to next spell
        }
        else if (_combo == 2)
        {
            if (!c.Poisoned)
            {
                spell = new PoisonSpell(Mobile);
            }
            else if (IsNecromancer)
            {
                spell = new StrangleSpell(Mobile);
            }

            ++_combo; // Move to next spell
        }

        if (_combo == 3 && spell == null)
        {
            switch (Utility.Random(IsNecromancer ? 4 : 3))
            {
                case 0:
                    {
                        if (c.Int < c.Dex)
                        {
                            spell = new FeeblemindSpell(Mobile);
                        }
                        else
                        {
                            spell = new ClumsySpell(Mobile);
                        }

                        ++_combo; // Move to next spell

                        break;
                    }
                case 1:
                    {
                        spell = new EnergyBoltSpell(Mobile);
                        _combo = -1; // Reset combo state
                        break;
                    }
                case 2:
                    {
                        spell = new FlameStrikeSpell(Mobile);
                        _combo = -1; // Reset combo state
                        break;
                    }
                default:
                    {
                        spell = new PainSpikeSpell(Mobile);
                        _combo = -1; // Reset combo state
                        break;
                    }
            }
        }
        else if (_combo == 4 && spell == null)
        {
            spell = new MindBlastSpell(Mobile);
            _combo = -1;
        }

        return spell;
    }

    private TimeSpan GetDelay(Spell spell)
    {
        if (SmartAI || spell is DispelSpell)
        {
            return TimeSpan.FromSeconds(Mobile.ActiveSpeed);
        }

        var del = ScaleBySkill(3.0, SkillName.Magery);
        var min = 6.0 - del * 0.75;
        var max = 6.0 - del * 1.25;

        return TimeSpan.FromSeconds(min + (max - min) * Utility.RandomDouble());
    }

    public override bool DoActionCombat()
    {
        var c = Mobile.Combatant;
        Mobile.Warmode = true;

        if (c?.Deleted != false || !c.Alive || c.IsDeadBondedPet || !Mobile.CanSee(c) ||
            !Mobile.CanBeHarmful(c, false) || c.Map != Mobile.Map)
        {
            // Our combatant is deleted, dead, hidden, or we cannot hurt them
            // Try to find another combatant

            if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
            {
                Mobile.Combatant = c = Mobile.FocusMob!;

                this.DebugSayFormatted($"Something happened to my combatant, so I am going to fight {c.Name}");

                Mobile.FocusMob = null;
            }
            else
            {
                DebugSay("Something happened to my combatant, and nothing is around. I am on guard.");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!Mobile.InLOS(c))
        {
            DebugSay("I can't see my target");

            if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
            {
                Mobile.Combatant = c = Mobile.FocusMob!;

                this.DebugSayFormatted($"I will switch to {c.Name}");

                Mobile.FocusMob = null;
            }
        }

        if (!Core.AOS && SmartAI && !Mobile.StunReady && Mobile.Skills.Wrestling.Value >= 80.0 &&
            Mobile.Skills.Anatomy.Value >= 80.0)
        {
            Fists.StunRequest(Mobile);
        }

        if (!Mobile.InRange(c, Mobile.RangePerception))
        {
            // They are somewhat far away, can we find something else?

            if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
            {
                Mobile.Combatant = Mobile.FocusMob;
                Mobile.FocusMob = null;
            }
            else if (!Mobile.InRange(c, Mobile.RangePerception * 3))
            {
                Mobile.Combatant = null;
            }

            c = Mobile.Combatant;

            if (c == null)
            {
                DebugSay("My combatant has fled, so I am on guard");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!Mobile.Controlled && !Mobile.Summoned && Mobile.CanFlee && Mobile.Hits < Mobile.HitsMax * 20 / 100)
        {
            // We are low on health, should we flee?
            // (10 + diff)% chance to flee
            var fleeChance = 10 + Math.Max(0, c.Hits - Mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                this.DebugSayFormatted($"I am going to flee from {c.Name}");

                Action = ActionType.Flee;
                return true;
            }
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, c))
        {
            DebugSay("I used my abilities!");
        }
        else if (Mobile.Spell == null && Core.TickCount - _nextCastTime >= 0)
        {
            if (Mobile.Controlled && c == Mobile)
            {
                DebugSay("I should not attack myself!");
                Mobile.Combatant = null;
                Action = ActionType.Guard;
                return true;
            }

            // We are ready to cast a spell
            Spell spell;
            var toDispel = FindDispelTarget(true);

            if (Mobile.Poisoned) // Top cast priority is cure
            {
                DebugSay("I am going to cure myself");

                spell = new CureSpell(Mobile);
            }
            else if (toDispel != null) // Something dispellable is attacking us
            {
                this.DebugSayFormatted($"I am going to dispel {toDispel}");

                spell = DoDispel(toDispel); // May return null if dumb AI and doesn't have enough skill
            }
            else
            {
                spell = null;
            }

            spell ??= SmartAI switch
            {
                // We are doing a spell combo
                true when _combo != -1 => DoCombo(c),
                // They have a heal spell out
                true when !c.Poisoned && c.Spell is HealSpell or GreaterHealSpell => new PoisonSpell(Mobile),
                _ => ChooseSpell(c)
            };

            // Now we have a spell picked
            var range = (spell as IRangedSpell)?.TargetRange ?? Mobile.RangePerception;

            // Move first before casting
            if (!SmartAI || toDispel == null)
            {
                RunTo(c);
            }
            else if (Mobile.InRange(toDispel, Math.Min(10, range)))
            {
                RunFrom(toDispel);
            }
            else if (!Mobile.InRange(toDispel, range))
            {
                RunTo(toDispel);
            }

            // After running, make sure we are still in range
            if (spell == null || Mobile.InRange(c, range))
            {
                spell?.Cast();

                // Even if we don't have a spell, delay the next potential cast
                _nextCastTime = Core.TickCount + (int)GetDelay(spell).TotalMilliseconds;
            }
        }
        else if (Mobile.Spell?.IsCasting != true)
        {
            RunTo(c);
        }

        if (Mobile.InRange(c, 1) || Mobile.Spell?.IsCasting == true || Core.TickCount - Mobile.LastMoveTime > 400)
        {
            Mobile.Direction = Mobile.GetDirectionTo(c);
        }

        _lastTarget = c;
        _lastTargetLoc = c.Location;

        return true;
    }

    public override bool DoActionGuard()
    {
        if (_lastTarget?.Hidden == true)
        {
            var map = Mobile.Map;

            if (map == null || !Mobile.InRange(_lastTargetLoc, Mobile.RangePerception))
            {
                _lastTarget = null;
            }
            else if (Mobile.Spell == null && Core.TickCount - _nextCastTime >= 0)
            {
                DebugSay("I am going to reveal my last target");

                _revealTarget = new LandTarget(_lastTargetLoc, map);
                Spell spell = new RevealSpell(Mobile);

                if (spell.Cast())
                {
                    _lastTarget = null; // only do it once
                }

                _nextCastTime = Core.TickCount + (int)GetDelay(spell).TotalMilliseconds;
            }
        }

        if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I am going to attack {Mobile.FocusMob.Name}");

            Mobile.Combatant = Mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            if (!Mobile.Controlled)
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
        if ((Mobile.Mana > 20 || Mobile.Mana == Mobile.ManaMax) && Mobile.Hits > Mobile.HitsMax / 2)
        {
            DebugSay("I am stronger now, my guard is up");

            Action = ActionType.Guard;
        }
        else if (AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I am scared of {Mobile.FocusMob.Name}");

            RunFrom(Mobile.FocusMob);
            Mobile.FocusMob = null;

            if (Mobile.Poisoned && Utility.Random(0, 5) == 0)
            {
                new CureSpell(Mobile).Cast();
            }
        }
        else
        {
            DebugSay("Area seems clear, but my guard is up");

            Action = ActionType.Guard;
            Mobile.Warmode = true;
        }

        return true;
    }

    public Mobile FindDispelTarget(bool activeOnly)
    {
        if (Mobile.Deleted || Mobile.Int < 95 || CanDispel(Mobile) || Mobile.AutoDispel)
        {
            return null;
        }

        if (activeOnly)
        {
            var aggressed = Mobile.Aggressed;
            var aggressors = Mobile.Aggressors;

            Mobile active = null;
            var activePrio = 0.0;

            var comb = Mobile.Combatant;

            if (comb?.Deleted == false && comb.Alive && !comb.IsDeadBondedPet &&
                Mobile.InRange(comb, Mobile.RangePerception) && CanDispel(comb))
            {
                active = comb;
                activePrio = Mobile.GetDistanceToSqrt(comb);

                if (activePrio <= 2)
                {
                    return active;
                }
            }

            for (var i = 0; i < aggressed.Count; ++i)
            {
                var info = aggressed[i];
                var m = info.Defender;

                if (m != comb && m.Combatant == Mobile && Mobile.InRange(m, Mobile.RangePerception) && CanDispel(m))
                {
                    var prio = Mobile.GetDistanceToSqrt(m);

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

                if (m != comb && m.Combatant == Mobile && Mobile.InRange(m, Mobile.RangePerception) && CanDispel(m))
                {
                    var prio = Mobile.GetDistanceToSqrt(m);

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

        var map = Mobile.Map;

        if (map != null)
        {
            Mobile active = null, inactive = null;
            double actPrio = 0.0, inactPrio = 0.0;

            var comb = Mobile.Combatant;

            if (comb?.Deleted == false && comb.Alive && !comb.IsDeadBondedPet && CanDispel(comb))
            {
                active = inactive = comb;
                actPrio = inactPrio = Mobile.GetDistanceToSqrt(comb);
            }

            foreach (var m in Mobile.GetMobilesInRange(Mobile.RangePerception))
            {
                if (m != Mobile && CanDispel(m))
                {
                    var prio = Mobile.GetDistanceToSqrt(m);

                    if (inactive == null || prio < inactPrio)
                    {
                        inactive = m;
                        inactPrio = prio;
                    }

                    if ((Mobile.Combatant == m || m.Combatant == Mobile) && (active == null || prio < actPrio))
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
        m is BaseCreature creature && creature.Summoned && creature.SummonMaster != Mobile &&
        Mobile.CanBeHarmful(creature, false) && !creature.IsAnimatedDead;

    private bool ProcessTarget()
    {
        var targ = Mobile.Target;

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
            toTarget = Mobile;
        }
        else if (isDispel)
        {
            toTarget = FindDispelTarget(false);

            if (!SmartAI && toTarget != null)
            {
                RunTo(toTarget);
            }
            else if (toTarget != null && Mobile.InRange(toTarget, 10))
            {
                RunFrom(toTarget);
            }
        }
        else if (SmartAI && (isParalyze || isTeleport))
        {
            toTarget = FindDispelTarget(true);

            if (toTarget == null)
            {
                toTarget = Mobile.Combatant;

                if (toTarget != null)
                {
                    RunTo(toTarget);
                }
            }
            else if (Mobile.InRange(toTarget, 10))
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
            toTarget = Mobile.Combatant;

            if (toTarget != null)
            {
                RunTo(toTarget);
            }
        }

        if ((targ.Flags & TargetFlags.Harmful) != 0 && toTarget != null)
        {
            if ((targ.Range == -1 || Mobile.InRange(toTarget, targ.Range)) && Mobile.CanSee(toTarget) &&
                Mobile.InLOS(toTarget))
            {
                targ.Invoke(Mobile, toTarget);
            }
            else if (isDispel)
            {
                targ.Cancel(Mobile, TargetCancelType.Canceled);
            }
        }
        else if ((targ.Flags & TargetFlags.Beneficial) != 0)
        {
            targ.Invoke(Mobile, Mobile);
        }
        else if (isReveal && _revealTarget != null)
        {
            targ.Invoke(Mobile, _revealTarget);
        }
        else
        {
            var map = Mobile.Map;

            if (map != null && isTeleport && toTarget != null)
            {
                var teleRange = targ.Range >= 0 ? targ.Range :
                    Core.ML ? 11 : 12;

                int px, py;

                if (teleportAway)
                {
                    var rx = Mobile.X - toTarget.X;
                    var ry = Mobile.Y - toTarget.Y;

                    var d = Mobile.GetDistanceToSqrt(toTarget);

                    px = toTarget.X + (int)(rx * (10 / d));
                    py = toTarget.Y + (int)(ry * (10 / d));
                }
                else
                {
                    px = toTarget.X;
                    py = toTarget.Y;
                }

                for (var i = 0; i < Offsets.Length; i += 2)
                {
                    int x = Offsets[i], y = Offsets[i + 1];

                    var p = new Point3D(px + x, py + y, 0);

                    var lt = new LandTarget(p, map);

                    if ((targ.Range == -1 || Mobile.InRange(p, targ.Range)) && Mobile.InLOS(lt) &&
                        map.CanSpawnMobile(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, map))
                    {
                        targ.Invoke(Mobile, lt);
                        return true;
                    }
                }

                for (var i = 0; i < 10; ++i)
                {
                    var randomPoint = new Point3D(
                        Mobile.X - teleRange + Utility.Random(teleRange * 2 + 1),
                        Mobile.Y - teleRange + Utility.Random(teleRange * 2 + 1),
                        0
                    );

                    var lt = new LandTarget(randomPoint, map);

                    if (Mobile.InLOS(lt) && map.CanSpawnMobile(lt.X, lt.Y, lt.Z) &&
                        !SpellHelper.CheckMulti(randomPoint, map))
                    {
                        targ.Invoke(Mobile, new LandTarget(randomPoint, map));
                        return true;
                    }
                }
            }

            targ.Cancel(Mobile, TargetCancelType.Canceled);
        }

        return true;
    }
}
