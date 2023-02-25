using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class EvalInt
    {
        public static void Initialize()
        {
            SkillInfo.Table[16].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTarget();

            m.SendLocalizedMessage(500906); // What do you wish to evaluate?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget() : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == targeted)
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500910); // Hmm, that person looks really silly.
                }
                else if (targeted is TownCrier crier)
                {
                    crier.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x3B2,
                        500907,
                        from.NetState
                    ); // He looks smart enough to remember the news.  Ask him about it.
                }
                else if (targeted is BaseVendor vendor && vendor.IsInvulnerable)
                {
                    vendor.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x3B2,
                        500909,
                        from.NetState
                    ); // That person could probably calculate the cost of what you buy from them.
                }
                else if (targeted is Mobile targ)
                {
                    var marginOfError = Math.Max(0, 20 - (int)(from.Skills.EvalInt.Value / 5));

                    var intel = targ.Int + Utility.RandomMinMax(-marginOfError, +marginOfError);
                    var mana = targ.Mana * 100 / Math.Max(targ.ManaMax, 1) +
                               Utility.RandomMinMax(-marginOfError, +marginOfError);

                    var intMod = Math.Clamp(intel / 10, 0, 10);
                    var mnMod = Math.Clamp(mana / 10, 0, 10);

                    int body;

                    if (targ.Body.IsHuman)
                    {
                        body = targ.Female ? 11 : 0;
                    }
                    else
                    {
                        body = 22;
                    }

                    if (from.CheckTargetSkill(SkillName.EvalInt, targ, 0.0, 120.0))
                    {
                        targ.PrivateOverheadMessage(
                            MessageType.Regular,
                            0x3B2,
                            1038169 + intMod + body,
                            from.NetState
                        ); // He/She/It looks [slighly less intelligent than a rock.]  [Of Average intellect] [etc...]

                        if (from.Skills.EvalInt.Base >= 76.0)
                        {
                            targ.PrivateOverheadMessage(
                                MessageType.Regular,
                                0x3B2,
                                1038202 + mnMod,
                                from.NetState
                            ); // That being is at [10,20,...] percent mental strength.
                        }
                    }
                    else
                    {
                        targ.PrivateOverheadMessage(
                            MessageType.Regular,
                            0x3B2,
                            1038166 + body / 11,
                            from.NetState
                        ); // You cannot judge his/her/its mental abilities.
                    }
                }
                else
                {
                    (targeted as Item)?.SendLocalizedMessageTo(
                        from,
                        500908
                    ); // It looks smarter than a rock, but dumber than a piece of wood.
                }
            }
        }
    }
}
