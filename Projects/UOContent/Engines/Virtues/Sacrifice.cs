using System;
using System.Runtime.CompilerServices;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server
{
    public static class SacrificeVirtue
    {
        private const int LossAmount = 500;
        private static readonly TimeSpan GainDelay = TimeSpan.FromDays(1.0);
        private static readonly TimeSpan LossDelay = TimeSpan.FromDays(7.0);

        public static void Initialize()
        {
            VirtueGump.Register(110, OnVirtueUsed);
        }

        public static void OnVirtueUsed(Mobile from)
        {
            if (!from.Hidden)
            {
                if (from.Alive)
                {
                    from.Target = new InternalTarget();
                }
                else
                {
                    Resurrect(from);
                }
            }
            else
            {
                from.SendLocalizedMessage(1052015); // You cannot do that while hidden.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldAtrophy(PlayerMobile pm) => pm.LastSacrificeLoss + LossDelay < Core.Now;

        public static void CheckAtrophy(PlayerMobile pm)
        {
            if (ShouldAtrophy(pm))
            {
                if (VirtueHelper.Atrophy(pm, VirtueName.Sacrifice, LossAmount))
                {
                    pm.SendLocalizedMessage(1052041); // You have lost some Sacrifice.
                }

                var level = VirtueHelper.GetLevel(pm, VirtueName.Sacrifice);

                pm.AvailableResurrects = (int)level;
                pm.LastSacrificeLoss = Core.Now;
            }
        }

        public static void Resurrect(Mobile from)
        {
            if (from.Alive)
            {
                return;
            }

            if (from is not PlayerMobile pm)
            {
                return;
            }

            if (from.Criminal)
            {
                from.SendLocalizedMessage(1052007); // You cannot use this ability while flagged as a criminal.
            }
            else if (!VirtueHelper.IsSeeker(from, VirtueName.Sacrifice))
            {
                from.SendLocalizedMessage(1052004); // You cannot use this ability.
            }
            else if (pm.AvailableResurrects <= 0)
            {
                from.SendLocalizedMessage(1052005); // You do not have any resurrections left.
            }
            else
            {
                /*
                 * We need to wait for them to accept the gump or they can just use
                 * Sacrifice and cancel to have items in their backpack for free.
                 */
                from.CloseGump<ResurrectGump>();
                from.SendGump(new ResurrectGump(from, true));
            }
        }

        public static void Sacrifice(Mobile from, object targeted)
        {
            if (!from.CheckAlive())
            {
                return;
            }

            if (from is not PlayerMobile pm)
            {
                return;
            }

            if (targeted is not Mobile targ)
            {
                return;
            }

            if (!ValidateCreature(targ))
            {
                from.SendLocalizedMessage(1052014); // You cannot sacrifice your fame for that creature.
            }
            else if (targ.Hits * 100 / Math.Max(targ.HitsMax, 1) < 90)
            {
                from.SendLocalizedMessage(1052013); // You cannot sacrifice for this monster because it is too damaged.
            }
            else if (from.Hidden)
            {
                from.SendLocalizedMessage(1052015); // You cannot do that while hidden.
            }
            else if (VirtueHelper.IsHighestPath(from, VirtueName.Sacrifice))
            {
                from.SendLocalizedMessage(1052068); // You have already attained the highest path in this virtue.
            }
            else if (from.Fame < 2500)
            {
                from.SendLocalizedMessage(1052017); // You do not have enough fame to sacrifice.
            }
            else if (Core.Now < pm.LastSacrificeGain + GainDelay)
            {
                from.SendLocalizedMessage(1052016); // You must wait approximately one day before sacrificing again.
            }
            else
            {
                int toGain = from.Fame switch
                {
                    < 5000  => 500,
                    < 10000 => 1000,
                    _       => 2000
                };

                from.Fame = 0;

                // I have seen the error of my ways!
                targ.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1052009);

                from.SendLocalizedMessage(1052010); // You have set the creature free.

                Timer.StartTimer(TimeSpan.FromSeconds(1.0), targ.Delete);

                pm.LastSacrificeGain = Core.Now;

                var gainedPath = false;

                if (VirtueHelper.Award(from, VirtueName.Sacrifice, toGain, ref gainedPath))
                {
                    if (gainedPath)
                    {
                        from.SendLocalizedMessage(1052008); // You have gained a path in Sacrifice!

                        if (pm.AvailableResurrects < 3)
                        {
                            ++pm.AvailableResurrects;
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(1054160); // You have gained in sacrifice.
                    }
                }

                from.SendLocalizedMessage(1052016); // You must wait approximately one day before sacrificing again.
            }
        }

        public static bool ValidateCreature(Mobile m) =>
            (m is not BaseCreature creature || !creature.Controlled && !creature.Summoned) &&
            m is Lich or Succubus or Daemon or EvilMage or EnslavedGargoyle or GargoyleEnforcer;

        private class InternalTarget : Target
        {
            public InternalTarget() : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Sacrifice(from, targeted);
            }
        }
    }
}
