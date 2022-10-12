using Server.Collections;

namespace Server.Spells.Mysticism
{
    public class HailStormSpell : MysticSpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Hail Storm",
            "Kal Des Ylem",
            -1,
            9002,
            Reagent.DragonsBlood,
            Reagent.Bloodmoss,
            Reagent.BlackPearl,
            Reagent.MandrakeRoot
        );

        public HailStormSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Seventh;
        public override int GetMana() => 50;

        public void Target(IPoint3D p)
        {
            var loc = (p as Item)?.GetWorldLocation() ?? new Point3D(p);

            if (SpellHelper.CheckTown(loc, Caster) && CheckSequence())
            {
                /* Summons a storm of hailstones that strikes all Targets within a radius around the Target's Location,
                 * dealing cold damage.
                 */

                SpellHelper.Turn(Caster, p);

                var map = Caster.Map;

                if (map != null)
                {
                    using var pool = PooledRefQueue<Mobile>.Create();
                    var pvp = false;

                    PlayEffect(loc, Caster.Map);

                    foreach (var m in map.GetMobilesInRange(loc, 2))
                    {
                        if (m == Caster || !SpellHelper.ValidIndirectTarget(Caster, m) || !Caster.CanBeHarmful(m, false)
                            || !Caster.CanSee(m) || !Caster.InLOS(m))
                        {
                            continue;
                        }

                        pool.Enqueue(m);

                        if (m.Player)
                        {
                            pvp = true;
                        }
                    }

                    double damage = GetNewAosDamage(51, 1, 5, pvp);

                    while (pool.Count > 0)
                    {
                        var m = pool.Dequeue();
                        Caster.DoHarmful(m);
                        SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);
                    }
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this);
        }

        private static void PlayEffect(Point3D p, Map map)
        {
            Effects.PlaySound(p, map, 0x64F);

            PlaySingleEffect(p, map, -1, 1, -1, 1);
            PlaySingleEffect(p, map, -2, 0, -3, -1);
            PlaySingleEffect(p, map, -3, -1, -1, 1);
            PlaySingleEffect(p, map, 1, 3, -1, 1);
            PlaySingleEffect(p, map, -1, 1, 1, 3);
        }

        private static void PlaySingleEffect(Point3D p, Map map, int a, int b, int c, int d)
        {
            var x = p.X;
            var y = p.Y;
            var z = p.Z + 18;

            SendEffectPacket(p, map, new Point3D(x + a, y + c, z), new Point3D(x + a, y + c, z));
            SendEffectPacket(p, map, new Point3D(x + b, y + c, z), new Point3D(x + b, y + c, z));
            SendEffectPacket(p, map, new Point3D(x + b, y + d, z), new Point3D(x + b, y + d, z));
            SendEffectPacket(p, map, new Point3D(x + a, y + d, z), new Point3D(x + a, y + d, z));

            SendEffectPacket(p, map, new Point3D(x + b, y + c, z), new Point3D(x + a, y + c, z));
            SendEffectPacket(p, map, new Point3D(x + b, y + d, z), new Point3D(x + b, y + c, z));
            SendEffectPacket(p, map, new Point3D(x + a, y + d, z), new Point3D(x + b, y + d, z));
            SendEffectPacket(p, map, new Point3D(x + a, y + c, z), new Point3D(x + a, y + d, z));
        }

        private static void SendEffectPacket(Point3D p, Map map, Point3D orig, Point3D dest)
        {
            Effects.SendMovingEffect(
                p,
                map,
                0x36D4,
                orig,
                dest,
                0,
                0,
                false,
                false,
                0x63,
                0x4
            );
        }
    }
}
