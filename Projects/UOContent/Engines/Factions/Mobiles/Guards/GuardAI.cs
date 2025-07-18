using System;
using System.Collections.Generic;
using Server.Factions.AI;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Targeting;

namespace Server.Factions
{
    [Flags]
    public enum GuardAI
    {
        Bless = 0x01, // heal, cure, +stats
        Curse = 0x02, // poison, -stats
        Melee = 0x04, // weapons
        Magic = 0x08, // damage spells
        Smart = 0x10  // smart weapons/damage spells
    }

    public class ComboEntry
    {
        public ComboEntry(Type spell, int chance = 100) : this(spell, chance, TimeSpan.Zero)
        {
        }

        public ComboEntry(Type spell, int chance, TimeSpan hold)
        {
            Spell = spell;
            Chance = chance;
            Hold = hold;
        }

        public Type Spell { get; }

        public TimeSpan Hold { get; }

        public int Chance { get; }
    }

    public class SpellCombo
    {
        public static readonly SpellCombo Simple = new(
            50,
            new ComboEntry(typeof(ParalyzeSpell), 20),
            new ComboEntry(typeof(ExplosionSpell), 100, TimeSpan.FromSeconds(2.8)),
            new ComboEntry(typeof(PoisonSpell), 30),
            new ComboEntry(typeof(EnergyBoltSpell))
        );

        public static readonly SpellCombo Strong = new(
            90,
            new ComboEntry(typeof(ParalyzeSpell), 20),
            new ComboEntry(typeof(ExplosionSpell), 50, TimeSpan.FromSeconds(2.8)),
            new ComboEntry(typeof(PoisonSpell), 30),
            new ComboEntry(typeof(ExplosionSpell), 100, TimeSpan.FromSeconds(2.8)),
            new ComboEntry(typeof(EnergyBoltSpell)),
            new ComboEntry(typeof(PoisonSpell), 30),
            new ComboEntry(typeof(EnergyBoltSpell))
        );

        public SpellCombo(int mana, params ComboEntry[] entries)
        {
            Mana = mana;
            Entries = entries;
        }

        public int Mana { get; }

        public ComboEntry[] Entries { get; }

        public static Spell Process(Mobile mob, Mobile targ, ref SpellCombo combo, ref int index, ref DateTime releaseTime)
        {
            while (++index < combo.Entries.Length)
            {
                var entry = combo.Entries[index];

                if (entry.Spell == typeof(PoisonSpell) && targ.Poisoned)
                {
                    continue;
                }

                if (entry.Chance > Utility.Random(100))
                {
                    releaseTime = Core.Now + entry.Hold;
                    return entry.Spell.CreateInstance<Spell>(mob, null);
                }
            }

            combo = null;
            index = -1;
            return null;
        }
    }

    public class FactionGuardAI : BaseAI
    {
        private const int ManaReserve = 30;
        private readonly BaseFactionGuard m_Guard;

        private BandageContext _bandage;
        private DateTime m_BandageStart;

        private SpellCombo m_Combo;
        private int m_ComboIndex = -1;
        private DateTime m_ReleaseTarget;

        public FactionGuardAI(BaseFactionGuard guard) : base(guard) => m_Guard = guard;

        public bool IsDamaged => m_Guard.Hits < m_Guard.HitsMax;

        public bool IsPoisoned => m_Guard.Poisoned;

        public TimeSpan TimeUntilBandage
        {
            get
            {
                if (_bandage != null && !_bandage.Running)
                {
                    _bandage = null;
                }

                if (_bandage == null)
                {
                    return TimeSpan.MaxValue;
                }

                var ts = m_BandageStart + _bandage.Delay - Core.Now;

                if (ts < TimeSpan.FromSeconds(-1.0))
                {
                    _bandage = null;
                    return TimeSpan.MaxValue;
                }

                return Utility.Max(ts, TimeSpan.Zero);
            }
        }

        public bool IsAllowed(GuardAI flag) => (m_Guard.GuardAI & flag) == flag;

