using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class CandyCane : Food
    {
        private static readonly Dictionary<Mobile, CandyCaneTimer> m_ToothAches = new();

        [Constructible]
        public CandyCane()
            : this(0x2bdd + Utility.Random(4))
        {
        }

        public CandyCane(int itemID) : base(itemID)
        {
            Stackable = false;
            LootType = LootType.Blessed;
        }

        public CandyCane(Serial serial)
            : base(serial)
        {
        }

        private static CandyCaneTimer EnsureTimer(Mobile from)
        {
            if (!m_ToothAches.TryGetValue(from, out var timer))
            {
                m_ToothAches[from] = timer = new CandyCaneTimer(from);
            }

            return timer;
        }

        public static int GetToothAche(Mobile from) => m_ToothAches.TryGetValue(from, out var timer) ? timer.Eaten : 0;

        public static void SetToothAche(Mobile from, int value)
        {
            EnsureTimer(from).Eaten = value;
        }

        public override bool CheckHunger(Mobile from)
        {
            EnsureTimer(from).Eaten += 32;

            from.SendLocalizedMessage(1077387); // You feel as if you could eat as much as you wanted!
            return true;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }

        public class CandyCaneTimer : Timer
        {
            public CandyCaneTimer(Mobile eater)
                : base(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30))
            {
                Eater = eater;
                Start();
            }

            public Mobile Eater { get; }

            public int Eaten { get; set; }

            protected override void OnTick()
            {
                --Eaten;

                if (Eater?.Deleted != false || Eaten <= 0)
                {
                    Stop();
                    m_ToothAches.Remove(Eater);
                }
                else if (Eater.Map != Map.Internal && Eater.Alive)
                {
                    if (Eaten > 60)
                    {
                        Eater.Say(1077388 + Utility.Random(5));

                        /* ARRGH! My tooth hurts sooo much!
                         * You just can't find a good Britannian dentist these days...
                         * My teeth!
                         * MAKE IT STOP!
                         * AAAH! It feels like someone kicked me in the teeth!
                         */

                        if (Utility.RandomBool() && Eater.Body.IsHuman && !Eater.Mounted)
                        {
                            Eater.Animate(32, 5, 1, true, false, 0);
                        }
                    }
                    else if (Eaten == 60)
                    {
                        Eater.SendLocalizedMessage(1077393); // The extreme pain in your teeth subsides.
                    }
                }
            }
        }
    }

    public class GingerBreadCookie : Food
    {
        private readonly int[] m_Messages =
        {
            0,
            1077396, // Noooo!
            1077397, // Please don't eat me... *whimper*
            1077405, // Not the face!
            1077406, // Ahhhhhh! My foot's gone!
            1077407, // Please. No! I have gingerkids!
            1077408, // No, no! I'm really made of poison. Really.
            1077409  // Run, run as fast as you can! You can't catch me! I'm the gingerbread man!
        };

        [Constructible]
        public GingerBreadCookie()
            : base(Utility.RandomBool() ? 0x2be1 : 0x2be2)
        {
            Stackable = false;
            LootType = LootType.Blessed;
        }

        public GingerBreadCookie(Serial serial)
            : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            var message = m_Messages.RandomElement();

            if (message != 0)
            {
                SendLocalizedMessageTo(from, message);
                return false;
            }

            return base.Eat(from);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
