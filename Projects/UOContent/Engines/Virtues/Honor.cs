using System;
using System.Runtime.CompilerServices;
using Server.Gumps;
using Server.Mobiles;
using Server.Regions;
using Server.Targeting;

namespace Server.Engines.Virtues;

public static class HonorVirtue
{
    public static readonly TimeSpan UseDelay = TimeSpan.FromMinutes(5.0);

    public static void Initialize()
    {
        VirtueGump.Register(107, OnVirtueUsed);
    }

    private static void OnVirtueUsed(Mobile from)
    {
        if (from.Alive)
        {
            from.SendLocalizedMessage(1063160); // Target what you wish to honor.
            from.Target = new InternalTarget();
        }
    }

    private static int GetHonorDuration(PlayerMobile from) =>
        VirtueSystem.GetLevel(from, VirtueName.Honor) switch
        {
            VirtueLevel.Seeker   => 30,
            VirtueLevel.Follower => 90,
            VirtueLevel.Knight   => 300,
            _                    => 0
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanUse(VirtueContext context) => Core.Now - context.LastHonorUse >= UseDelay;

    private static void EmbraceHonor(PlayerMobile pm)
    {
        var virtues = VirtueSystem.GetVirtues(pm);

        if (virtues?.HonorActive == true)
        {
            pm.SendLocalizedMessage(1063230); // You must wait awhile before you can embrace honor again.
            return;
        }

        if (GetHonorDuration(pm) == 0)
        {
            pm.SendLocalizedMessage(1063234); // You do not have enough honor to do that
            return;
        }

        if (virtues != null)
        {
            var waitTime = Core.Now - virtues.LastHonorUse;
            if (waitTime < UseDelay)
            {
                var remainingTime = UseDelay - waitTime;
                var remainingMinutes = (int)Math.Ceiling(remainingTime.TotalMinutes);

                // You must wait ~1_HONOR_WAIT~ minutes before embracing honor again
                pm.SendLocalizedMessage(1063240, remainingMinutes.ToString());
                return;
            }
        }

        pm.SendGump(new HonorSelf(pm));
    }

    public static void ActivateEmbrace(PlayerMobile pm)
    {
        var duration = GetHonorDuration(pm);
        var virtues = VirtueSystem.GetOrCreateVirtues(pm);

        int usedPoints = virtues.Honor switch
        {
            < 4399  => 400,
            < 10599 => 600,
            _       => 1000
        };

        VirtueSystem.Atrophy(pm, VirtueName.Honor, usedPoints);

        virtues.HonorActive = true;
        pm.SendLocalizedMessage(1063235); // You embrace your honor

        Timer.DelayCall(
            TimeSpan.FromSeconds(duration),
            (m) =>
            {
                // We get the virtues again, in case it was deleted/dereferenced
                var v = VirtueSystem.GetOrCreateVirtues(m);
                v.HonorActive = false;
                v.LastHonorUse = Core.Now;
                m.SendLocalizedMessage(1063236); // You no longer embrace your honor
            },
            pm
        );
    }

    private static void Honor(PlayerMobile source, Mobile target)
    {
        if (target is not IHonorTarget honorTarget)
        {
            return;
        }

        var reg = source.Region.GetRegion<GuardedRegion>();
        var map = source.Map;

        if (honorTarget.ReceivedHonorContext != null)
        {
            if (honorTarget.ReceivedHonorContext.Source == source)
            {
                return;
            }

            if (honorTarget.ReceivedHonorContext.CheckDistance())
            {
                source.SendLocalizedMessage(1063233); // Somebody else is honoring this opponent
                return;
            }
        }

        if (target.Hits < target.HitsMax)
        {
            source.SendLocalizedMessage(1063166); // You cannot honor this monster because it is too damaged.
            return;
        }

        if (target.Body.IsHuman && (target is not BaseCreature cret || !cret.AlwaysAttackable && !cret.AlwaysMurderer))
        {
            if (reg?.IsDisabled() != true)
            {
                // Allow honor on blue if not in a guarded region
            }
            else if ((map?.Rules & MapRules.HarmfulRestrictions) == 0)
            {
                // Allow honor on blue if in Fel
            }
            else
            {
                source.SendLocalizedMessage(1001018); // You cannot perform negative acts
                return;                               // cannot honor in trammel town on blue
            }
        }

        if (Core.ML && target is PlayerMobile)
        {
            source.SendLocalizedMessage(1075614); // You cannot honor other players.
            return;
        }

        source.SentHonorContext?.Cancel();

        _ = new HonorContext(source, target);

        source.Direction = source.GetDirectionTo(target);

        if (!source.Mounted)
        {
            source.Animate(32, 5, 1, true, true, 0);
        }
    }

    private class InternalTarget : Target
    {
        public InternalTarget() : base(12, false, TargetFlags.None) => CheckLOS = true;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from is not PlayerMobile pm)
            {
                return;
            }

            if (targeted == pm)
            {
                EmbraceHonor(pm);
            }
            else if (targeted is Mobile mobile)
            {
                Honor(pm, mobile);
            }
        }

        protected override void OnTargetOutOfRange(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(1063232); // You are too far away to honor your opponent
        }
    }
}
