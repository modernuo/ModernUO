namespace Server.Spells.Sixth
{
    public class MassCurseSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Mass Curse",
            "Vas Des Sanct",
            218,
            9031,
            false,
            Reagent.Garlic,
            Reagent.Nightshade,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh
        );

        public MassCurseSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public void Target(IPoint3D p)
        {
            if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                var map = Caster.Map;

                if (map != null)
                {
                    var eable = map.GetMobilesInRange(new Point3D(p), 2);

                    foreach (var m in eable)
                    {
                        if (Core.AOS && (m == Caster || !SpellHelper.ValidIndirectTarget(Caster, m) || !Caster.CanSee(m) ||
                                         !Caster.CanBeHarmful(m, false)))
                        {
                            continue;
                        }

                        Caster.DoHarmful(m);

                        var length = SpellHelper.GetDuration(Caster, m);
                        SpellHelper.AddStatCurse(Caster, m, StatType.Str, length, false);
                        SpellHelper.AddStatCurse(Caster, m, StatType.Dex, length);
                        SpellHelper.AddStatCurse(Caster, m, StatType.Int, length);

                        m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
                        m.PlaySound(0x1FB);

                        HarmfulSpell(m);
                    }

                    eable.Free();
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
