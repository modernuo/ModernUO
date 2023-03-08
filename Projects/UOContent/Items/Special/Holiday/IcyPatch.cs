using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class IcyPatch : Item
{
    /* On OSI, the iceypatch with itemid 0x122a is "rarer", so we will give it 1:10 chance of creating it that way */
    [Constructible]
    public IcyPatch() : this(Utility.Random(10) == 0 ? 0x122A : 0x122F)
    {
    }

    public IcyPatch(int itemid) : base(itemid) => Hue = 0x481;

    public override int LabelNumber => 1095159; // An Icy Patch
    public override double DefaultWeight => 5.0;

    public override bool OnMoveOver(Mobile m)
    {
        if (m is PlayerMobile && m.Alive && m.AccessLevel == AccessLevel.Player)
        {
            switch (Utility.Random(3))
            {
                case 0:
                    {
                        RunSequence(m, 1095160, false);
                        break; // You steadily walk over the slippery surface.
                    }
                case 1:
                    {
                        RunSequence(m, 1095161, true);
                        break; // You skillfully manage to maintain your balance.
                    }
                default:
                    {
                        RunSequence(m, 1095162, true);
                        break; // You lose your footing and ungracefully splatter on the ground.
                    }
            }
        }

        return base.OnMoveOver(m);
    }

    public virtual void RunSequence(Mobile m, int message, bool freeze)
    {
        if (freeze)
        {
            m.Frozen = true;
            Timer.StartTimer(TimeSpan.FromSeconds(message == 1095162 ? 2.0 : 1.25), () => m.Frozen = false);
        }

        m.SendLocalizedMessage(message);

        var action = 0;
        var sound = 0;

        if (message == 1095162)
        {
            if (m.Mounted)
            {
                m.Mount.Rider = null;
            }

            var p = new Point3D(Location);
            var map = Map;

            if (SpellHelper.FindValidSpawnLocation(map, ref p, true))
            {
                Timer.StartTimer(TimeSpan.FromSeconds(0), () => m.MoveToWorld(p, map));
            }

            action = 21 + Utility.Random(2);
            sound = m.Female ? 0x317 : 0x426;
        }
        else if (message == 1095161)
        {
            action = 17;
            sound = m.Female ? 0x319 : 0x429;
        }

        if (action > 0)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(0.4),
                () =>
                {
                    if (!m.Mounted)
                    {
                        m.Animate(action, 1, 1, false, true, 0);
                    }

                    m.PlaySound(sound);
                }
            );
        }
    }
}
