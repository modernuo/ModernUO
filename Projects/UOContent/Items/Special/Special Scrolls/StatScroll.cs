using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StatCapScroll : SpecialScroll
{
    [Constructible]
    public StatCapScroll(int value = 105) : base(SkillName.Alchemy, value) => Hue = 0x481;

    /* Using a scroll increases the maximum amount of a specific skill or your maximum statistics.
     * When used, the effect is not immediately seen without a gain of points with that skill or statistics.
     * You can view your maximum skill values in your skills window.
     * You can view your maximum statistic value in your statistics window.
     */
    public override int Message => 1049469;

    public override int Title
    {
        get
        {
            var level = ((int)Value - 230) / 5;

            /* Wondrous Scroll (+5 Maximum Stats): OR
             * Exalted Scroll (+10 Maximum Stats): OR
             * Mythical Scroll (+15 Maximum Stats): OR
             * Legendary Scroll (+20 Maximum Stats): OR
             * Ultimate Scroll (+25 Maximum Stats):
             */
            if (level is >= 0 and <= 4 && Value % 5 == 0)
            {
                return 1049458 + level;
            }

            return 0;
        }
    }

    public override string DefaultTitle =>
        $"<basefont color=#FFFFFF>Power Scroll ({((int)Value - 225 >= 0 ? "+" : "")}{(int)Value - 225} Maximum Stats):</basefont>";

    public override void AddNameProperty(IPropertyList list)
    {
        var level = ((int)Value - 230) / 5;

        if (level is >= 0 and <= 4 && (int)Value % 5 == 0)
        {
            /* a wondrous scroll of ~1_type~ (+5 Maximum Stats) OR
             * an exalted scroll of ~1_type~ (+10 Maximum Stats) OR
             * a mythical scroll of ~1_type~ (+15 Maximum Stats) OR
             * a legendary scroll of ~1_type~ (+20 Maximum Stats) OR
             * an ultimate scroll of ~1_type~ (+25 Maximum Stats)
             */
            list.Add(1049463 + level, 1049476);
        }
        else
        {
            var diff = Value - 225;
            list.Add($"a scroll of power ({(diff >= 0 ? "+" : "")}{diff} Maximum Stats)");
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        var level = ((int)Value - 230) / 5;

        if (level is >= 0 and <= 4 && (int)Value % 5 == 0)
        {
            LabelTo(from, 1049463 + level, "#1049476");
        }
        else
        {
            var diff = Value - 225;
            LabelTo(from, $"a scroll of power ({(diff >= 0 ? "+" : "")}{diff} Maximum Stats)");
        }
    }

    public override bool CanUse(Mobile from)
    {
        if (!base.CanUse(from))
        {
            return false;
        }

        var newValue = (int)Value;

        if (from is PlayerMobile { HasStatReward: true })
        {
            newValue += 5;
        }

        if (from.StatCap >= newValue)
        {
            from.SendLocalizedMessage(1049510); // Your stats are too high for this power scroll.
            return false;
        }

        return true;
    }

    public override void Use(Mobile from)
    {
        if (!CanUse(from))
        {
            return;
        }

        from.SendLocalizedMessage(1049512); // You feel a surge of magic as the scroll enhances your powers!

        if (from is PlayerMobile { HasStatReward: true } mobile)
        {
            mobile.StatCap = (int)Value + 5;
        }
        else
        {
            from.StatCap = (int)Value;
        }

        Effects.SendLocationParticles(
            EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
            0,
            0,
            0,
            0,
            0,
            5060,
            0
        );
        Effects.PlaySound(from.Location, from.Map, 0x243);

        Effects.SendMovingParticles(
            new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 6, from.Z + 15), from.Map),
            from,
            0x36D4,
            7,
            0,
            false,
            true,
            0x497,
            0,
            9502,
            1,
            0,
            (EffectLayer)255,
            0x100
        );
        Effects.SendMovingParticles(
            new Entity(Serial.Zero, new Point3D(from.X - 4, from.Y - 6, from.Z + 15), from.Map),
            from,
            0x36D4,
            7,
            0,
            false,
            true,
            0x497,
            0,
            9502,
            1,
            0,
            (EffectLayer)255,
            0x100
        );
        Effects.SendMovingParticles(
            new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 4, from.Z + 15), from.Map),
            from,
            0x36D4,
            7,
            0,
            false,
            true,
            0x497,
            0,
            9502,
            1,
            0,
            (EffectLayer)255,
            0x100
        );

        Effects.SendTargetParticles(from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

        Delete();
    }
}
