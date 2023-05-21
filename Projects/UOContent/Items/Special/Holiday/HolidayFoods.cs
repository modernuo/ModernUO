using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CandyCane : Food
{
    private static readonly Dictionary<Mobile, CandyCaneTimer> _toothAches = new();

    [Constructible]
    public CandyCane() : this(0x2bdd + Utility.Random(4))
    {
    }

    public CandyCane(int itemID) : base(itemID)
    {
        Stackable = false;
        LootType = LootType.Blessed;
    }

    private static CandyCaneTimer EnsureTimer(Mobile from)
    {
        if (!_toothAches.TryGetValue(from, out var timer))
        {
            _toothAches[from] = timer = new CandyCaneTimer(from);
        }

        return timer;
    }

    public static int GetToothAche(Mobile from) => _toothAches.TryGetValue(from, out var timer) ? timer.Eaten : 0;

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

    public class CandyCaneTimer : Timer
    {
        private readonly Mobile _eater;

        public CandyCaneTimer(Mobile eater) : base(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30))
        {
            _eater = eater;
            Start();
        }

        public int Eaten { get; set; }

        protected override void OnTick()
        {
            --Eaten;

            if (_eater.Deleted || Eaten <= 0)
            {
                Stop();
                _toothAches.Remove(_eater);
            }
            else if (_eater.Map != Map.Internal && _eater.Alive)
            {
                if (Eaten > 60)
                {
                    _eater.Say(1077388 + Utility.Random(5));

                    /* ARRGH! My tooth hurts sooo much!
                     * You just can't find a good Britannian dentist these days...
                     * My teeth!
                     * MAKE IT STOP!
                     * AAAH! It feels like someone kicked me in the teeth!
                     */

                    if (Utility.RandomBool() && _eater.Body.IsHuman && !_eater.Mounted)
                    {
                        _eater.Animate(32, 5, 1, true, false, 0);
                    }
                }
                else if (Eaten == 60)
                {
                    _eater.SendLocalizedMessage(1077393); // The extreme pain in your teeth subsides.
                }
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GingerBreadCookie : Food
{
    private readonly int[] _messages =
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
    public GingerBreadCookie() : base(Utility.RandomBool() ? 0x2be1 : 0x2be2)
    {
        Stackable = false;
        LootType = LootType.Blessed;
    }

    public override bool Eat(Mobile from)
    {
        var message = _messages.RandomElement();

        if (message != 0)
        {
            SendLocalizedMessageTo(from, message);
            return false;
        }

        return base.Eat(from);
    }
}
