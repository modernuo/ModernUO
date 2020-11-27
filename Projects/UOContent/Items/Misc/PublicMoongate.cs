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
        public static readonly PMList Trammel =
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

        public static readonly PMList Felucca =
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

        public static readonly PMList Ilshenar =
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

        public static readonly PMList Malas =
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

        public static readonly PMList Tokuno =
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

        public static readonly PMList[] UORLists = { Trammel, Felucca };
        public static readonly PMList[] UORListsYoung = { Trammel };
        public static readonly PMList[] LBRLists = { Trammel, Felucca, Ilshenar };
        public static readonly PMList[] LBRListsYoung = { Trammel, Ilshenar };
        public static readonly PMList[] AOSLists = { Trammel, Felucca, Ilshenar, Malas };
        public static readonly PMList[] AOSListsYoung = { Trammel, Ilshenar, Malas };
        public static readonly PMList[] SELists = { Trammel, Felucca, Ilshenar, Malas, Tokuno };
        public static readonly PMList[] SEListsYoung = { Trammel, Ilshenar, Malas, Tokuno };
        public static readonly PMList[] RedLists = { Felucca };
        public static readonly PMList[] SigilLists = { Felucca };

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
        private readonly PMList[] m_Lists;
        private readonly Mobile m_Mobile;
        private readonly Item m_Moongate;

        public MoongateGump(Mobile mobile, Item moongate) : base(100, 100)
        {
            m_Mobile = mobile;
            m_Moongate = moongate;

            PMList[] checkLists;

            if (mobile.Player)
            {
                if (Sigil.ExistsOn(mobile))
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
                    var young = mobile is PlayerMobile playerMobile && playerMobile.Young;

                    if (Core.SE && (flags & ClientFlags.Tokuno) != 0)
                    {
                        checkLists = young ? PMList.SEListsYoung : PMList.SELists;
                    }
                    else if (Core.AOS && (flags & ClientFlags.Malas) != 0)
                    {
                        checkLists = young ? PMList.AOSListsYoung : PMList.AOSLists;
                    }
                    else if ((flags & ClientFlags.Ilshenar) != 0)
                    {
                        checkLists = young ? PMList.LBRListsYoung : PMList.LBRLists;
                    }
                    else
                    {
                        checkLists = young ? PMList.UORListsYoung : PMList.UORLists;
                    }
                }
            }
            else
            {
                checkLists = PMList.SELists;
            }

            m_Lists = new PMList[checkLists.Length];

            for (var i = 0; i < m_Lists.Length; ++i)
            {
                m_Lists[i] = checkLists[i];
            }

            for (var i = 0; i < m_Lists.Length; ++i)
            {
                if (m_Lists[i].Map == mobile.Map)
                {
                    var temp = m_Lists[i];

                    m_Lists[i] = m_Lists[0];
                    m_Lists[0] = temp;

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
                AddButton(10, 35 + i * 25, 2117, 2118, 0, GumpButtonType.Page, Array.IndexOf(m_Lists, checkLists[i]) + 1);
                AddHtmlLocalized(30, 35 + i * 25, 150, 20, checkLists[i].Number);
            }

            for (var i = 0; i < m_Lists.Length; ++i)
            {
                RenderPage(i, Array.IndexOf(checkLists, m_Lists[i]));
            }
        }

        private void RenderPage(int index, int offset)
        {
            var list = m_Lists[index];

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

            if (m_Mobile.Deleted || m_Moongate.Deleted || m_Mobile.Map == null)
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

            if (listIndex < 0 || listIndex >= m_Lists.Length)
            {
                return;
            }

            var list = m_Lists[listIndex];

            if (listEntry < 0 || listEntry >= list.Entries.Length)
            {
                return;
            }

            var entry = list.Entries[listEntry];

            if (!m_Mobile.InRange(m_Moongate.GetWorldLocation(), 1) || m_Mobile.Map != m_Moongate.Map)
            {
                m_Mobile.SendLocalizedMessage(1019002); // You are too far away to use the gate.
            }
            else if (m_Mobile.Player && m_Mobile.Kills >= 5 && list.Map != Map.Felucca)
            {
                m_Mobile.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            }
            else if (Sigil.ExistsOn(m_Mobile) && list.Map != Faction.Facet)
            {
                m_Mobile.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            }
            else if (m_Mobile.Criminal)
            {
                m_Mobile.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            }
            else if (SpellHelper.CheckCombat(m_Mobile))
            {
                m_Mobile.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            }
            else if (m_Mobile.Spell != null)
            {
                m_Mobile.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
            }
            else if (m_Mobile.Map == list.Map && m_Mobile.InRange(entry.Location, 1))
            {
                m_Mobile.SendLocalizedMessage(1019003); // You are already there.
            }
            else
            {
                BaseCreature.TeleportPets(m_Mobile, entry.Location, list.Map);

                m_Mobile.Combatant = null;
                m_Mobile.Warmode = false;
                m_Mobile.Hidden = true;

                m_Mobile.MoveToWorld(entry.Location, list.Map);

                Effects.PlaySound(entry.Location, list.Map, 0x1FE);
            }
        }
    }
}