        public bool DequipWeapon()
        {
            var pack = m_Guard.Backpack;

            if (pack == null)
            {
                return false;
            }

            if (m_Guard.Weapon is Item weapon && weapon.Parent == m_Guard && weapon is not Fists)
            {
                pack.DropItem(weapon);
                return true;
            }

            return false;
        }

        public bool EquipWeapon()
        {
            Item weapon = m_Guard.Backpack?.FindItemByType<BaseWeapon>();

            return weapon != null && m_Guard.EquipItem(weapon);
        }

        public bool StartBandage()
        {
            _bandage = null;

            if (m_Guard.Backpack?.FindItemByType<Bandage>() != null)
            {
                _bandage = BandageContext.BeginHeal(m_Guard, m_Guard);
                m_BandageStart = Core.Now;
            }

            return _bandage != null;
        }

        public bool UseItemByType(Type type)
        {
            var pack = m_Guard.Backpack;

            var item = pack?.FindItemByType(type);

            if (item == null)
            {
                return false;
            }

            var requip = DequipWeapon();

            item.OnDoubleClick(m_Guard);

            if (requip)
            {
                EquipWeapon();
            }

            return true;
        }

        public static int GetStatMod(Mobile mob, StatType type) =>
            mob.GetStatMod($"[Magic] {type} Curse")?.Offset ?? 0;

