using System;
using ModernUO.Serialization;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Seventh
{
    public partial class GateTravelSpell : MagerySpell, IRecallSpell
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

                firstGate.LinkedGate = secondGate;
                secondGate.LinkedGate = firstGate;
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
            foreach (var item in map.GetItemsAt(loc))
            {
                if (item is Moongate or PublicMoongate)
                {
                    return true;
                }
            }

            return false;
        }

        [DispellableField]
        [SerializationGenerator(0)]
        private partial class InternalItem : Moongate
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

            [CommandProperty(AccessLevel.GameMaster)]
            public Moongate LinkedGate { get; set; }

            public override bool ShowFeluccaWarning => Core.AOS;

            public override void UseGate(Mobile m)
            {
                if (LinkedGate?.Deleted != false)
                {
                    m.SendMessage("The other gate no longer exists.");
                    return;
                }

                var target = LinkedGate.Location;
                var targetMap = LinkedGate.TargetMap;

                // TODO: Add boat permissions
                // BaseBoat boat = BaseBoat.FindBoatAt(target, targetMap);
                //
                // if (boat != null && !boat.HasAccess(m))
                // {
                //     m.SendLocalizedMessage(1116617); // You do not have permission to board this ship.
                //     return;
                // }

                Target = target;
                TargetMap = targetMap;

                base.UseGate(m);
            }

            [AfterDeserialization(false)]
            private void AfterDeserialization()
            {
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
