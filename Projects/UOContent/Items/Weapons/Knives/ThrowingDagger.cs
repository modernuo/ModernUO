using System;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items
{
    [Flippable(0xF52, 0xF51)]
    [SerializationGenerator(0, false)]
    public partial class ThrowingDagger : Item
    {
        [Constructible]
        public ThrowingDagger() : base(0xF52)
        {
            Weight = 1.0;
            Layer = Layer.OneHanded;
        }

        public override string DefaultName => "a throwing dagger";

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Items.Contains(this))
            {
                var t = new InternalTarget(this);
                from.Target = t;
            }
            else
            {
                from.SendMessage("You must be holding that weapon to use it.");
            }
        }

        private class InternalTarget : Target
        {
            private readonly ThrowingDagger m_Dagger;

            public InternalTarget(ThrowingDagger dagger) : base(10, false, TargetFlags.Harmful) => m_Dagger = dagger;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Dagger.Deleted)
                {
                    return;
                }

                if (!from.Items.Contains(m_Dagger))
                {
                    from.SendMessage("You must be holding that weapon to use it.");
                }
                else if (targeted is Mobile m && m != from && from.HarmfulCheck(m))
                {
                    var to = from.GetDirectionTo(m);

                    from.Direction = to;

                    from.Animate(from.Mounted ? 26 : 9, 7, 1, true, false, 0);

                    if (Utility.RandomDouble() >= Math.Sqrt(m.Dex / 100.0) * 0.8)
                    {
                        from.MovingEffect(m, 0x1BFE, 7, 1, false, false, 0x481, 0);

                        AOS.Damage(m, from, Utility.Random(5, from.Str / 10), 100, 0, 0, 0, 0);

                        m_Dagger.MoveToWorld(m.Location, m.Map);
                    }
                    else
                    {
                        int x = 0, y = 0;

                        switch (to & Direction.Mask)
                        {
                            case Direction.North:
                                {
                                    --y;
                                    break;
                                }
                            case Direction.South:
                                {
                                    ++y;
                                    break;
                                }
                            case Direction.West:
                                {
                                    --x;
                                    break;
                                }
                            case Direction.East:
                                {
                                    ++x;
                                    break;
                                }
                            case Direction.Up:
                                {
                                    --x;
                                    --y;
                                    break;
                                }
                            case Direction.Down:
                                {
                                    ++x;
                                    ++y;
                                    break;
                                }
                            case Direction.Left:
                                {
                                    --x;
                                    ++y;
                                    break;
                                }
                            case Direction.Right:
                                {
                                    ++x;
                                    --y;
                                    break;
                                }
                        }

                        x += Utility.Random(-1, 3);
                        y += Utility.Random(-1, 3);

                        x += m.X;
                        y += m.Y;

                        m_Dagger.MoveToWorld(new Point3D(x, y, m.Z), m.Map);

                        from.MovingEffect(m_Dagger, 0x1BFE, 7, 1, false, false, 0x481, 0);

                        from.SendMessage("You miss.");
                    }
                }
            }
        }
    }
}