        public Spell RandomOffenseSpell()
        {
            var maxCircle = Math.Max((int)((m_Guard.Skills.Magery.Value + 20.0) / (100.0 / 7.0)), 1);

            return Utility.Random(maxCircle * 2) switch
            {
                0  => new MagicArrowSpell(m_Guard),
                1  => new MagicArrowSpell(m_Guard),
                2  => new HarmSpell(m_Guard),
                3  => new HarmSpell(m_Guard),
                4  => new FireballSpell(m_Guard),
                5  => new FireballSpell(m_Guard),
                6  => new LightningSpell(m_Guard),
                7  => new LightningSpell(m_Guard),
                8  => new MindBlastSpell(m_Guard),
                9  => new ParalyzeSpell(m_Guard),
                10 => new EnergyBoltSpell(m_Guard),
                11 => new ExplosionSpell(m_Guard),
                _  => new FlameStrikeSpell(m_Guard)
            };
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

                if (comb?.Deleted == false && comb.Alive && !comb.IsDeadBondedPet && _mobile.InRange(comb, 12) &&
                    CanDispel(comb))
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

                    if (m != comb && m.Combatant == _mobile && _mobile.InRange(m, 12) && CanDispel(m))
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

                    if (m != comb && m.Combatant == _mobile && _mobile.InRange(m, 12) && CanDispel(m))
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

                foreach (var m in _mobile.GetMobilesInRange(12))
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
            m is BaseCreature creature && creature.Summoned && _mobile.CanBeHarmful(creature, false) &&
            !creature.IsAnimatedDead;

        public void RunTo(Mobile m)
        {
            /*if (m.Paralyzed || m.Frozen)
            {
              if (m_Mobile.InRange( m, 1 ))
                RunFrom( m );
              else if (!m_Mobile.InRange( m, m_Mobile.RangeFight > 2 ? m_Mobile.RangeFight : 2 ) && !MoveTo( m, true, 1 ))
                OnFailedMove();
            }
            else
            {*/
            if (!_mobile.InRange(m, _mobile.RangeFight))
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

            /*}*/
        }

        public void RunFrom(Mobile m)
        {
            Run((_mobile.GetDirectionTo(m) - 4) & Direction.Mask);
        }

        public void OnFailedMove()
        {
            /*if (!m_Mobile.DisallowAllMoves && 20 > Utility.Random( 100 ) && IsAllowed( GuardAI.Magic ))
            {
              if (m_Mobile.Target != null)
                m_Mobile.Target.Cancel( m_Mobile, TargetCancelType.Canceled );

              new TeleportSpell( m_Mobile, null ).Cast();

              DebugSay( "I am stuck, I'm going to try teleporting away" );
            }
            else*/
            if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
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

        public override bool Think()
        {
            if (_mobile.Deleted)
            {
                return false;
            }

            var combatant = m_Guard.Combatant;

            if (combatant?.Deleted != false || !combatant.Alive || combatant.IsDeadBondedPet ||
                !_mobile.CanSee(combatant) || !_mobile.CanBeHarmful(combatant, false) || combatant.Map != _mobile.Map)
            {
                // Our combatant is deleted, dead, hidden, or we cannot hurt them
                // Try to find another combatant

                if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
                {
                    _mobile.Combatant = combatant = _mobile.FocusMob;
                    _mobile.FocusMob = null;
                }
                else
                {
                    _mobile.Combatant = combatant = null;
                }
            }

            if (combatant != null && (!_mobile.InLOS(combatant) || !_mobile.InRange(combatant, 12)))
            {
                if (AcquireFocusMob(_mobile.RangePerception, _mobile.FightMode, false, false, true))
                {
                    _mobile.Combatant = combatant = _mobile.FocusMob;
                    _mobile.FocusMob = null;
                }
                else if (!_mobile.InRange(combatant, 36))
                {
                    _mobile.Combatant = combatant = null;
                }
            }

            var dispelTarget = FindDispelTarget(true);

            if (m_Guard.Target != null && m_ReleaseTarget == DateTime.MinValue)
            {
                m_ReleaseTarget = Core.Now + TimeSpan.FromSeconds(10.0);
            }

            if (m_Guard.Target != null && Core.Now > m_ReleaseTarget)
            {
                var targ = m_Guard.Target;

                var toHarm = dispelTarget ?? combatant;

                if ((targ.Flags & TargetFlags.Harmful) != 0 && toHarm != null)
                {
                    if (m_Guard.Map == toHarm.Map && (targ.Range < 0 || m_Guard.InRange(toHarm, targ.Range)) &&
                        m_Guard.CanSee(toHarm) && m_Guard.InLOS(toHarm))
                    {
                        targ.Invoke(m_Guard, toHarm);
                    }
                    else if ((targ as ISpellTarget<Mobile>)?.Spell is DispelSpell)
                    {
                        targ.Cancel(m_Guard, TargetCancelType.Canceled);
                    }
                }
                else if ((targ.Flags & TargetFlags.Beneficial) != 0)
                {
                    targ.Invoke(m_Guard, m_Guard);
                }
                else
                {
                    targ.Cancel(m_Guard, TargetCancelType.Canceled);
                }

                m_ReleaseTarget = DateTime.MinValue;
            }

            if (dispelTarget != null)
            {
                if (Action != ActionType.Combat)
                {
                    Action = ActionType.Combat;
                }

                m_Guard.Warmode = true;

                RunFrom(dispelTarget);
            }
            else if (combatant != null)
            {
                if (Action != ActionType.Combat)
                {
                    Action = ActionType.Combat;
                }

                m_Guard.Warmode = true;

                RunTo(combatant);
            }
            else if (m_Guard.Orders.Movement != MovementType.Stand)
            {
                Mobile toFollow = null;

                if (m_Guard.Town != null && m_Guard.Orders.Movement == MovementType.Follow)
                {
                    toFollow = m_Guard.Orders.Follow ?? m_Guard.Town.Sheriff;
                }

                if (toFollow != null && toFollow.Map == m_Guard.Map &&
                    toFollow.InRange(m_Guard, m_Guard.RangePerception * 3) &&
                    Town.FromRegion(toFollow.Region) == m_Guard.Town)
                {
                    if (Action != ActionType.Combat)
                    {
                        Action = ActionType.Combat;
                    }

                    _mobile.SetCurrentSpeedToActive();
                    m_Guard.Warmode = true;

                    RunTo(toFollow);
                }
                else
                {
                    if (Action != ActionType.Wander)
                    {
                        Action = ActionType.Wander;
                    }

                    _mobile.SetCurrentSpeedToPassive();
                    m_Guard.Warmode = false;

                    WalkRandomInHome(2, 2, 1);
                }
            }
            else
            {
                if (Action != ActionType.Wander)
                {
                    Action = ActionType.Wander;
                }

                m_Guard.Warmode = false;
            }

            if ((IsDamaged || IsPoisoned) && m_Guard.Skills.Healing.Base > 20.0)
            {
                var ts = TimeUntilBandage;

                if (ts == TimeSpan.MaxValue)
                {
                    StartBandage();
                }
            }

            var spell = _mobile.Spell as Spell;

            if (spell == null && Core.TickCount - _mobile.NextSpellTime >= 0)
            {
                var toRelease = DateTime.MinValue;

                if (IsPoisoned)
                {
                    var p = m_Guard.Poison;

                    var ts = TimeUntilBandage;

                    if (p != Poison.Lesser || ts == TimeSpan.MaxValue || TimeUntilBandage < TimeSpan.FromSeconds(1.5) ||
                        m_Guard.HitsMax - m_Guard.Hits > Utility.Random(250))
                    {
                        if (IsAllowed(GuardAI.Bless))
                        {
                            spell = new CureSpell(m_Guard);
                        }
                        else
                        {
                            UseItemByType(typeof(BaseCurePotion));
                        }
                    }
                }
                else if (IsDamaged && m_Guard.HitsMax - m_Guard.Hits > Utility.Random(200))
                {
                    if (IsAllowed(GuardAI.Magic) && m_Guard.Hits * 100 / Math.Max(m_Guard.HitsMax, 1) < 10 &&
                        m_Guard.Home != Point3D.Zero && !Utility.InRange(m_Guard.Location, m_Guard.Home, 15) &&
                        m_Guard.Mana >= 11)
                    {
                        spell = new RecallSpell(
                            m_Guard,
                            new RunebookEntry(null, m_Guard.Home, m_Guard.Map, "Guard's Home")
                        );
                    }
                    else if (IsAllowed(GuardAI.Bless))
                    {
                        if (m_Guard.Mana >= 11 && m_Guard.Hits + 30 < m_Guard.HitsMax)
                        {
                            spell = new GreaterHealSpell(m_Guard);
                        }
                        else if (m_Guard.Hits + 10 < m_Guard.HitsMax &&
                                 (m_Guard.Mana < 11 || m_Guard.NextCombatTime - Core.TickCount > 2000))
                        {
                            spell = new HealSpell(m_Guard);
                        }
                    }
                    else if (m_Guard.CanBeginAction<BaseHealPotion>())
                    {
                        UseItemByType(typeof(BaseHealPotion));
                    }
                }
                else if (dispelTarget != null &&
                         (IsAllowed(GuardAI.Magic) || IsAllowed(GuardAI.Bless) || IsAllowed(GuardAI.Curse)))
                {
                    if (!dispelTarget.Paralyzed && m_Guard.Mana > ManaReserve + 20 && Utility.Random(100) < 40)
                    {
                        spell = new ParalyzeSpell(m_Guard);
                    }
                    else
                    {
                        spell = new DispelSpell(m_Guard);
                    }
                }

                if (combatant != null)
                {
                    if (m_Combo != null)
                    {
                        if (spell == null)
                        {
                            spell = SpellCombo.Process(m_Guard, combatant, ref m_Combo, ref m_ComboIndex, ref toRelease);
                        }
                        else
                        {
                            m_Combo = null;
                            m_ComboIndex = -1;
                        }
                    }
                    else if (Utility.Random(100) < 20 && IsAllowed(GuardAI.Magic))
                    {
                        if (Utility.Random(100) < 80)
                        {
                            m_Combo = IsAllowed(GuardAI.Smart) ? SpellCombo.Simple : SpellCombo.Strong;
                            m_ComboIndex = -1;

                            if (m_Guard.Mana >= ManaReserve + m_Combo.Mana)
                            {
                                spell = SpellCombo.Process(m_Guard, combatant, ref m_Combo, ref m_ComboIndex, ref toRelease);
                            }
                            else
                            {
                                m_Combo = null;

                                if (m_Guard.Mana >= ManaReserve + 40)
                                {
                                    spell = RandomOffenseSpell();
                                }
                            }
                        }
                        else if (m_Guard.Mana >= ManaReserve + 40)
                        {
                            spell = RandomOffenseSpell();
                        }
                    }

                    if (spell == null && Utility.Random(100) < 2 && m_Guard.Mana >= ManaReserve + 10)
                    {
                        var strMod = GetStatMod(m_Guard, StatType.Str);
                        var dexMod = GetStatMod(m_Guard, StatType.Dex);
                        var intMod = GetStatMod(m_Guard, StatType.Int);

                        var types = new List<Type>();

                        if (strMod <= 0)
                        {
                            types.Add(typeof(StrengthSpell));
                        }

                        if (dexMod <= 0 && IsAllowed(GuardAI.Melee))
                        {
                            types.Add(typeof(AgilitySpell));
                        }

                        if (intMod <= 0 && IsAllowed(GuardAI.Magic))
                        {
                            types.Add(typeof(CunningSpell));
                        }

                        if (IsAllowed(GuardAI.Bless))
                        {
                            if (types.Count > 1)
                            {
                                spell = new BlessSpell(m_Guard);
                            }
                            else if (types.Count == 1)
                            {
                                spell = types[0].CreateInstance<Spell>(m_Guard, null);
                            }
                        }
                        else if (types.Count > 0)
                        {
                            if (types[0] == typeof(StrengthSpell))
                            {
                                UseItemByType(typeof(BaseStrengthPotion));
                            }
                            else if (types[0] == typeof(AgilitySpell))
                            {
                                UseItemByType(typeof(BaseAgilityPotion));
                            }
                        }
                    }

                    if (spell == null && Utility.Random(100) < 2 && m_Guard.Mana >= ManaReserve + 10 &&
                        IsAllowed(GuardAI.Curse))
                    {
                        if (!combatant.Poisoned && Utility.Random(100) < 40)
                        {
                            spell = new PoisonSpell(m_Guard);
                        }
                        else
                        {
                            var strMod = GetStatMod(combatant, StatType.Str);
                            var dexMod = GetStatMod(combatant, StatType.Dex);
                            var intMod = GetStatMod(combatant, StatType.Int);

                            var types = new List<Type>();

                            if (strMod >= 0)
                            {
                                types.Add(typeof(WeakenSpell));
                            }

                            if (dexMod >= 0 && IsAllowed(GuardAI.Melee))
                            {
                                types.Add(typeof(ClumsySpell));
                            }

                            if (intMod >= 0 && IsAllowed(GuardAI.Magic))
                            {
                                types.Add(typeof(FeeblemindSpell));
                            }

                            if (types.Count > 1)
                            {
                                spell = new CurseSpell(m_Guard);
                            }
                            else if (types.Count == 1)
                            {
                                spell = types[0].CreateInstance<Spell>(m_Guard, null);
                            }
                        }
                    }
                }

                if (spell != null && m_Guard.HitsMax - m_Guard.Hits + 10 > Utility.Random(100))
                {
                    Type type = spell switch
                    {
                        GreaterHealSpell _ => typeof(BaseHealPotion),
                        CureSpell _        => typeof(BaseCurePotion),
                        StrengthSpell _    => typeof(BaseStrengthPotion),
                        AgilitySpell _     => typeof(BaseAgilityPotion),
                        _                  => null
                    };

                    if (type == typeof(BaseHealPotion) && !m_Guard.CanBeginAction(type))
                    {
                        type = null;
                    }

                    if (type != null && m_Guard.Target == null && UseItemByType(type))
                    {
                        if (spell is not GreaterHealSpell)
                        {
                            spell = null;
                        }
                        else if (m_Guard.Hits + 30 > m_Guard.HitsMax && m_Guard.Hits + 10 < m_Guard.HitsMax)
                        {
                            spell = new HealSpell(m_Guard);
                        }
                    }
                }
                else if (spell == null && m_Guard.Stam < m_Guard.StamMax / 3 && IsAllowed(GuardAI.Melee))
                {
                    UseItemByType(typeof(BaseRefreshPotion));
                }

                if (spell?.Cast() != true)
                {
                    EquipWeapon();
                }
            }
            else if (spell?.State == SpellState.Sequencing)
            {
                EquipWeapon();
            }

            return true;
        }
    }
}
