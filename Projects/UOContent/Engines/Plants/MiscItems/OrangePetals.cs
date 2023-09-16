using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class OrangePetals : Item
{
    private static readonly Dictionary<Mobile, Timer> _table =
        new();

    [Constructible]
    public OrangePetals(int amount = 1) : base(0x1021)
    {
        Stackable = true;
        Hue = 0x2B;
        Amount = amount;
    }

    public override int LabelNumber => 1053122; // orange petals

    public override double DefaultWeight => 0.1;

    public override bool CheckItemUse(Mobile from, Item item)
    {
        if (item != this)
        {
            return base.CheckItemUse(from, item);
        }

        if (from != RootParent)
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
            return false;
        }

        return base.CheckItemUse(from, item);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (UnderEffect(from))
        {
            // * You already feel resilient! You decide to save the petal for later *
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061904);
            return;
        }

        // * You eat the orange petal.  You feel more resilient! *
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061905);
        from.PlaySound(0x3B);

        Timer timer = new OrangePetalsTimer(from);
        timer.Start();

        _table[from] = timer;

        Consume();
    }

    public static void RemoveEffect(Mobile m)
    {
        if (_table.Remove(m, out var timer))
        {
            timer.Stop();
        }
    }

    public static bool UnderEffect(Mobile m) => _table.ContainsKey(m);

    private class OrangePetalsTimer : Timer
    {
        private readonly Mobile _mobile;

        public OrangePetalsTimer(Mobile from) : base(TimeSpan.FromMinutes(5.0)) => _mobile = from;

        protected override void OnTick()
        {
            if (!_mobile.Deleted)
            {
                // * You feel the effects of your poison resistance wearing off *
                _mobile.LocalOverheadMessage(MessageType.Regular, 0x3F, 1053091);
            }

            RemoveEffect(_mobile);
        }
    }
}
