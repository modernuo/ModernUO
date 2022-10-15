using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
    public class PublicMoongate : Item
    {
        [Constructible]
        public PublicMoongate() : base(0xF6C)
        {
            Movable = false;
            Light = LightType.Circle300;
        }

        public PublicMoongate(Serial serial) : base(serial)
        {
        }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        public override bool HandlesOnMovement => true;

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Player)
            {
                return;
            }

            if (from.InRange(GetWorldLocation(), 1))
            {
                UseGate(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            // Changed so criminals are not blocked by it.
            if (m.Player)
            {
                UseGate(m);
            }

            return true;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m is PlayerMobile)
            {
                if (!Utility.InRange(m.Location, Location, 1) && Utility.InRange(oldLocation, Location, 1))
                {
                    m.CloseGump<MoongateGump>();
                }
            }
        }

        public bool UseGate(Mobile m)
        {
            if (m.Criminal)
            {
                m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }

            if (SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }

            if (m.Spell != null)
            {
                m.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                return false;
            }

            m.CloseGump<MoongateGump>();
            m.SendGump(new MoongateGump(m, this));

            if (!m.Hidden || m.AccessLevel == AccessLevel.Player)
            {
                Effects.PlaySound(m.Location, m.Map, 0x20E);
            }

            return true;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public static void Initialize()
        {
            CommandSystem.Register("MoonGen", AccessLevel.Administrator, MoonGen_OnCommand);
        }

        [Usage("MoonGen"), Description("Generates public moongates. Removes all old moongates.")]
        public static void MoonGen_OnCommand(CommandEventArgs e)
        {
            DeleteAll();

            var count = 0;

            count += MoonGen(PMList.Trammel);
            count += MoonGen(PMList.Felucca);
            count += MoonGen(PMList.Ilshenar);
            count += MoonGen(PMList.Malas);
            count += MoonGen(PMList.Tokuno);

            World.Broadcast(0x35, true, "{0} moongates generated.", count);
        }

        private static void DeleteAll()
        {
            var list = new List<Item>();

            foreach (var item in World.Items.Values)
            {
                if (item is PublicMoongate)
                {
                    list.Add(item);
                }
            }

            foreach (var item in list)
            {
                item.Delete();
            }

            if (list.Count > 0)
            {
                World.Broadcast(0x35, true, "{0} moongates removed.", list.Count);
            }
        }

        private static int MoonGen(PMList list)
        {
            foreach (var entry in list.Entries)
            {
                Item item = new PublicMoongate();

                item.MoveToWorld(entry.Location, list.Map);

                if (entry.Number == 1060642) // Umbra
                {
                    item.Hue = 0x497;
                }
            }

            return list.Entries.Length;
        }
    }

    public class PMEntry
    {
        public PMEntry(Point3D loc, int number)
        {
            Location = loc;
            Number = number;
        }

        public Point3D Location { get; }

        public int Number { get; }
    }

    public class PMList
    {
        public static PMList Trammel =
            new(
                1012000,
                1012012,
                Map.Trammel,
                new[]
                {
                    new PMEntry(new Point3D(4467, 1283, 5), 1012003),   // Moonglow
                    new PMEntry(new Point3D(1336, 1997, 5), 1012004),   // Britain
                    new PMEntry(new Point3D(1499, 3771, 5), 1012005),   // Jhelom
                    new PMEntry(new Point3D(771, 752, 5), 1012006),     // Yew
                    new PMEntry(new Point3D(2701, 692, 5), 1012007),    // Minoc
                    new PMEntry(new Point3D(1828, 2948, -20), 1012008), // Trinsic
                    new PMEntry(new Point3D(643, 2067, 5), 1012009),    // Skara Brae
                    /* Dynamic Z for Magincia to support both old and new maps. */
                    new PMEntry(new Point3D(3563, 2139, Map.Trammel.GetAverageZ(3563, 2139)), 1012010), // (New) Magincia
                    new PMEntry(new Point3D(3450, 2677, 25), 1078098)                                   // New Haven
                }
            );

        public static PMList Felucca =
            new(
                1012001,
                1012013,
                Map.Felucca,
                new[]
                {
                    new PMEntry(new Point3D(4467, 1283, 5), 1012003),   // Moonglow
                    new PMEntry(new Point3D(1336, 1997, 5), 1012004),   // Britain
                    new PMEntry(new Point3D(1499, 3771, 5), 1012005),   // Jhelom
                    new PMEntry(new Point3D(771, 752, 5), 1012006),     // Yew
                    new PMEntry(new Point3D(2701, 692, 5), 1012007),    // Minoc
                    new PMEntry(new Point3D(1828, 2948, -20), 1012008), // Trinsic
                    new PMEntry(new Point3D(643, 2067, 5), 1012009),    // Skara Brae
                    /* Dynamic Z for Magincia to support both old and new maps. */
                    new PMEntry(new Point3D(3563, 2139, Map.Felucca.GetAverageZ(3563, 2139)), 1012010), // (New) Magincia
                    new PMEntry(new Point3D(2711, 2234, 0), 1019001)                                    // Buccaneer's Den
                }
            );

        public static PMList Ilshenar =
            new(
                1012002,
                1012014,
                Map.Ilshenar,
                new[]
                {
                    new PMEntry(new Point3D(1215, 467, -13), 1012015),  // Compassion
                    new PMEntry(new Point3D(722, 1366, -60), 1012016),  // Honesty
                    new PMEntry(new Point3D(744, 724, -28), 1012017),   // Honor
                    new PMEntry(new Point3D(281, 1016, 0), 1012018),    // Humility
                    new PMEntry(new Point3D(987, 1011, -32), 1012019),  // Justice
                    new PMEntry(new Point3D(1174, 1286, -30), 1012020), // Sacrifice
                    new PMEntry(new Point3D(1532, 1340, -3), 1012021),  // Spirituality
                    new PMEntry(new Point3D(528, 216, -45), 1012022),   // Valor
                    new PMEntry(new Point3D(1721, 218, 96), 1019000)    // Chaos
                }
            );

        public static PMList Malas =
            new(
                1060643,
                1062039,
                Map.Malas,
                new[]
                {
                    new PMEntry(new Point3D(1015, 527, -65), 1060641), // Luna
                    new PMEntry(new Point3D(1997, 1386, -85), 1060642) // Umbra
                }
            );

        public static PMList Tokuno =
            new(
                1063258,
                1063415,
                Map.Tokuno,
                new[]
                {
                    new PMEntry(new Point3D(1169, 998, 41), 1063412), // Isamu-Jima
                    new PMEntry(new Point3D(802, 1204, 25), 1063413), // Makoto-Jima
                    new PMEntry(new Point3D(270, 628, 15), 1063414)   // Homare-Jima
                }
            );

        public static PMList TerMur =
            new(
                1113602,
                1113604,
                Map.TerMur,
                new[]
                {
                    new PMEntry(new Point3D(850, 3525, -38), 1113603), // Royal City
                }
            );

        public static PMList TerMurEodon =
            new(
                1113602,
                1113604,
                Map.TerMur,
                new[]
                {
                    new PMEntry(new Point3D(850, 3525, -38), 1113603), // Royal City
                    new PMEntry(new Point3D(719, 1863, 40), 1156262)   // Valley of Eodon
                }
            );

        public static PMList[] NoTrammelLists = { Felucca };
        public static PMList[] T2ALists = { Trammel, Felucca };
        public static PMList[] T2AListsYoung = { Trammel };
        public static PMList[] LBRLists = { Trammel, Felucca, Ilshenar };
        public static PMList[] LBRListsYoung = { Trammel, Ilshenar };
        public static PMList[] AOSLists = { Trammel, Felucca, Ilshenar, Malas };
        public static PMList[] AOSListsYoung = { Trammel, Ilshenar, Malas };
        public static PMList[] SELists = { Trammel, Felucca, Ilshenar, Malas, Tokuno };
        public static PMList[] SEListsYoung = { Trammel, Ilshenar, Malas, Tokuno };
        public static PMList[] SALists = { Trammel, Felucca, Ilshenar, Malas, Tokuno, TerMur };
        public static PMList[] SAListsYoung = { Trammel, Ilshenar, Malas, Tokuno, TerMur };
        public static PMList[] TOLLists = { Trammel, Felucca, Ilshenar, Malas, Tokuno, TerMurEodon };
        public static PMList[] TOLListsYoung = { Trammel, Ilshenar, Malas, Tokuno, TerMurEodon };
        public static PMList[] RedLists = { Felucca };
        public static PMList[] SigilLists = { Felucca };

        public PMList(int number, int selNumber, Map map, PMEntry[] entries)
        {
            Number = number;
            SelNumber = selNumber;
            Map = map;
            Entries = entries;
        }

        public int Number { get; }

        public int SelNumber { get; }

        public Map Map { get; }

        public PMEntry[] Entries { get; }
    }

    public class MoongateGump : Gump
    {
        private PMList[] _lists;
        private Mobile _mobile;
        private Item _moongate;

        public MoongateGump(Mobile mobile, Item moongate) : base(100, 100)
        {
            _mobile = mobile;
            _moongate = moongate;

            PMList[] checkLists;

            if (!mobile.Player)
            {
                checkLists = PMList.SELists;
            }
            else if (Sigil.ExistsOn(mobile))
            {
                checkLists = PMList.SigilLists;
            }
            else if (mobile.Kills >= 5)
            {
                checkLists = PMList.RedLists;
            }
            else
            {
                var flags = mobile.NetState?.Flags ?? ClientFlags.None;
                var young = mobile is PlayerMobile { Young: true };

                checkLists = Core.Expansion switch
                {
                    >= Expansion.TOL when (flags & ClientFlags.TerMur) != 0 =>
                        young ? PMList.TOLListsYoung : PMList.TOLLists,
                    >= Expansion.SA when (flags & ClientFlags.TerMur) != 0 => young ? PMList.SAListsYoung : PMList.SALists,
                    >= Expansion.SE when (flags & ClientFlags.Tokuno) != 0 => young ? PMList.SEListsYoung : PMList.SELists,
                    >= Expansion.AOS when (flags & ClientFlags.Malas) != 0 => young ? PMList.AOSListsYoung : PMList.AOSLists,
                    >= Expansion.LBR when (flags & ClientFlags.Ilshenar) != 0 =>
                        young ? PMList.LBRListsYoung : PMList.LBRLists,
                    >= Expansion.T2A when (flags & ClientFlags.Trammel) != 0 =>
                        young ? PMList.T2AListsYoung : PMList.T2ALists,
                    _ => PMList.NoTrammelLists
                };
            }

            _lists = new PMList[checkLists.Length];

            for (var i = 0; i < _lists.Length; ++i)
            {
                _lists[i] = checkLists[i];
            }

            for (var i = 0; i < _lists.Length; ++i)
            {
                if (_lists[i].Map == mobile.Map)
                {
                    (_lists[i], _lists[0]) = (_lists[0], _lists[i]);
                    break;
                }
            }

            AddPage(0);

            AddBackground(0, 0, 380, 280, 5054);

            AddButton(10, 210, 4005, 4007, 1);
            AddHtmlLocalized(45, 210, 140, 25, 1011036); // OKAY

            AddButton(10, 235, 4005, 4007, 0);
            AddHtmlLocalized(45, 235, 140, 25, 1011012); // CANCEL

            AddHtmlLocalized(5, 5, 200, 20, 1012011); // Pick your destination:

            for (var i = 0; i < checkLists.Length; ++i)
            {
                AddButton(10, 35 + i * 25, 2117, 2118, 0, GumpButtonType.Page, Array.IndexOf(_lists, checkLists[i]) + 1);
                AddHtmlLocalized(30, 35 + i * 25, 150, 20, checkLists[i].Number);
            }

            for (var i = 0; i < _lists.Length; ++i)
            {
                RenderPage(i, Array.IndexOf(checkLists, _lists[i]));
            }
        }

        private void RenderPage(int index, int offset)
        {
            var list = _lists[index];

            AddPage(index + 1);

            AddButton(10, 35 + offset * 25, 2117, 2118, 0, GumpButtonType.Page, index + 1);
            AddHtmlLocalized(30, 35 + offset * 25, 150, 20, list.SelNumber);

            var entries = list.Entries;

            for (var i = 0; i < entries.Length; ++i)
            {
                AddRadio(200, 35 + i * 25, 210, 211, false, index * 100 + i);
                AddHtmlLocalized(225, 35 + i * 25, 150, 20, entries[i].Number);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 0) // Cancel
            {
                return;
            }

            if (_mobile.Deleted || _moongate.Deleted || _mobile.Map == null)
            {
                return;
            }

            var switches = info.Switches;

            if (switches.Length == 0)
            {
                return;
            }

            var switchID = switches[0];
            var listIndex = switchID / 100;
            var listEntry = switchID % 100;

            if (listIndex < 0 || listIndex >= _lists.Length)
            {
                return;
            }

            var list = _lists[listIndex];

            if (listEntry < 0 || listEntry >= list.Entries.Length)
            {
                return;
            }

            var entry = list.Entries[listEntry];

            if (!_mobile.InRange(_moongate.GetWorldLocation(), 1) || _mobile.Map != _moongate.Map)
            {
                _mobile.SendLocalizedMessage(1019002); // You are too far away to use the gate.
            }
            else if (_mobile.Player && _mobile.Kills >= 5 && list.Map != Map.Felucca)
            {
                _mobile.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            }
            else if (Sigil.ExistsOn(_mobile) && list.Map != Faction.Facet)
            {
                _mobile.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            }
            else if (_mobile.Criminal)
            {
                _mobile.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            }
            else if (SpellHelper.CheckCombat(_mobile))
            {
                _mobile.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            }
            else if (_mobile.Spell != null)
            {
                _mobile.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
            }
            else if (_mobile.Map == list.Map && _mobile.InRange(entry.Location, 1))
            {
                _mobile.SendLocalizedMessage(1019003); // You are already there.
            }
            else
            {
                BaseCreature.TeleportPets(_mobile, entry.Location, list.Map);

                _mobile.Combatant = null;
                _mobile.Warmode = false;
                _mobile.Hidden = true;

                _mobile.MoveToWorld(entry.Location, list.Map);

                Effects.PlaySound(entry.Location, list.Map, 0x1FE);
            }
        }
    }
}
