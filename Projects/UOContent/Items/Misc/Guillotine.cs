using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Guillotine : Item
{
    private DateTime m_NextUse;

    [Constructible]
    public Guillotine() : base(4656) => Movable = false;

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
        }
        else if (Visible && ItemID is 4656 or 4702 && Core.Now >= m_NextUse)
        {
            var p = GetWorldLocation();

            if (Utility.Random(Math.Max((from.X - p.X).Abs(), (from.Y - p.Y).Abs())) < 1)
            {
                Effects.PlaySound(from.Location, from.Map, from.GetHurtSound());
                from.PublicOverheadMessage(MessageType.Regular, from.SpeechHue, true, "Ouch!");
                SpellHelper.Damage(TimeSpan.FromSeconds(0.5), from, Utility.Dice(2, 10, 5));
            }

            Effects.PlaySound(GetWorldLocation(), Map, 0x387);

            Timer.StartTimer(TimeSpan.FromSeconds(0.25), Down1);
            Timer.StartTimer(TimeSpan.FromSeconds(0.50), Down2);

            Timer.StartTimer(TimeSpan.FromSeconds(5.00), BackUp);

            m_NextUse = Core.Now + TimeSpan.FromSeconds(10.0);
        }
    }

    private void Down1()
    {
        ItemID = ItemID == 4656 ? 4678 : 4712;
    }

    private void Down2()
    {
        ItemID = ItemID == 4678 ? 4679 : 4713;

        var p = GetWorldLocation();
        var f = Map;

        if (f == null)
        {
            return;
        }

        new Blood(4650).MoveToWorld(p, f);

        for (var i = 0; i < 4; ++i)
        {
            var x = p.X - 2 + Utility.Random(5);
            var y = p.Y - 2 + Utility.Random(5);
            var z = p.Z;

            if (!f.CanFit(x, y, z, 1, false, false))
            {
                z = f.GetAverageZ(x, y);

                if (!f.CanFit(x, y, z, 1, false, false))
                {
                    continue;
                }
            }

            var loc = f.GetRandomNearbyLocation(p, 2, -2, 4, 1);

            new Blood().MoveToWorld(loc, f);
        }
    }

    private void BackUp()
    {
        if (ItemID is 4678 or 4679)
        {
            ItemID = 4656;
        }
        else if (ItemID is 4712 or 4713)
        {
            ItemID = 4702;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        BackUp();
    }
}
