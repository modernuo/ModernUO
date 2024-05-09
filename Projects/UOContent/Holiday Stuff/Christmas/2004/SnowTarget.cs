using System;
using Server.Engines.ConPVP;
using Server.Targeting;

namespace Server.Items;

public class SnowTarget : Target
{
    private static readonly Type[] _snowPileTypes = [typeof(SnowPile), typeof(PileOfGlacialSnow)];

    public SnowTarget() : base(10, false, TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object target)
    {
        if (target == from)
        {
            from.SendLocalizedMessage(1005576); // You can't throw this at yourself.
            return;
        }

        if (target is not Mobile targ)
        {
            // You can only throw a snowball at something that can throw one back.
            from.SendLocalizedMessage(1005577);
            return;
        }

        var pack = targ.Backpack;

        if (from.Region.IsPartOf<SafeZone>() || targ.Region.IsPartOf<SafeZone>())
        {
            from.SendMessage("You may not throw snow here.");
            return;
        }

        if (pack?.FindItemByType(_snowPileTypes) == null)
        {
            // You can only throw a snowball at something that can throw one back.
            from.SendLocalizedMessage(1005577);
            return;
        }

        if (!from.BeginAction<SnowPile>())
        {
            from.SendLocalizedMessage(1005574); // The snow is not ready to be packed yet.  Keep trying.
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(5.0), from.EndAction<SnowPile>);

        from.PlaySound(0x145);

        from.Animate(9, 1, 1, true, false, 0);

        targ.SendLocalizedMessage(1010572); // You have just been hit by a snowball!
        from.SendLocalizedMessage(1010573); // You throw the snowball and hit the target!

        Effects.SendMovingEffect(from, targ, 0x36E4, 7, 0, false, true, 0x47F);
    }
}
