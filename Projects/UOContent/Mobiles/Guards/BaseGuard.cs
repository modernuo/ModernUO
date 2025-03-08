using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public abstract partial class BaseGuard : Mobile
{
    public static bool GuardsInstantKill { get; private set; }
    public static void Configure()
    {
        GuardsInstantKill = ServerConfiguration.GetSetting("guards.instantKill", true);
    }

    public static void Spawn(Mobile caller, Mobile target, int amount = 1, bool onlyAdditional = false)
    {
        if (target?.Deleted != false)
        {
            return;
        }

        foreach (var m in target.GetMobilesInRange(15))
        {
            if (m is BaseGuard g)
            {
                if (g.Focus == null) // idling
                {
                    g.Focus = target;

                    --amount;
                }
                else if (g.Focus == target && !onlyAdditional)
                {
                    --amount;
                }
            }
        }

        while (amount-- > 0)
        {
            caller.Region.MakeGuard(target);
        }
    }

    public static void TeleportTo(Mobile source, Point3D to)
    {
        Effects.SendLocationParticles(
            EffectItem.Create(source.Location, source.Map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            2023
        );

        source.Location = to;

        Effects.SendLocationParticles(
            EffectItem.Create(to, source.Map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            5023
        );

        source.PlaySound(0x1FE);
    }


    private GuardIdleTimer _idleTimer;
    private GuardAttackTimer _attackTimer;

    public BaseGuard(Mobile target)
    {
        Title = "the guard";

        if (target != null)
        {
            Location = target.Location;
            Map = target.Map;

            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                5023
            );

            Focus = target;
        }
    }

    protected GuardAttackTimer AttackTimer
    {
        get => _attackTimer;
        set
        {
            _attackTimer?.Stop();
            _attackTimer = value;
            _attackTimer?.Start();
        }
    }

    protected GuardIdleTimer IdleTimer
    {
        get => _idleTimer;
        set
        {
            _idleTimer?.Stop();
            _idleTimer = value;
            _idleTimer?.Start();
        }
    }

    public abstract Mobile Focus { get; set; }

    public override void OnAfterDelete()
    {
        AttackTimer = null;
        IdleTimer = null;
        base.OnAfterDelete();
    }

    public override bool OnBeforeDeath()
    {
        Effects.SendLocationParticles(
            EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            2023
        );

        PlaySound(0x1FE);
        Delete();
        return false;
    }

    public abstract void NonLethalAttack(Mobile target);

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Focus != null)
        {
            AttackTimer = new GuardAttackTimer(this);
        }
        else
        {
            IdleTimer = new GuardIdleTimer(this);
        }
    }
}

public class GuardAvengeTimer : Timer
{
    private readonly Mobile _focus;

    public GuardAvengeTimer(Mobile focus) : base(TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(1.0), 3) =>
        _focus = focus;

    protected override void OnTick() => BaseGuard.Spawn(_focus, _focus, 1, true);
}

public class GuardIdleTimer : Timer
{
    private readonly BaseGuard _owner;
    private int m_Stage;

    public GuardIdleTimer(BaseGuard owner) : base(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.5)) =>
        _owner = owner;

    protected override void OnTick()
    {
        if (_owner.Deleted)
        {
            Stop();
            return;
        }

        if (m_Stage++ % 4 == 0 || !_owner.Move(_owner.Direction))
        {
            _owner.Direction = (Direction)Utility.Random(8);
        }

        if (m_Stage > 16)
        {
            Effects.SendLocationParticles(
                EffectItem.Create(_owner.Location, _owner.Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                2023
            );
            _owner.PlaySound(0x1FE);

            if (_owner.Spawner == null)
            {
                _owner.Delete();
            }
            else
            {
                BaseGuard.TeleportTo(_owner, _owner.Spawner.HomeLocation);
            }
        }
    }
}

public class GuardAttackTimer : Timer
{
    private readonly BaseGuard _owner;

    public GuardAttackTimer(BaseGuard owner) : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.1)) =>
        _owner = owner;

    public void DoOnTick()
    {
        OnTick();
    }

    protected override void OnTick()
    {
        if (_owner.Deleted)
        {
            Stop();
            return;
        }

        _owner.Criminal = false;
        _owner.Kills = 0;
        _owner.Stam = _owner.StamMax;

        var target = _owner.Focus;

        if (target != null && (target.Deleted || !target.Alive || !_owner.CanBeHarmful(target)))
        {
            _owner.Focus = null;
            Stop();
            return;
        }

        if (_owner.Weapon is Fists)
        {
            _owner.Kill();
            Stop();
            return;
        }

        if (target != null && _owner.Combatant != target)
        {
            _owner.Combatant = target;
        }

        if (target == null)
        {
            Stop();
        }
        else if (BaseGuard.GuardsInstantKill)
        {
            BaseGuard.TeleportTo(_owner, target.Location);
            target.BoltEffect(0);

            if (target is BaseCreature creature)
            {
                creature.NoKillAwards = true;
            }

            target.Damage(target.HitsMax, _owner);
            target.Kill(); // just in case, maybe Damage is overridden on some shard

            if (target.Corpse != null && !target.Player)
            {
                target.Corpse.Delete();
            }

            _owner.Focus = null;
            Stop();
        }
        else
        {
            _owner.NonLethalAttack(target);
        }
    }
}
