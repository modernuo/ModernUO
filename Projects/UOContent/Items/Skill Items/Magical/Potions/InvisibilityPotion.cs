using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class InvisibilityPotion : BasePotion
{
    private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

    [Constructible]
    public InvisibilityPotion() : base(0xF0A, PotionEffect.Invisibility) => Hue = 0x48D;

    public override int LabelNumber => 1072941; // Potion of Invisibility

    public override bool CanDrink(Mobile from)
    {
        if (!base.CanDrink(from))
        {
            return false;
        }

        if (from.Hidden)
        {
            from.SendLocalizedMessage(1073185); // You are already unseen.
            return false;
        }

        if (HasTimer(from))
        {
            from.SendLocalizedMessage(1073186); // An invisibility potion is already taking effect on your person.
            return false;
        }

        return true;
    }

    public override void Drink(Mobile from)
    {
        Timer.StartTimer(TimeSpan.FromSeconds(2), () => Hide(from), out var timerToken);
        _table[from] = timerToken;
        PlayDrinkEffect(from);
    }

    public static void Hide(Mobile m)
    {
        Effects.SendLocationParticles(
            EffectItem.Create(new Point3D(m.X, m.Y, m.Z + 16), m.Map, EffectItem.DefaultDuration),
            0x376A,
            10,
            15,
            5045
        );

        m.PlaySound(0x3C4);

        m.Hidden = true;

        if (m is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.HidingAndOrStealth);
            pm.AddBuff(new BuffInfo(BuffIcon.Invisibility, 1075825)); // Invisibility/Invisible
        }

        RemoveTimer(m);

        Timer.StartTimer(TimeSpan.FromSeconds(30), () => EndHide(m));
    }

    public static void EndHide(Mobile m)
    {
        m.RevealingAction();
        RemoveTimer(m);
    }

    public static bool HasTimer(Mobile m) => _table.ContainsKey(m);

    public static void RemoveTimer(Mobile m, bool interrupted = false)
    {
        if (_table.Remove(m, out var timer))
        {
            if (interrupted)
            {
                m.SendLocalizedMessage(1073187); // The invisibility effect is interrupted.
            }

            timer.Cancel();
        }
    }
}
