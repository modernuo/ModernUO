using System;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Seventh
{
    public class GateTravelSpell : MagerySpell, IRecallSpell
    {
        private static readonly SpellInfo _info = new(
            "Gate Travel",
            "Vas Rel Por",
            263,
            9032,
            Reagent.BlackPearl,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh
        );

        private readonly RunebookEntry m_Entry;

        public GateTravelSpell(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public GateTravelSpell(Mobile caster, RunebookEntry entry = null, Item scroll = null) :
            base(caster, scroll, _info) => m_Entry = entry;

        public override SpellCircle Circle => SpellCircle.Seventh;

        public void Effect(Point3D loc, Map map, bool checkMulti)
        {
            if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (map == null || !Core.AOS && Caster.Map != map)
            {
                Caster.SendLocalizedMessage(1005570); // You can not gate to another facet.
            }
            else if (!SpellHelper.CheckTravel(Caster, TravelCheckType.GateFrom, out var failureMessage))
            {
                failureMessage.SendMessageTo(Caster);
            }
            else if (!SpellHelper.CheckTravel(Caster, map, loc, TravelCheckType.GateTo, out failureMessage))
            {
                failureMessage.SendMessageTo(Caster);
            }
            else if (map == Map.Felucca && Caster is PlayerMobile mobile && mobile.Young)
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
                Caster.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            }
            else if (!map.CanSpawnMobile(loc.X, loc.Y, loc.Z))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (checkMulti && SpellHelper.CheckMulti(loc, map))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            // SE restricted stacking gates
            else if (Core.SE && (GateExistsAt(map, loc) || GateExistsAt(Caster.Map, Caster.Location)))
            {
                Caster.SendLocalizedMessage(1071242); // There is already a gate there.
            }
            else if (CheckSequence())
            {
                Caster.SendLocalizedMessage(501024); // You open a magical gate to another location

                Effects.PlaySound(Caster.Location, Caster.Map, 0x20E);

                var firstGate = new InternalItem(loc, map);
                firstGate.MoveToWorld(Caster.Location, Caster.Map);

                Effects.PlaySound(loc, map, 0x20E);

                var secondGate = new InternalItem(Caster.Location, Caster.Map);
                secondGate.MoveToWorld(loc, map);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            if (m_Entry == null)
            {
                Caster.Target = new RecallSpellTarget(this, false);
            }
            else
            {
                Effect(m_Entry.Location, m_Entry.Map, true);
            }
        }

        public override bool CheckCast()
        {
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
                Caster.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }

            if (!SpellHelper.CheckTravel(Caster, TravelCheckType.GateFrom, out var failureMessage))
            {
                failureMessage.SendMessageTo(Caster);
                return false;
            }

            return true;
        }

        private static bool GateExistsAt(Map map, Point3D loc)
        {
            var eable = map.GetItemsInRange(loc, 0);

            foreach (var item in eable)
            {
                if (item is Moongate or PublicMoongate)
                {
                    return true;
                }
            }

            return false;
        }

        [DispellableField]
        private class InternalItem : Moongate
        {
            public InternalItem(Point3D target, Map map) : base(target, map)
            {
                Map = map;

                if (ShowFeluccaWarning && map == Map.Felucca)
                {
                    ItemID = 0xDDA;
                }

                Dispellable = true;

                var t = new InternalTimer(this);
                t.Start();
            }

            public InternalItem(Serial serial) : base(serial)
            {
            }

            public override bool ShowFeluccaWarning => Core.AOS;

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                Delete();
            }

            private class InternalTimer : Timer
            {
                private readonly Item m_Item;

                public InternalTimer(Item item) : base(TimeSpan.FromSeconds(Core.T2A ? 30.0 : 10.0))
                {
                    m_Item = item;
                }

                protected override void OnTick()
                {
                    m_Item.Delete();
                }
            }
        }
    }
}
