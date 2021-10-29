using System;

namespace Server.Spells.Mysticism
{
    public class MassSleepSpell : MysticSpell, ISpellTargetingPoint3D
    {
        public override SpellCircle Circle => SpellCircle.Fifth;

        private static readonly SpellInfo _info = new(
            "Mass Sleep", "Vas Zu",
            230,
            9022,
            Reagent.Ginseng,
            Reagent.Nightshade,
            Reagent.SpidersSilk
        );

        public MassSleepSpell(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, range: 10);
        }

        public void Target(IPoint3D p)
        {
            if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                Map map = Caster.Map;
                if (map != null)
                {
                    double duration = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 20 + 3;

                    var eable = map.GetMobilesInRange(new Point3D(p), 3);

                    foreach (var m in eable)
                    {
                        if (m == Caster || !SpellHelper.ValidIndirectTarget(Caster, m) || !Caster.CanSee(m) ||
                            !Caster.CanBeHarmful(m, false))
                        {
                            continue;
                        }

                        SleepSpell.DoSleep(Caster, m, TimeSpan.FromSeconds(duration - GetResistSkill(m) / 10));
                    }

                    eable.Free();
                }
            }

            FinishSequence();
        }
    }
}
