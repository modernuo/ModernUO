using Server.Collections;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Seventh
{
    public class MassDispelSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Mass Dispel",
            "Vas An Ort",
            263,
            9002,
            Reagent.Garlic,
            Reagent.MandrakeRoot,
            Reagent.BlackPearl,
            Reagent.SulfurousAsh
        );

        public MassDispelSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Seventh;

        public void Target(IPoint3D p)
        {
            if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                var map = Caster.Map;

                if (map != null)
                {
                    using var queue = PooledRefQueue<Mobile>.Create();
                    foreach (var bc in map.GetMobilesInRange<BaseCreature>(new Point3D(p), 8))
                    {
                        if (!(bc.IsDispellable && Caster.CanBeHarmful(bc, false)))
                        {
                            continue;
                        }

                        var dispelChance =
                            (50.0 + 100 * (Caster.Skills.Magery.Value - bc.DispelDifficulty) / (bc.DispelFocus * 2)) / 100;

                        if (dispelChance > Utility.RandomDouble())
                        {
                            Effects.SendLocationParticles(
                                EffectItem.Create(bc.Location, bc.Map, EffectItem.DefaultDuration),
                                0x3728,
                                8,
                                20,
                                5042
                            );
                            Effects.PlaySound(bc, 0x201);

                            queue.Enqueue(bc);
                        }
                        else
                        {
                            Caster.DoHarmful(bc);
                            bc.FixedEffect(0x3779, 10, 20);
                        }
                    }

                    while (queue.Count > 0)
                    {
                        queue.Dequeue().Delete();
                    }
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, range: Core.ML ? 10 : 12);
        }
    }
}
