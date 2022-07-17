using System.Collections.Generic;
using Server.Factions;
using Server.Spells.Fifth;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;

namespace Server.Spells.Mysticism
{
    public class StoneFormSpell : MysticSpell
    {
        private static readonly SpellInfo _info = new(
            "Stone Form",
            "In Rel Ylem",
            -1,
            9002,
            Reagent.Bloodmoss,
            Reagent.FertileDirt,
            Reagent.Garlic
        );

        private static readonly Dictionary<Mobile, ResistanceMod[]> _table = new();

        public StoneFormSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        public static void Initialize()
        {
            EventSink.PlayerDeath += OnPlayerDeath;
        }

        public static bool UnderEffect(Mobile m) => _table.ContainsKey(m);

        public override bool CheckCast()
        {
            if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                return false;
            }

            if (!Caster.CanBeginAction<PolymorphSpell>())
            {
                Caster.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
                return false;
            }

            if (AnimalForm.UnderTransformation(Caster))
            {
                Caster.SendLocalizedMessage(1063218); // You cannot use that ability in this form.
                return false;
            }

            if (Caster.Flying)
            {
                Caster.SendLocalizedMessage(1113415); // You cannot use this ability while flying.
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (!Caster.CanBeginAction<PolymorphSpell>())
            {
                Caster.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
            }
            else if (!Caster.CanBeginAction<IncognitoSpell>() || Caster.IsBodyMod && !UnderEffect(Caster))
            {
                Caster.SendLocalizedMessage(1063218); // You cannot use that ability in this form.
            }
            else if (CheckSequence())
            {
                if (UnderEffect(Caster))
                {
                    RemoveEffects(Caster);

                    Caster.PlaySound(0xFA);
                    Caster.Delta(MobileDelta.Resistances);
                }
                else
                {
                    var mount = Caster.Mount;

                    if (mount != null)
                    {
                        mount.Rider = null;
                    }

                    Caster.BodyMod = 0x2C1;
                    Caster.HueMod = 0;

                    var offset = (int)((GetBaseSkill(Caster) + GetDamageSkill(Caster)) / 24.0);

                    ResistanceMod[] mods =
                    {
                        new(ResistanceType.Physical, offset),
                        new(ResistanceType.Fire, offset),
                        new(ResistanceType.Cold, offset),
                        new(ResistanceType.Poison, offset),
                        new(ResistanceType.Energy, offset)
                    };

                    for (var i = 0; i < mods.Length; ++i)
                    {
                        Caster.AddResistanceMod(mods[i]);
                    }

                    _table[Caster] = mods;

                    Caster.PlaySound(0x65A);
                    Caster.Delta(MobileDelta.Resistances);

                    var damageBonus = (int)((GetBaseSkill(Caster) + GetDamageSkill(Caster)) / 12.0);
                    var resistCap = (int)((GetBaseSkill(Caster) + GetDamageSkill(Caster)) / 48.0);

                    BuffInfo.AddBuff(
                        Caster,
                        new BuffInfo(
                            BuffIcon.StoneForm,
                            1080145,
                            1080146,
                            $"-10\t-2\t{offset}\t{resistCap}\t{damageBonus}",
                            false
                        )
                    );
                }
            }

            FinishSequence();
        }

        public static void RemoveEffects(Mobile m)
        {
            if (!_table.Remove(m, out var mods))
            {
                return;
            }

            for (var i = 0; i < mods.Length; ++i)
            {
                m.RemoveResistanceMod(mods[i]);
            }

            m.BodyMod = 0;
            m.HueMod = -1;

            BuffInfo.RemoveBuff(m, BuffIcon.StoneForm);
        }

        private static void OnPlayerDeath(Mobile m)
        {
            RemoveEffects(m);
        }
    }
}
