using Server.Collections;

namespace Server.Spells.Mysticism
{
    public class NetherCycloneSpell : MysticSpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Nether Cyclone",
            "Grav Hur",
            -1,
            9002,
            Reagent.MandrakeRoot,
            Reagent.Nightshade,
            Reagent.SulfurousAsh,
            Reagent.Bloodmoss
        );

        public NetherCycloneSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Eighth;

        public void Target(IPoint3D p)
        {
            var loc = (p as Item)?.GetWorldLocation() ?? new Point3D(p);

            if (SpellHelper.CheckTown(loc, Caster) && CheckSequence())
            {
                /* Summons a gale of lethal winds that strikes all Targets within a radius around
                 * the Target's Location, dealing chaos damage. In addition to inflicting damage,
                 * each Target of the Nether Cyclone temporarily loses a percentage of mana and
                 * stamina. The effectiveness of the Nether Cyclone is determined by a comparison
                 * between the Caster's Mysticism and either Focus or Imbuing (whichever is greater)
                 * skills and the Resisting Spells skill of the Target.
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
                        if (m == Caster)
                        {
                            continue;
                        }

                        if (SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false) && Caster.CanSee(m))
                        {
                            if (!Caster.InLOS(m))
                            {
                                continue;
                            }

                            pool.Enqueue(m);

                            if (m.Player)
                            {
                                pvp = true;
                            }
                        }
                    }

                    var damage = GetNewAosDamage(51, 1, 5, pvp);
                    var reduction = (GetBaseSkill(Caster) + GetDamageSkill(Caster)) / 1200.0;

                    while (pool.Count > 0)
                    {
                        var m = pool.Dequeue();
                        Caster.DoHarmful(m);
                        SpellHelper.Damage(this, m, damage, 0, 0, 0, 0, 0, 100);

                        var resistedReduction = reduction - m.Skills.MagicResist.Value / 800.0;

                        m.Stam -= (int)(m.StamMax * resistedReduction);
                        m.Mana -= (int)(m.ManaMax * resistedReduction);
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
            int x = p.X, y = p.Y, z = p.Z + 18;

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
                0x375A,
                orig,
                dest,
                0,
                0,
                false,
                false,
                0x49A,
                0x4
            );
        }
    }
}
