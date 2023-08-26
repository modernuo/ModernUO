using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public abstract partial class BaseGuard : Mobile
{
    public BaseGuard(Mobile target)
    {
        if (target != null)
        {
            Location = target.Location;
            Map = target.Map;

            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                5023
            );
        }
    }

    public abstract Mobile Focus { get; set; }

    public static void Spawn(Mobile caller, Mobile target, int amount = 1, bool onlyAdditional = false)
    {
        if (target?.Deleted != false)
        {
            return;
        }

        foreach (var m in target.GetMobilesInRange(15))
        {
            if (m is BaseGuard g)
            {
                if (g.Focus == null) // idling
                {
                    g.Focus = target;

                    --amount;
                }
                else if (g.Focus == target && !onlyAdditional)
                {
                    --amount;
                }
            }
        }

        while (amount-- > 0)
        {
            caller.Region.MakeGuard(target);
        }
    }

    public override bool OnBeforeDeath()
    {
        Effects.SendLocationParticles(
            EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            2023
        );

        PlaySound(0x1FE);

        Delete();

        return false;
    }
}
