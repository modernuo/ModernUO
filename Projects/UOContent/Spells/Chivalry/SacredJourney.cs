using System;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Chivalry
{
    public class SacredJourneySpell : PaladinSpell, IRecallSpell
    {
        private static readonly SpellInfo _info = new(
            "Sacred Journey",
            "Sanctum Viatas",
            -1,
            9002
        );

        private readonly Runebook m_Book;
        private readonly RunebookEntry m_Entry;

        public SacredJourneySpell(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public SacredJourneySpell(
            Mobile caster, RunebookEntry entry = null, Runebook book = null, Item scroll = null
        ) : base(caster, scroll, _info)
        {
            m_Entry = entry;
            m_Book = book;
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 15.0;
        public override int RequiredMana => 10;
        public override int RequiredTithing => 15;
        public override int MantraNumber => 1060727; // Sanctum Viatas
        public override bool BlocksMovement => false;

        public void Effect(Point3D loc, Map map, bool checkMulti)
        {
            if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (map == null || !Core.AOS && Caster.Map != map)
            {
                Caster.SendLocalizedMessage(1005569); // You can not recall to another facet.
            }
            else if (!SpellHelper.CheckTravel(Caster, TravelCheckType.RecallFrom))
            {
            }
            else if (!SpellHelper.CheckTravel(Caster, map, loc, TravelCheckType.RecallTo))
            {
            }
            else if (map == Map.Felucca && Caster is PlayerMobile { Young: true } mobile)
            {
                mobile.SendLocalizedMessage(1049543); // You decide against traveling to Felucca while you are still young.
            }
            else if (Caster.Kills >= 5 && map != Map.Felucca)
            {
                Caster.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            }
            else if (Caster.Criminal)
            {
                Caster.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            }
            else if (SpellHelper.CheckCombat(Caster))
            {
                Caster.SendLocalizedMessage(1061282); // You cannot use the Sacred Journey ability to flee from combat.
            }
            else if (WeightOverloading.IsOverloaded(Caster))
            {
                Caster.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
            }
            else if (!map.CanSpawnMobile(loc.X, loc.Y, loc.Z))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (checkMulti && SpellHelper.CheckMulti(loc, map))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (m_Book?.CurCharges <= 0)
            {
                Caster.SendLocalizedMessage(502412); // There are no charges left on that item.
            }
            else if (CheckSequence())
            {
                BaseCreature.TeleportPets(Caster, loc, map, true);

                if (m_Book != null)
                {
                    --m_Book.CurCharges;
                }

                Effects.SendLocationParticles(
                    EffectItem.Create(Caster.Location, Caster.Map, EffectItem.DefaultDuration),
                    0,
                    0,
                    0,
                    5033
                );

                Caster.PlaySound(0x1FC);
                Caster.MoveToWorld(loc, map);
                Caster.PlaySound(0x1FC);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            if (m_Entry == null)
            {
                Caster.Target = new RecallSpellTarget(this);
            }
            else
            {
                Effect(m_Entry.Location, m_Entry.Map, true);
            }
        }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                return false;
            }

            if (Caster.Criminal)
            {
                Caster.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }

            if (SpellHelper.CheckCombat(Caster))
            {
                Caster.SendLocalizedMessage(1061282); // You cannot use the Sacred Journey ability to flee from combat.
                return false;
            }

            if (WeightOverloading.IsOverloaded(Caster))
            {
                Caster.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
                return false;
            }

            return SpellHelper.CheckTravel(Caster, TravelCheckType.RecallFrom);
        }
    }
}
