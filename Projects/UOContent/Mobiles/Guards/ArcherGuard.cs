using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class ArcherGuard : BaseGuard
{
    private Timer _attackTimer;
    private Timer _idleTimer;

    [Constructible]
    public ArcherGuard(Mobile target = null) : base(target)
    {
        InitStats(100, 125, 25);
        Title = "the guard";

        SpeechHue = Utility.RandomDyedHue();

        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        new Horse().Rider = this;

        AddItem(new StuddedChest());
        AddItem(new StuddedArms());
        AddItem(new StuddedGloves());
        AddItem(new StuddedGorget());
        AddItem(new StuddedLegs());
        AddItem(new Boots());
        AddItem(new SkullCap());

        var bow = new Bow();

        bow.Movable = false;
        bow.Crafter = Name;
        bow.Quality = WeaponQuality.Exceptional;

        AddItem(bow);

        Container pack = new Backpack();

        pack.Movable = false;

        var arrows = new Arrow(250);

        arrows.LootType = LootType.Newbied;

        pack.DropItem(arrows);
        pack.DropItem(new Gold(10, 25));

        AddItem(pack);

        Skills.Anatomy.Base = 120.0;
        Skills.Tactics.Base = 120.0;
        Skills.Archery.Base = 120.0;
        Skills.MagicResist.Base = 120.0;
        Skills.DetectHidden.Base = 100.0;

        NextCombatTime = Core.TickCount + 500;
        Focus = target;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public override Mobile Focus
    {
        get => _focus;
        set
        {
            if (Deleted)
            {
                return;
            }

            var oldFocus = _focus;

            if (oldFocus != value)
            {
                _focus = value;

                if (value != null)
                {
                    AggressiveAction(value);
                }

                Combatant = value;

                if (oldFocus?.Alive == false)
                {
                    Say("Thou hast suffered thy punishment, scoundrel.");
                }

                if (value != null)
                {
                    Say(500131); // Thou wilt regret thine actions, swine!
                }

                if (_attackTimer != null)
                {
                    _attackTimer.Stop();
                    _attackTimer = null;
                }

                if (_idleTimer != null)
                {
                    _idleTimer.Stop();
                    _idleTimer = null;
                }

                if (_focus != null)
                {
                    _attackTimer = new AttackTimer(this);
                    _attackTimer.Start();
                    ((AttackTimer)_attackTimer).DoOnTick();
                }
                else
                {
                    _idleTimer = new IdleTimer(this);
                    _idleTimer.Start();
                }
            }
            else if (_focus == null && _idleTimer == null)
            {
                _idleTimer = new IdleTimer(this);
                _idleTimer.Start();
            }

            this.MarkDirty();
        }
    }

    public override bool OnBeforeDeath()
    {
        if (_focus?.Alive == true)
        {
            new AvengeTimer(_focus).Start(); // If a guard dies, three more guards will spawn
        }

        return base.OnBeforeDeath();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_focus != null)
        {
            _attackTimer = new AttackTimer(this);
            _attackTimer.Start();
        }
        else
        {
            _idleTimer = new IdleTimer(this);
            _idleTimer.Start();
        }
    }

    public override void OnAfterDelete()
    {
        if (_attackTimer != null)
        {
            _attackTimer.Stop();
            _attackTimer = null;
        }

        if (_idleTimer != null)
        {
            _idleTimer.Stop();
            _idleTimer = null;
        }

        base.OnAfterDelete();
    }

    private class AvengeTimer : Timer
    {
        private readonly Mobile m_Focus;

        // After 2.5 seconds, one guard will spawn every 1.0 second, three times
        public AvengeTimer(Mobile focus) : base(TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(1.0), 3) =>
            m_Focus = focus;

        protected override void OnTick()
        {
            Spawn(m_Focus, m_Focus, 1, true);
        }
    }

    private class AttackTimer : Timer
    {
        private readonly ArcherGuard m_Owner;
        // private bool m_Shooting;

        public AttackTimer(ArcherGuard owner) : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.1)) =>
            m_Owner = owner;

        public void DoOnTick()
        {
            OnTick();
        }

        protected override void OnTick()
        {
            if (m_Owner.Deleted)
            {
                Stop();
                return;
            }

            m_Owner.Criminal = false;
            m_Owner.Kills = 0;
            m_Owner.Stam = m_Owner.StamMax;

            var target = m_Owner.Focus;

            if (target != null && (target.Deleted || !target.Alive || !m_Owner.CanBeHarmful(target)))
            {
                m_Owner.Focus = null;
                Stop();
                return;
            }

            if (m_Owner.Weapon is Fists)
            {
                m_Owner.Kill();
                Stop();
                return;
            }

            if (target != null && m_Owner.Combatant != target)
            {
                m_Owner.Combatant = target;
            }

            if (target == null)
            {
                Stop();
            }
            else
            {
                // <instakill>
                TeleportTo(target);
                target.BoltEffect(0);

                if (target is BaseCreature creature)
                {
                    creature.NoKillAwards = true;
                }

                target.Damage(target.HitsMax, m_Owner);
                target.Kill(); // just in case, maybe Damage is overridden on some shard

                if (target.Corpse != null && !target.Player)
                {
                    target.Corpse.Delete();
                }

                m_Owner.Focus = null;
                Stop();
            } // </instakill>

            /*else if (!m_Owner.InRange( target, 20 ))
            {
              m_Shooting = false;
              m_Owner.Focus = null;
            }
            else if (!m_Owner.InLOS( target ))
            {
              m_Shooting = false;
              TeleportTo( target );
            }
            else if (!m_Owner.CanSee( target ))
            {
              m_Shooting = false;

              if (!m_Owner.InRange( target, 2 ))
              {
                if (!m_Owner.Move( m_Owner.GetDirectionTo( target ) | Direction.Running ) && OutOfMaxDistance( target ))
                  TeleportTo( target );
              }
              else
              {
                if (!m_Owner.UseSkill( SkillName.DetectHidden ) && Utility.Random( 50 ) == 0)
                  m_Owner.Say( "Reveal!" );
              }
            }
            else
            {
              if (m_Shooting && (TimeToSpare() || OutOfMaxDistance( target )))
                m_Shooting = false;
              else if (!m_Shooting && InMinDistance( target ))
                m_Shooting = true;

              if (!m_Shooting)
              {
                if (m_Owner.InRange( target, 1 ))
                {
                  if (!m_Owner.Move( (Direction)(m_Owner.GetDirectionTo( target ) - 4) | Direction.Running ) && OutOfMaxDistance( target ))
                    TeleportTo( target ); // Too close, move away
                }
                else if (!m_Owner.InRange( target, 2 ))
                {
                  if (!m_Owner.Move( m_Owner.GetDirectionTo( target ) | Direction.Running ) && OutOfMaxDistance( target ))
                    TeleportTo( target );
                }
              }
            }*/
        }

        private bool TimeToSpare() => m_Owner.NextCombatTime - Core.TickCount > 1000;

        private bool OutOfMaxDistance(IPoint2D target) => !m_Owner.InRange(target, m_Owner.Weapon.MaxRange);

        private bool InMinDistance(IPoint2D target) => m_Owner.InRange(target, 4);

        private void TeleportTo(IEntity target)
        {
            var from = m_Owner.Location;
            var to = target.Location;

            m_Owner.Location = to;

            Effects.SendLocationParticles(
                EffectItem.Create(from, m_Owner.Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                2023
            );
            Effects.SendLocationParticles(
                EffectItem.Create(to, m_Owner.Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                5023
            );

            m_Owner.PlaySound(0x1FE);
        }
    }

    private class IdleTimer : Timer
    {
        private readonly ArcherGuard m_Owner;
        private int m_Stage;

        public IdleTimer(ArcherGuard owner) : base(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.5)) =>
            m_Owner = owner;

        protected override void OnTick()
        {
            if (m_Owner.Deleted)
            {
                Stop();
                return;
            }

            if (m_Stage++ % 4 == 0 || !m_Owner.Move(m_Owner.Direction))
            {
                m_Owner.Direction = (Direction)Utility.Random(8);
            }

            if (m_Stage > 16)
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(m_Owner.Location, m_Owner.Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    2023
                );
                m_Owner.PlaySound(0x1FE);

                m_Owner.Delete();
            }
        }
    }
}
