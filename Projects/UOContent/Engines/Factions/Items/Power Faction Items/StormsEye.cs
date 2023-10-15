using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Spells;
using Server.Targeting;

namespace Server
{
    public sealed class StormsEye : PowerFactionItem
    {
        public StormsEye()
            : base(3967) =>
            Hue = 1165;

        public StormsEye(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "storms eye";

        public override bool Use(Mobile user)
        {
            if (!Movable)
            {
                return false;
            }

            user.BeginTarget(
                12,
                true,
                TargetFlags.None,
                (from, obj, stormsEye) =>
                {
                    if (!stormsEye.Movable || stormsEye.Deleted || obj is not IPoint3D pt)
                    {
                        return;
                    }

                    SpellHelper.GetSurfaceTop(ref pt);

                    var origin = new Point3D(pt);
                    var facet = from.Map;

                    if (facet?.CanFit(pt.X, pt.Y, pt.Z, 16, false, false) != true)
                    {
                        return;
                    }

                    stormsEye.Movable = false;

                    Effects.SendMovingEffect(
                        from,
                        new Entity(Serial.Zero, origin, facet),
                        ItemID & 0x3FFF,
                        7,
                        0,
                        false,
                        false,
                        Hue - 1
                    );

                    Timer.StartTimer(TimeSpan.FromSeconds(0.5), () => OnDelay(from, stormsEye, origin, facet));
                },
                this
            );

            return false;
        }

        private static void OnDelay(Mobile from, StormsEye stormsEye, Point3D origin, Map facet)
        {
            stormsEye.Delete();

            Effects.PlaySound(origin, facet, 530);
            Effects.PlaySound(origin, facet, 263);

            Effects.SendLocationEffect(
                origin,
                facet,
                14284,
                96,
                1,
                0,
                2
            );

            Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => OnHit(from, origin, facet));
        }

        private static void OnHit(Mobile from, Point3D origin, Map facet)
        {
            var eable = facet.GetMobilesInRange(origin, 12);
            var targets = new List<Mobile>();
            foreach (var m in eable)
            {
                if (from.CanBeHarmful(m, false) &&
                    m.InLOS(new Point3D(origin.X, origin.Y, origin.Z + 1)) &&
                    Faction.Find(m) != null)
                {
                    targets.Add(from);
                }
            }

            foreach (var mob in targets)
            {
                var damage = mob.Hits * 6 / 10;

                if (!mob.Player && damage < 10)
                {
                    damage = 10;
                }
                else if (damage > 75)
                {
                    damage = 75;
                }

                Effects.SendMovingEffect(
                    new Entity(Serial.Zero, new Point3D(origin.X, origin.Y, origin.Z + 4), facet),
                    mob,
                    14068,
                    1,
                    32,
                    false,
                    false,
                    1111,
                    2
                );

                from.DoHarmful(mob);

                SpellHelper.Damage(
                    TimeSpan.FromSeconds(0.50),
                    mob,
                    from,
                    damage / 3.0,
                    0,
                    0,
                    0,
                    0,
                    100
                );
                SpellHelper.Damage(
                    TimeSpan.FromSeconds(0.70),
                    mob,
                    from,
                    damage / 3.0,
                    0,
                    0,
                    0,
                    0,
                    100
                );
                SpellHelper.Damage(
                    TimeSpan.FromSeconds(1.00),
                    mob,
                    from,
                    damage / 3.0,
                    0,
                    0,
                    0,
                    0,
                    100
                );

                Timer.StartTimer(TimeSpan.FromSeconds(0.50), () => mob.PlaySound(0x1FB));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
