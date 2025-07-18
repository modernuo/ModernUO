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

    public virtual bool SmartAI => _mobile is BaseVendor or BaseEscortable or Changeling;

    public virtual bool IsNecromancer => Core.AOS && _mobile.Skills.Necromancy.Value > 50;

    public override bool Think()
    {
        if (_mobile.Deleted)
        {
            return false;
        }

        return ProcessTarget() || base.Think();
    }

    public virtual double ScaleBySkill(double v, SkillName skill) => v * _mobile.Skills[skill].Value / 100;

    public override bool DoActionWander()
    {
        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I am going to attack {_mobile.FocusMob.Name}");

            _mobile.Combatant = _mobile.FocusMob;
            Action = ActionType.Combat;
            _nextCastTime = Core.TickCount;
        }
        else if (SmartAI && _mobile.Mana < _mobile.ManaMax && !_mobile.Meditating)
        {
            DebugSay("I am going to meditate");

            _mobile.UseSkill(SkillName.Meditation);
        }
        else
        {
            DebugSay("I am wandering");

            _mobile.Warmode = false;

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
        if (_mobile.Poisoned)
        {
            return new CureSpell(_mobile);
        }

        // Summoned creatures never heal themselves.
        if (_mobile.Summoned || _mobile.Controlled && Core.TickCount - _nextHealTime < 0)
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
        else if (Utility.Random(0, 4 + (_mobile.Hits == 0 ? _mobile.HitsMax : _mobile.HitsMax / _mobile.Hits)) < 3)
        {
            return null;
        }

        Spell spell = null;

        if (_mobile.Hits < _mobile.HitsMax - 50)
        {
            if (UseNecromancy())
            {
                _mobile.UseSkill(SkillName.SpiritSpeak);
            }
            else
            {
                spell = new GreaterHealSpell(_mobile);
            }
        }
        else if (_mobile.Hits < _mobile.HitsMax - 10)
        {
            spell = new HealSpell(_mobile);
        }

        var delay = _mobile.Int >= 500 ? Utility.RandomMinMax(7, 10) : Math.Sqrt(600 - _mobile.Int);

        _nextHealTime = Core.TickCount + (int)TimeSpan.FromSeconds(delay).TotalMilliseconds;

        return spell;
    }

    public void RunTo(Mobile m)
    {
        if (!SmartAI)
        {
            if (!MoveTo(m, true, _mobile.RangeFight))
            {
                OnFailedMove();
            }

            return;
        }

        if (m.Paralyzed || m.Frozen)
        {
            if (_mobile.InRange(m, 1))
            {
                RunFrom(m);
            }
            else if (!_mobile.InRange(m, Math.Max(_mobile.RangeFight, 2)) && !MoveTo(m, true, 1))
            {
                OnFailedMove();
            }
        }
        else if (!_mobile.InRange(m, _mobile.RangeFight))
        {
            if (!MoveTo(m, true, 1))
            {
                OnFailedMove();
            }
        }
        else if (_mobile.InRange(m, _mobile.RangeFight - 1))
        {
            RunFrom(m);
        }
    }

    public void RunFrom(Mobile m)
    {
        Run((_mobile.GetDirectionTo(m) - 4) & Direction.Mask);
    }

    public void OnFailedMove()
    {
        if (!_mobile.DisallowAllMoves && (SmartAI
                ? Utility.Random(4) == 0
                : TeleportChance > 0 && ScaleBySkill(TeleportChance, SkillName.Magery) > Utility.RandomDouble()))
        {
            _mobile.Target?.Cancel(_mobile, TargetCancelType.Canceled);

            new TeleportSpell(_mobile).Cast();

            DebugSay("I am stuck, I'm going to try teleporting away");
        }
        else if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"My move is blocked, so I am going to attack {_mobile.FocusMob.Name}");

            _mobile.Combatant = _mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            DebugSay("I am stuck");
        }
    }

    public void Run(Direction d)
    {
        if (_mobile.Spell?.IsCasting == true || _mobile.Paralyzed || _mobile.Frozen ||
            _mobile.DisallowAllMoves)
        {
            return;
        }

        _mobile.Direction = d | Direction.Running;

        if (!DoMove(_mobile.Direction, true))
        {
            OnFailedMove();
        }
    }

    public virtual bool UseNecromancy()
    {
        var magery = _mobile.Skills.Magery.BaseFixedPoint;
        var necro = _mobile.Skills.Necromancy.BaseFixedPoint;
        return IsNecromancer && Utility.Random(magery + necro) >= magery;
    }

    public virtual Spell GetRandomDamageSpell() =>
        UseNecromancy() ? GetRandomDamageSpellNecro() : GetRandomDamageSpellMage();

    public virtual Spell GetRandomDamageSpellNecro()
    {
        var bound = _mobile.Skills.Necromancy.Value >= 100 ? 5 : 3;

        return Utility.Random(bound) switch
        {
            0 => new PainSpikeSpell(_mobile),
            1 => new PoisonStrikeSpell(_mobile),
            2 => new StrangleSpell(_mobile),
            3 => new WitherSpell(_mobile),
            _ => new VengefulSpiritSpell(_mobile)
        };
    }

    public virtual Spell GetRandomDamageSpellMage()
    {
        var maxCircle = Math.Clamp((int)((_mobile.Skills.Magery.Value + 20.0) / (100.0 / 7.0)), 1, 8);

        return Utility.Random(maxCircle * 2) switch
        {
            0  => new MagicArrowSpell(_mobile),
            1  => new MagicArrowSpell(_mobile),
            2  => new HarmSpell(_mobile),
            3  => new HarmSpell(_mobile),
            4  => new FireballSpell(_mobile),
            5  => new FireballSpell(_mobile),
            6  => new LightningSpell(_mobile),
            7  => new LightningSpell(_mobile),
            8  => new MindBlastSpell(_mobile),
            9  => new MindBlastSpell(_mobile),
            10 => new EnergyBoltSpell(_mobile),
            11 => new ExplosionSpell(_mobile),
            _  => new FlameStrikeSpell(_mobile)
        };
    }

    public virtual Spell GetRandomCurseSpell() =>
        UseNecromancy() ? GetRandomCurseSpellNecro() : GetRandomCurseSpellMage();

    public virtual Spell GetRandomCurseSpellNecro()
    {
        return Utility.Random(4) switch
        {
            0 => new BloodOathSpell(_mobile),
            1 => new CorpseSkinSpell(_mobile),
            2 => new EvilOmenSpell(_mobile),
            _ => new MindRotSpell(_mobile)
        };
    }

    public virtual Spell GetRandomCurseSpellMage()
    {
        if (_mobile.Skills.Magery.Value >= 40.0 && Utility.Random(4) == 0)
        {
            return new CurseSpell(_mobile);
        }

        return Utility.Random(3) switch
        {
            0 => new WeakenSpell(_mobile),
            1 => new ClumsySpell(_mobile),
            _ => new FeeblemindSpell(_mobile)
        };
    }

    public virtual Spell GetRandomManaDrainSpell()
    {
        if (_mobile.Skills.Magery.Value >= 80.0 && Utility.RandomBool())
        {
            return new ManaVampireSpell(_mobile);
        }

        return new ManaDrainSpell(_mobile);
    }

    public virtual Spell DoDispel(Mobile toDispel)
    {
        if (!SmartAI)
        {
            if (DispelChance > 0 && ScaleBySkill(DispelChance, SkillName.Magery) > Utility.RandomDouble())
            {
                return new DispelSpell(_mobile);
            }

            return null;
        }

        var spell = CheckCastHealingSpell();

        if (spell != null)
        {
            return spell;
        }

        var distance = (int)_mobile.GetDistanceToSqrt(toDispel);
        if (!_mobile.DisallowAllMoves && distance > 0 && Utility.Random(distance) == 0)
        {
            return new TeleportSpell(_mobile);
        }

        if (Utility.Random(3) == 0 && !_mobile.InRange(toDispel, 3) && !toDispel.Paralyzed && !toDispel.Frozen)
        {
            return new ParalyzeSpell(_mobile);
        }

        return new DispelSpell(_mobile);
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
                    (_mobile.Skills.SpiritSpeak.Value - c.Skills.MagicResist.Value) / 10 +
                    (c.Player ? 18 : 30);

                if (psDamage > c.Hits)
                {
                    return new PainSpikeSpell(_mobile);
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

                        spell = new PoisonSpell(_mobile);
                        break;
                    }
                case 2: // Bless ourselves
                    {
                        DebugSay("Blessing myself");

                        spell = new BlessSpell(_mobile);
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
                        if (c.Paralyzed || _mobile.Skills.Magery.Value <= 50.0)
                        {
                            goto default;
                        }

                        DebugSay("Attempting to paralyze");

                        spell = new ParalyzeSpell(_mobile);
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

                        spell = new InvisibilitySpell(_mobile);
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

                            spell = new PoisonSpell(_mobile);
                            break;
                        }
                    case 1: // Deal some damage
                        {
                            spell = GetRandomDamageSpell();
                            break;
                        }
                    default: // Set up a combo
                        {
                            if (_mobile.Mana is > 15 and < 40)
                            {
                                if (c.Paralyzed && !c.Poisoned && !_mobile.Meditating)
                                {
                                    DebugSay("I am going to meditate");

                                    _mobile.UseSkill(SkillName.Meditation);
                                }
                                else if (!c.Poisoned)
                                {
                                    spell = new ParalyzeSpell(_mobile);
                                }
                            }
                            else if (_mobile.Mana > 60)
                            {
                                if (Utility.RandomBool() && !c.Paralyzed && !c.Frozen && !c.Poisoned)
                                {
                                    _combo = 0;
                                    spell = new ParalyzeSpell(_mobile);
                                }
                                else
                                {
                                    _combo = 1;
                                    spell = new ExplosionSpell(_mobile);
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
            spell = new ExplosionSpell(_mobile);
            ++_combo; // Move to next spell
        }
        else if (_combo == 1)
        {
            spell = new WeakenSpell(_mobile);
            ++_combo; // Move to next spell
        }
        else if (_combo == 2)
        {
            if (!c.Poisoned)
            {
                spell = new PoisonSpell(_mobile);
            }
            else if (IsNecromancer)
            {
                spell = new StrangleSpell(_mobile);
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
                            spell = new FeeblemindSpell(_mobile);
                        }
                        else
                        {
                            spell = new ClumsySpell(_mobile);
                        }

                        ++_combo; // Move to next spell

                        break;
                    }
                case 1:
                    {
                        spell = new EnergyBoltSpell(_mobile);
                        _combo = -1; // Reset combo state
                        break;
                    }
                case 2:
                    {
                        spell = new FlameStrikeSpell(_mobile);
                        _combo = -1; // Reset combo state
                        break;
                    }
                default:
                    {
                        spell = new PainSpikeSpell(_mobile);
                        _combo = -1; // Reset combo state
                        break;
                    }
            }
        }
        else if (_combo == 4 && spell == null)
        {
            spell = new MindBlastSpell(_mobile);
            _combo = -1;
        }

        return spell;
    }

    private TimeSpan GetDelay(Spell spell)
    {
        if (SmartAI || spell is DispelSpell)
        {
            return TimeSpan.FromSeconds(_mobile.ActiveSpeed);
        }

        var del = ScaleBySkill(3.0, SkillName.Magery);
        var min = 6.0 - del * 0.75;
        var max = 6.0 - del * 1.25;

        return TimeSpan.FromSeconds(min + (max - min) * Utility.RandomDouble());
    }

    public override bool DoActionCombat()
    {
        var c = _mobile.Combatant;
        _mobile.Warmode = true;

        if (c?.Deleted != false || !c.Alive || c.IsDeadBondedPet || !_mobile.CanSee(c) ||
            !_mobile.CanBeHarmful(c, false) || c.Map != _mobile.Map)
        {
            // Our combatant is deleted, dead, hidden, or we cannot hurt them
            // Try to find another combatant

            if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
            {
                _mobile.Combatant = c = _mobile.FocusMob!;

                this.DebugSayFormatted($"Something happened to my combatant, so I am going to fight {c.Name}");

                _mobile.FocusMob = null;
            }
            else
            {
                DebugSay("Something happened to my combatant, and nothing is around. I am on guard.");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!_mobile.InLOS(c))
        {
            DebugSay("I can't see my target");

            if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
            {
                _mobile.Combatant = c = _mobile.FocusMob!;

                this.DebugSayFormatted($"I will switch to {c.Name}");

                _mobile.FocusMob = null;
            }
        }

        if (!Core.AOS && SmartAI && !_mobile.StunReady && _mobile.Skills.Wrestling.Value >= 80.0 &&
            _mobile.Skills.Anatomy.Value >= 80.0)
        {
            Fists.StunRequest(_mobile);
        }

        if (!_mobile.InRange(c, _mobile.RangePerception))
        {
            // They are somewhat far away, can we find something else?

            if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
            {
                _mobile.Combatant = _mobile.FocusMob;
                _mobile.FocusMob = null;
            }
            else if (!_mobile.InRange(c, _mobile.RangePerception * 3))
            {
                _mobile.Combatant = null;
            }

            c = _mobile.Combatant;

            if (c == null)
            {
                DebugSay("My combatant has fled, so I am on guard");

                Action = ActionType.Guard;
                return true;
            }
        }

        if (!_mobile.Controlled && !_mobile.Summoned && _mobile.CanFlee && _mobile.Hits < _mobile.HitsMax * 20 / 100)
        {
            // We are low on health, should we flee?
            // (10 + diff)% chance to flee
            var fleeChance = 10 + Math.Max(0, c.Hits - _mobile.Hits);

            if (Utility.Random(0, 100) > fleeChance)
            {
                this.DebugSayFormatted($"I am going to flee from {c.Name}");

                Action = ActionType.Flee;
                return true;
            }
        }

        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, c))
        {
            DebugSay("I used my abilities!");
        }
        else if (_mobile.Spell == null && Core.TickCount - _nextCastTime >= 0)
        {
            // We are ready to cast a spell
            Spell spell;
            var toDispel = FindDispelTarget(true);

            if (_mobile.Poisoned) // Top cast priority is cure
            {
                DebugSay("I am going to cure myself");

                spell = new CureSpell(_mobile);
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
                true when !c.Poisoned && c.Spell is HealSpell or GreaterHealSpell => new PoisonSpell(_mobile),
                _ => ChooseSpell(c)
            };

            // Now we have a spell picked
            var range = (spell as IRangedSpell)?.TargetRange ?? _mobile.RangePerception;

            // Move first before casting
            if (!SmartAI || toDispel == null)
            {
                RunTo(c);
            }
            else if (_mobile.InRange(toDispel, Math.Min(10, range)))
            {
                RunFrom(toDispel);
            }
            else if (!_mobile.InRange(toDispel, range))
            {
                RunTo(toDispel);
            }

            // After running, make sure we are still in range
            if (spell == null || _mobile.InRange(c, range))
            {
                spell?.Cast();

                // Even if we don't have a spell, delay the next potential cast
                _nextCastTime = Core.TickCount + (int)GetDelay(spell).TotalMilliseconds;
            }
        }
        else if (_mobile.Spell?.IsCasting != true)
        {
            RunTo(c);
        }

        if (_mobile.Spell != null || !_mobile.InRange(c, 1) || Core.TickCount - _mobile.LastMoveTime > 800)
        {
            _mobile.Direction = _mobile.GetDirectionTo(c);
        }

        _lastTarget = c;
        _lastTargetLoc = c.Location;

        return true;
    }

    public override bool DoActionGuard()
    {
        if (_lastTarget?.Hidden == true)
        {
            var map = _mobile.Map;

            if (map == null || !_mobile.InRange(_lastTargetLoc, _mobile.RangePerception))
            {
                _lastTarget = null;
            }
            else if (_mobile.Spell == null && Core.TickCount - _nextCastTime >= 0)
            {
                DebugSay("I am going to reveal my last target");

                _revealTarget = new LandTarget(_lastTargetLoc, map);
                Spell spell = new RevealSpell(_mobile);

                if (spell.Cast())
                {
                    _lastTarget = null; // only do it once
                }

                _nextCastTime = Core.TickCount + (int)GetDelay(spell).TotalMilliseconds;
            }
        }

        if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I am going to attack {_mobile.FocusMob.Name}");

            _mobile.Combatant = _mobile.FocusMob;
            Action = ActionType.Combat;
        }
        else
        {
            if (!_mobile.Controlled)
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
        // Mobile c = _mobile.Combatant;

        if ((_mobile.Mana > 20 || _mobile.Mana == _mobile.ManaMax) && _mobile.Hits > _mobile.HitsMax / 2)
        {
            DebugSay("I am stronger now, my guard is up");

            Action = ActionType.Guard;
        }
        else if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
        {
            this.DebugSayFormatted($"I am scared of {_mobile.FocusMob.Name}");

            RunFrom(_mobile.FocusMob);
            _mobile.FocusMob = null;

            if (_mobile.Poisoned && Utility.Random(0, 5) == 0)
            {
                new CureSpell(_mobile).Cast();
            }
        }
        else
        {
            DebugSay("Area seems clear, but my guard is up");

            Action = ActionType.Guard;
            _mobile.Warmode = true;
        }

        return true;
    }

    public Mobile FindDispelTarget(bool activeOnly)
    {
        if (_mobile.Deleted || _mobile.Int < 95 || CanDispel(_mobile) || _mobile.AutoDispel)
        {
            return null;
        }

        if (activeOnly)
        {
            var aggressed = _mobile.Aggressed;
            var aggressors = _mobile.Aggressors;

            Mobile active = null;
            var activePrio = 0.0;

            var comb = _mobile.Combatant;

            if (comb?.Deleted == false && comb.Alive && !comb.IsDeadBondedPet &&
                _mobile.InRange(comb, _mobile.RangePerception) && CanDispel(comb))
            {
                active = comb;
                activePrio = _mobile.GetDistanceToSqrt(comb);

                if (activePrio <= 2)
                {
                    return active;
                }
            }

            for (var i = 0; i < aggressed.Count; ++i)
            {
                var info = aggressed[i];
                var m = info.Defender;

                if (m != comb && m.Combatant == _mobile && _mobile.InRange(m, _mobile.RangePerception) && CanDispel(m))
                {
                    var prio = _mobile.GetDistanceToSqrt(m);

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

                if (m != comb && m.Combatant == _mobile && _mobile.InRange(m, _mobile.RangePerception) && CanDispel(m))
                {
                    var prio = _mobile.GetDistanceToSqrt(m);

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

        var map = _mobile.Map;

        if (map != null)
        {
            Mobile active = null, inactive = null;
            double actPrio = 0.0, inactPrio = 0.0;

            var comb = _mobile.Combatant;

            if (comb?.Deleted == false && comb.Alive && !comb.IsDeadBondedPet && CanDispel(comb))
            {
                active = inactive = comb;
                actPrio = inactPrio = _mobile.GetDistanceToSqrt(comb);
            }

            foreach (var m in _mobile.GetMobilesInRange(_mobile.RangePerception))
            {
                if (m != _mobile && CanDispel(m))
                {
                    var prio = _mobile.GetDistanceToSqrt(m);

                    if (inactive == null || prio < inactPrio)
                    {
                        inactive = m;
                        inactPrio = prio;
                    }

                    if ((_mobile.Combatant == m || m.Combatant == _mobile) && (active == null || prio < actPrio))
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
        m is BaseCreature creature && creature.Summoned && creature.SummonMaster != _mobile &&
        _mobile.CanBeHarmful(creature, false) && !creature.IsAnimatedDead;

    private bool ProcessTarget()
    {
        var targ = _mobile.Target;

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
            toTarget = _mobile;
        }
        else if (isDispel)
        {
            toTarget = FindDispelTarget(false);

            if (!SmartAI && toTarget != null)
            {
                RunTo(toTarget);
            }
            else if (toTarget != null && _mobile.InRange(toTarget, 10))
            {
                RunFrom(toTarget);
            }
        }
        else if (SmartAI && (isParalyze || isTeleport))
        {
            toTarget = FindDispelTarget(true);

            if (toTarget == null)
            {
                toTarget = _mobile.Combatant;

                if (toTarget != null)
                {
                    RunTo(toTarget);
                }
            }
            else if (_mobile.InRange(toTarget, 10))
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
            toTarget = _mobile.Combatant;

            if (toTarget != null)
            {
                RunTo(toTarget);
            }
        }

        if ((targ.Flags & TargetFlags.Harmful) != 0 && toTarget != null)
        {
            if ((targ.Range == -1 || _mobile.InRange(toTarget, targ.Range)) && _mobile.CanSee(toTarget) &&
                _mobile.InLOS(toTarget))
            {
                targ.Invoke(_mobile, toTarget);
            }
            else if (isDispel)
            {
                targ.Cancel(_mobile, TargetCancelType.Canceled);
            }
        }
        else if ((targ.Flags & TargetFlags.Beneficial) != 0)
        {
            targ.Invoke(_mobile, _mobile);
        }
        else if (isReveal && _revealTarget != null)
        {
            targ.Invoke(_mobile, _revealTarget);
        }
        else
        {
            var map = _mobile.Map;

            if (map != null && isTeleport && toTarget != null)
            {
                var teleRange = targ.Range >= 0 ? targ.Range :
                    Core.ML ? 11 : 12;

                int px, py;

                if (teleportAway)
                {
                    var rx = _mobile.X - toTarget.X;
                    var ry = _mobile.Y - toTarget.Y;

                    var d = _mobile.GetDistanceToSqrt(toTarget);

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

                    if ((targ.Range == -1 || _mobile.InRange(p, targ.Range)) && _mobile.InLOS(lt) &&
                        map.CanSpawnMobile(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, map))
                    {
                        targ.Invoke(_mobile, lt);
                        return true;
                    }
                }

                for (var i = 0; i < 10; ++i)
                {
                    var randomPoint = new Point3D(
                        _mobile.X - teleRange + Utility.Random(teleRange * 2 + 1),
                        _mobile.Y - teleRange + Utility.Random(teleRange * 2 + 1),
                        0
                    );

                    var lt = new LandTarget(randomPoint, map);

                    if (_mobile.InLOS(lt) && map.CanSpawnMobile(lt.X, lt.Y, lt.Z) &&
                        !SpellHelper.CheckMulti(randomPoint, map))
                    {
                        targ.Invoke(_mobile, new LandTarget(randomPoint, map));
                        return true;
                    }
                }
            }

            targ.Cancel(_mobile, TargetCancelType.Canceled);
        }

        return true;
    }
}
