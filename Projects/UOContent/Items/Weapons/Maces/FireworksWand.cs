using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FireworksWand : MagicWand
    {
        [SerializableField(0)]
        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _charges;

        [Constructible]
        public FireworksWand(int charges = 100)
        {
            Charges = charges;
            LootType = LootType.Blessed;
        }

        public override int LabelNumber => 1041424; // a fireworks wand

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1060741, $"{_charges}"); // charges: ~1_val~
        }

        public override void OnDoubleClick(Mobile from)
        {
            BeginLaunch(from, true);
        }

        public void BeginLaunch(Mobile from, bool useCharges)
        {
            var map = from.Map;

            if (map == null || map == Map.Internal)
            {
                return;
            }

            if (useCharges)
            {
                if (Charges > 0)
                {
                    --Charges;
                }
                else
                {
                    from.SendLocalizedMessage(502412); // There are no charges left on that item.
                    return;
                }
            }

            from.SendLocalizedMessage(502615); // You launch a firework!

            var ourLoc = GetWorldLocation();

            var startLoc = new Point3D(ourLoc.X, ourLoc.Y, ourLoc.Z + 10);
            var endLoc = new Point3D(
                startLoc.X + Utility.RandomMinMax(-2, 2),
                startLoc.Y + Utility.RandomMinMax(-2, 2),
                startLoc.Z + 32
            );

            Effects.SendMovingEffect(
                new Entity(Serial.Zero, startLoc, map),
                new Entity(Serial.Zero, endLoc, map),
                0x36E4,
                5,
                0
            );

            Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => FinishLaunch(endLoc, map));
        }

        private static void FinishLaunch(Point3D endLoc, Map map)
        {
            var hue = Utility.Random(40) switch
            {
                < 8  => 0x66D,
                < 10 => 0x482,
                < 12 => 0x47E,
                < 16 => 0x480,
                < 20 => 0x47F,
                _    => 0
            };

            if (Utility.RandomBool())
            {
                hue = Utility.RandomList(0x47E, 0x47F, 0x480, 0x482, 0x66D);
            }

            var renderMode = Utility.RandomList(0, 2, 3, 4, 5, 7);

            Effects.PlaySound(endLoc, map, Utility.Random(0x11B, 4));
            Effects.SendLocationEffect(endLoc, map, 0x373A + 0x10 * Utility.Random(4), 16, 10, hue, renderMode);
        }
    }
}
