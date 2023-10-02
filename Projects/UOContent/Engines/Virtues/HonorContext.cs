using System;
using Server.Mobiles;

namespace Server.Engines.Virtues;

public class HonorContext
{
    private readonly Point3D _initialLocation;
    private readonly Map _initialMap;

    private readonly InternalTimer _timer;
    private FirstHit _firstHit;
    private double _honorDamage;
    private bool _poisoned;
    private int _totalDamage;

    public HonorContext(PlayerMobile source, Mobile target)
    {
        Source = source;
        Target = target;

        _firstHit = FirstHit.NotDelivered;
        _poisoned = false;

        _initialLocation = source.Location;
        _initialMap = source.Map;

        source.SentHonorContext = this;
        ((IHonorTarget)target).ReceivedHonorContext = this;

        _timer = new InternalTimer(this);
        _timer.Start();
        source._honorTime = Core.Now + TimeSpan.FromMinutes(40);

        Timer.DelayCall(
            TimeSpan.FromMinutes(40),
            (m, c) =>
            {
                if (m._honorTime < Core.Now && m.SentHonorContext != null)
                {
                    c.Cancel();
                }
            },
            source,
            this
        );
    }

    public PlayerMobile Source { get; }

    public Mobile Target { get; }

    public int PerfectionDamageBonus { get; private set; }

    public int PerfectionLuckBonus => PerfectionDamageBonus * PerfectionDamageBonus / 10;

    public void OnSourceDamaged(Mobile from, int amount)
    {
        if (from != Target)
        {
            return;
        }

        if (_firstHit == FirstHit.NotDelivered)
        {
            _firstHit = FirstHit.Granted;
        }
    }

    public void OnTargetPoisoned()
    {
        _poisoned = true; // Set this flag for OnTargetDamaged which will be called next
    }

    public void OnTargetDamaged(Mobile from, int amount)
    {
        if (_firstHit == FirstHit.NotDelivered)
        {
            _firstHit = FirstHit.Delivered;
        }

        if (_poisoned)
        {
            _honorDamage += amount * 0.8;
            _poisoned = false; // Reset the flag

            return;
        }

        _totalDamage += amount;

        if (from == Source)
        {
            if (Target.CanSee(Source) && Target.InLOS(Source) &&
                (Source.InRange(Target, 1) || Source.Location == _initialLocation && Source.Map == _initialMap))
            {
                _honorDamage += amount;
            }
            else
            {
                _honorDamage += amount * 0.8;
            }
        }
        else if (from is BaseCreature creature && creature.GetMaster() == Source)
        {
            _honorDamage += amount * 0.8;
        }
    }

    public void OnTargetHit(Mobile from)
    {
        if (from != Source || PerfectionDamageBonus == 100)
        {
            return;
        }

        var bushido = (int)from.Skills.Bushido.Value;
        if (bushido < 50)
        {
            return;
        }

        PerfectionDamageBonus += bushido / 10;

        if (PerfectionDamageBonus >= 100)
        {
            PerfectionDamageBonus = 100;
            Source.SendLocalizedMessage(1063254); // You have Achieved Perfection in inflicting damage to this opponent!
        }
        else
        {
            Source.SendLocalizedMessage(1063255); // You gain in Perfection as you precisely strike your opponent.
        }
    }

    public void OnTargetMissed(Mobile from)
    {
        if (from != Source || PerfectionDamageBonus == 0)
        {
            return;
        }

        PerfectionDamageBonus -= 25;

        if (PerfectionDamageBonus <= 0)
        {
            PerfectionDamageBonus = 0;
            Source.SendLocalizedMessage(1063256); // You have lost all Perfection in fighting this opponent.
        }
        else
        {
            Source.SendLocalizedMessage(1063257); // You have lost some Perfection in fighting this opponent.
        }
    }

    public void OnSourceBeneficialAction(Mobile to)
    {
        if (to != Target)
        {
            return;
        }

        if (PerfectionDamageBonus >= 0)
        {
            PerfectionDamageBonus = 0;
            Source.SendLocalizedMessage(1063256); // You have lost all Perfection in fighting this opponent.
        }
    }

    public void OnSourceKilled()
    {
    }

    public void OnTargetKilled()
    {
        Cancel();

        var targetFame = Target.Fame;

        if (PerfectionDamageBonus > 0)
        {
            var restore = Math.Min(PerfectionDamageBonus * (targetFame + 5000) / 25000, 10);

            Source.Hits += restore;
            Source.Stam += restore;
            Source.Mana += restore;
        }

        if (VirtueSystem.GetVirtues(Source).Honor > targetFame)
        {
            return;
        }

        // Initial honor gain is 100th of the monsters honor
        var dGain = targetFame / 100.0 * (_honorDamage / _totalDamage);

        if (_honorDamage == _totalDamage && _firstHit == FirstHit.Granted)
        {
            dGain *= 1.5; // honor gain is increased a lot more if the combat was fully honorable
        }
        else
        {
            dGain *= 0.9;
        }

        // Minimum gain of 1 honor when the honor is under the monsters fame
        var gain = Math.Clamp((int)dGain, 1, 200);

        if (VirtueSystem.IsHighestPath(Source, VirtueName.Honor))
        {
            Source.SendLocalizedMessage(1063228); // You cannot gain more Honor.
            return;
        }

        var gainedPath = false;
        if (VirtueSystem.Award(Source, VirtueName.Honor, gain, ref gainedPath))
        {
            if (gainedPath)
            {
                Source.SendLocalizedMessage(1063226); // You have gained a path in Honor!
            }
            else
            {
                Source.SendLocalizedMessage(1063225); // You have gained in Honor.
            }
        }
    }

    public bool CheckDistance() => Utility.InRange(Source.Location, Target.Location, 18);

    public void Cancel()
    {
        Source.SentHonorContext = null;
        ((IHonorTarget)Target).ReceivedHonorContext = null;

        _timer.Stop();
    }

    private enum FirstHit
    {
        NotDelivered,
        Delivered,
        Granted
    }

    private class InternalTimer : Timer
    {
        private readonly HonorContext _context;

        public InternalTimer(HonorContext context) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0)) =>
            _context = context;

        protected override void OnTick()
        {
            if (!_context.CheckDistance())
            {
                _context.Cancel();
            }
        }
    }
}
