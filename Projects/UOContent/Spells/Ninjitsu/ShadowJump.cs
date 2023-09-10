using System;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Regions;
using Server.SkillHandlers;
using Server.Targeting;

namespace Server.Spells.Ninjitsu
{
    public class Shadowjump : NinjaSpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Shadowjump",
            null,
            -1,
            9002
        );

        public Shadowjump(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

        public override double RequiredSkill => 50.0;
        public override int RequiredMana => 15;

        public override bool BlockedByAnimalForm => false;

        public void Target(IPoint3D p)
        {
            var orig = p;
            var map = Caster.Map;

            SpellHelper.GetSurfaceTop(ref p);

            var from = Caster.Location;
            var to = new Point3D(p);

            if ((Caster as PlayerMobile)?.IsStealthing != true)
            {
                Caster.SendLocalizedMessage(1063087); // You must be in stealth mode to use this ability.
            }
            else if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (StaminaSystem.IsOverloaded(Caster))
            {
                Caster.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
            }
            else if (!SpellHelper.CheckTravel(Caster, TravelCheckType.TeleportFrom, out var failureMessage))
            {
                failureMessage.SendMessageTo(Caster);
            }
            else if (!SpellHelper.CheckTravel(Caster, map, to, TravelCheckType.TeleportTo, out failureMessage))
            {
                failureMessage.SendMessageTo(Caster);
            }
            else if (map?.CanSpawnMobile(p.X, p.Y, p.Z) != true)
            {
                Caster.SendLocalizedMessage(502831); // Cannot teleport to that spot.
            }
            else if (SpellHelper.CheckMulti(to, map, true, 5))
            {
                Caster.SendLocalizedMessage(502831); // Cannot teleport to that spot.
            }
            else if (Region.Find(to, map).IsPartOf<HouseRegion>())
            {
                Caster.SendLocalizedMessage(502829); // Cannot teleport to that spot.
            }
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, orig);

                var m = Caster;

                m.Location = to;
                m.ProcessDelta();

                Effects.SendLocationParticles(
                    EffectItem.Create(from, m.Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    2023
                );

                m.PlaySound(0x512);

                Stealth.OnUse(m); // stealth check after the shadow jump
            }

            FinishSequence();
        }

        public override bool CheckCast()
        {
            // IsStealthing should be moved to Server.Mobiles
            if ((Caster as PlayerMobile)?.IsStealthing != true)
            {
                Caster.SendLocalizedMessage(1063087); // You must be in stealth mode to use this ability.
                return false;
            }

            return base.CheckCast();
        }

        public override bool CheckDisturb(DisturbType type, bool firstCircle, bool resistable) => false;

        public override void OnCast()
        {
            Caster.SendLocalizedMessage(1063088); // You prepare to perform a Shadowjump.
            Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, 11);
        }
    }
}
