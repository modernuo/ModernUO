using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Events.Halloween;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Events
{
    public static class TrickOrTreat
    {
        public static TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        public static void Initialize()
        {
            var now = Core.Now;

            if (now >= HolidaySettings.StartHalloween && now <= HolidaySettings.FinishHalloween)
            {
                EventSink.Speech += EventSink_Speech;
            }
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            if (e.Speech.InsensitiveContains("trick or treat"))
            {
                e.Mobile.Target = new TrickOrTreatTarget();

                e.Mobile.SendLocalizedMessage(1076764); /* Pick someone to Trick or Treat. */
            }
        }

        public static void Bleeding(Mobile m_From)
        {
            if (CheckMobile(m_From))
            {
                if (m_From.Location != Point3D.Zero)
                {
                    var amount = Utility.RandomMinMax(3, 7);

                    for (var i = 0; i < amount; i++)
                    {
                        new Blood(Utility.RandomMinMax(0x122C, 0x122F)).MoveToWorld(
                            RandomPointOneAway(m_From.X, m_From.Y, m_From.Z, m_From.Map),
                            m_From.Map
                        );
                    }
                }
            }
        }

        public static void RemoveHueMod(Mobile target)
        {
            if (target?.Deleted == false)
            {
                target.SolidHueOverride = -1;
            }
        }

        public static void SolidHueMobile(Mobile target)
        {
            if (CheckMobile(target))
            {
                target.SolidHueOverride = Utility.RandomMinMax(2501, 2644);

                Timer.StartTimer(TimeSpan.FromSeconds(10), () => RemoveHueMod(target));
            }
        }

        public static void MakeTwin(Mobile m_From)
        {
            var m_Items = new List<Item>();

            if (CheckMobile(m_From))
            {
                Mobile twin = new NaughtyTwin(m_From);

                if (twin.Deleted)
                {
                    return;
                }

                foreach (var item in m_From.Items)
                {
                    if (item.Layer != Layer.Backpack && item.Layer != Layer.Mount && item.Layer != Layer.Bank)
                    {
                        m_Items.Add(item);
                    }
                }

                if (m_Items.Count > 0)
                {
                    for (var i = 0; i < m_Items.Count; i++) /* dupe exploits start out like this ... */
                    {
                        twin.AddItem(Mobile.LiftItemDupe(m_Items[i], 1));
                    }

                    foreach (var item in twin.Items) /* ... and end like this */
                    {
                        if (item.Layer != Layer.Backpack && item.Layer != Layer.Mount && item.Layer != Layer.Bank)
                        {
                            item.Movable = false;
                        }
                    }
                }

                twin.Hue = m_From.Hue;
                twin.Body = m_From.Body;
                twin.Kills = m_From.Kills;

                var point = RandomPointOneAway(m_From.X, m_From.Y, m_From.Z, m_From.Map);

                twin.MoveToWorld(m_From.Map.CanSpawnMobile(point) ? point : m_From.Location, m_From.Map);

                Timer.StartTimer(TimeSpan.FromSeconds(5), () => DeleteTwin(twin));
            }
        }

        public static void DeleteTwin(Mobile m_Twin)
        {
            if (CheckMobile(m_Twin))
            {
                m_Twin.Delete();
            }
        }

        public static Point3D RandomPointOneAway(int x, int y, int z, Map map)
        {
            var loc = new Point3D(x + Utility.Random(-1, 3), y + Utility.Random(-1, 3), 0);

            loc.Z = map.CanFit(loc, 0) ? map.GetAverageZ(loc.X, loc.Y) : z;

            return loc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckMobile(Mobile mobile) =>
            mobile?.Map != null && !mobile.Deleted && mobile.Alive && mobile.Map != Map.Internal;

        private class TrickOrTreatTarget : Target
        {
            public TrickOrTreatTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ == null || !CheckMobile(from))
                {
                    return;
                }

                if (targ is not Mobile)
                {
                    from.SendLocalizedMessage(1076781); /* There is little chance of getting candy from that! */
                    return;
                }

                var begged = targ as BaseVendor;

                if (begged?.Deleted != false)
                {
                    from.SendLocalizedMessage(1076765); /* That doesn't look friendly. */
                    return;
                }

                var now = Core.Now;

                if (CheckMobile(begged))
                {
                    if (begged.NextTrickOrTreat > now)
                    {
                        from.SendLocalizedMessage(1076767); /* That doesn't appear to have any more candy. */
                        return;
                    }

                    begged.NextTrickOrTreat = now + TimeSpan.FromMinutes(Utility.RandomMinMax(5, 10));

                    if (from.Backpack?.Deleted != false)
                    {
                        return;
                    }

                    if (Utility.RandomDouble() < 0.90)
                    {
                        begged.Say(
                            Utility.Random(3) switch
                            {
                                0 => 1076768, // Oooooh, aren't you cute!
                                1 => 1076779, // All right...This better not spoil your dinner!
                                _ => 1076778  // Here you go! Enjoy!
                            }
                        );

                        if (Utility.RandomDouble() < 0.01 && from.Skills.Begging.Value >= 100)
                        {
                            from.AddToBackpack(HolidaySettings.RandomGMBeggerItem);

                            from.SendLocalizedMessage(1076777); /* You receive a special treat! */
                        }
                        else
                        {
                            from.AddToBackpack(HolidaySettings.RandomTreat);

                            from.SendLocalizedMessage(1076769); /* You receive some candy. */
                        }
                    }
                    else
                    {
                        begged.Say(1076770); /* TRICK! */

                        var action = Utility.Random(4);

                        if (action == 0)
                        {
                            Timer.StartTimer(OneSecond, OneSecond, 10, () => Bleeding(from));
                        }
                        else if (action == 1)
                        {
                            Timer.StartTimer(TimeSpan.FromSeconds(2), () => SolidHueMobile(from));
                        }
                        else
                        {
                            Timer.StartTimer(TimeSpan.FromSeconds(2), () => MakeTwin(from));
                        }
                    }
                }
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class NaughtyTwin : BaseCreature
    {
        private static readonly Point3D[] Felucca_Locations =
        {
            new(4467, 1283, 5),                                  // Moonglow
            new(1336, 1997, 5),                                  // Britain
            new(1499, 3771, 5),                                  // Jhelom
            new(771, 752, 5),                                    // Yew
            new(2701, 692, 5),                                   // Minoc
            new(1828, 2948, -20),                                // Trinsic
            new(643, 2067, 5),                                   // Skara Brae
            new(3563, 2139, Map.Trammel.GetAverageZ(3563, 2139)) // (New) Magincia
        };

        private static readonly Point3D[] Malas_Locations =
        {
            new(1015, 527, -65), // Luna
            new(1997, 1386, -85) // Umbra
        };

        private static readonly Point3D[] Ilshenar_Locations =
        {
            new(1215, 467, -13),  // Compassion
            new(722, 1366, -60),  // Honesty
            new(744, 724, -28),   // Honor
            new(281, 1016, 0),    // Humility
            new(987, 1011, -32),  // Justice
            new(1174, 1286, -30), // Sacrifice
            new(1532, 1340, -3),  // Spirituality
            new(528, 216, -45),   // Valor
            new(1721, 218, 96)    // Chaos
        };

        private static readonly Point3D[] Tokuno_Locations =
        {
            new(1169, 998, 41), // Isamu-Jima
            new(802, 1204, 25), // Makoto-Jima
            new(270, 628, 15)   // Homare-Jima
        };

        private readonly Mobile m_From;

        public NaughtyTwin(Mobile from) : base(AIType.AI_Melee, FightMode.None)
        {
            if (TrickOrTreat.CheckMobile(from))
            {
                Body = from.Body;

                m_From = from;
                Name = $"{from.Name}\'s Naughty Twin";

                Timer.StartTimer(TrickOrTreat.OneSecond, () => StealCandyOrGate(m_From));
            }
        }

        public override void OnThink()
        {
            if (m_From?.Deleted != false)
            {
                Delete();
            }
        }

        public static Item FindCandyTypes(Mobile target)
        {
            Type[] types =
                { typeof(WrappedCandy), typeof(Lollipops), typeof(NougatSwirl), typeof(Taffy), typeof(JellyBeans) };

            return TrickOrTreat.CheckMobile(target) ? target.Backpack.FindItemByType(types) : null;
        }

        public static void StealCandyOrGate(Mobile target)
        {
            if (TrickOrTreat.CheckMobile(target))
            {
                if (Utility.RandomBool())
                {
                    var item = FindCandyTypes(target);

                    target.SendLocalizedMessage(1113967); /* Your naughty twin steals some of your candy. */

                    if (item?.Deleted == false)
                    {
                        item.Delete();
                    }
                }
                else
                {
                    target.SendLocalizedMessage(1113972); /* Your naughty twin teleports you away with a naughty laugh! */
                    target.MoveToWorld(RandomMoongate(target), target.Map);
                }
            }
        }

        public static Point3D RandomMoongate(Mobile target)
        {
            return target.Map.MapID switch
            {
                2 => Ilshenar_Locations.RandomElement(),
                3 => Malas_Locations.RandomElement(),
                4 => Tokuno_Locations.RandomElement(),
                _ => Felucca_Locations.RandomElement()
            };
        }
    }
}
