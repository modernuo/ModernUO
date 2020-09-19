using System.Collections.Generic;
using System.Text;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class ArenasMoongate : Item
    {
        [Constructible]
        public ArenasMoongate() : base(0x1FD4)
        {
            Movable = false;
            Light = LightType.Circle300;
        }

        public ArenasMoongate(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "arena moongate";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            Light = LightType.Circle300;
        }

        public bool UseGate(Mobile from)
        {
            if (DuelContext.CheckCombat(from))
            {
                from.SendMessage(0x22, "You have recently been in combat with another player and cannot use this moongate.");
                return false;
            }

            if (from.Spell != null)
            {
                from.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                return false;
            }

            from.CloseGump<ArenaGump>();
            from.SendGump(new ArenaGump(from, this));

            if (!from.Hidden || from.AccessLevel == AccessLevel.Player)
            {
                Effects.PlaySound(from.Location, from.Map, 0x20E);
            }

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 1))
            {
                UseGate(from);
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
            }
        }

        public override bool OnMoveOver(Mobile m) => !m.Player || UseGate(m);
    }

    public class ArenaGump : Gump
    {
        private readonly List<Arena> m_Arenas;
        private readonly Mobile m_From;
        private readonly ArenasMoongate m_Gate;

        private int m_ColumnX = 12;

        public ArenaGump(Mobile from, ArenasMoongate gate) : base(50, 50)
        {
            m_From = from;
            m_Gate = gate;
            m_Arenas = Arena.Arenas;

            AddPage(0);

            var height = 12 + 20 + m_Arenas.Count * 31 + 24 + 12;

            AddBackground(0, 0, 499 + 40, height, 0x2436);

            var list = m_Arenas;

            for (var i = 1; i < list.Count; i += 2)
            {
                AddImageTiled(12, 32 + i * 31, 475 + 40, 30, 0x2430);
            }

            AddAlphaRegion(10, 10, 479 + 40, height - 20);

            AddColumnHeader(35, null);
            AddColumnHeader(115, "Arena");
            AddColumnHeader(325, "Participants");
            AddColumnHeader(40, "Obs");

            AddButton(499 + 40 - 12 - 63 - 4 - 63, height - 12 - 24, 247, 248, 1);
            AddButton(499 + 40 - 12 - 63, height - 12 - 24, 241, 242, 2);

            for (var i = 0; i < list.Count; ++i)
            {
                var ar = list[i];

                var x = 12;
                var y = 32 + i * 31;

                var color = ar.Players.Count > 0 ? 0xCCFFCC : 0xCCCCCC;

                AddRadio(x + 3, y + 1, 9727, 9730, false, i);
                x += 35;

                AddBorderedText(x + 5, y + 5, 115 - 5, ar.Name ?? "(no name)", color, 0);
                x += 115;

                var sb = new StringBuilder();

                if (ar.Players.Count > 0)
                {
                    var ladder = Ladder.Instance;

                    if (ladder == null)
                    {
                        continue;
                    }

                    LadderEntry p1 = null, p2 = null, p3 = null, p4 = null;

                    for (var j = 0; j < ar.Players.Count; ++j)
                    {
                        var mob = ar.Players[j];
                        var c = ladder.Find(mob);

                        if (p1 == null || c.Index < p1.Index)
                        {
                            p4 = p3;
                            p3 = p2;
                            p2 = p1;
                            p1 = c;
                        }
                        else if (p2 == null || c.Index < p2.Index)
                        {
                            p4 = p3;
                            p3 = p2;
                            p2 = c;
                        }
                        else if (p3 == null || c.Index < p3.Index)
                        {
                            p4 = p3;
                            p3 = c;
                        }
                        else if (p4 == null || c.Index < p4.Index)
                        {
                            p4 = c;
                        }
                    }

                    Append(sb, p1);
                    Append(sb, p2);
                    Append(sb, p3);
                    Append(sb, p4);

                    if (ar.Players.Count > 4)
                    {
                        sb.Append(", ...");
                    }
                }
                else
                {
                    sb.Append("Empty");
                }

                AddBorderedText(x + 5, y + 5, 325 - 5, sb.ToString(), color, 0);
                x += 325;

                AddBorderedText(x, y + 5, 40, Center(ar.Spectators.ToString()), color, 0);
            }
        }

        private void Append(StringBuilder sb, LadderEntry le)
        {
            if (le == null)
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append(le.Mobile.Name);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID != 1)
            {
                return;
            }

            var switches = info.Switches;

            if (switches.Length == 0)
            {
                return;
            }

            var opt = switches[0];

            if (opt < 0 || opt >= m_Arenas.Count)
            {
                return;
            }

            var arena = m_Arenas[opt];

            if (!m_From.InRange(m_Gate.GetWorldLocation(), 1) || m_From.Map != m_Gate.Map)
            {
                m_From.SendLocalizedMessage(1019002); // You are too far away to use the gate.
            }
            else if (DuelContext.CheckCombat(m_From))
            {
                m_From.SendMessage(
                    0x22,
                    "You have recently been in combat with another player and cannot use this moongate."
                );
            }
            else if (m_From.Spell != null)
            {
                m_From.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
            }
            else if (m_From.Map == arena.Facet && arena.Zone.Contains(m_From))
            {
                m_From.SendLocalizedMessage(1019003); // You are already there.
            }
            else
            {
                BaseCreature.TeleportPets(m_From, arena.GateIn, arena.Facet);

                m_From.Combatant = null;
                m_From.Warmode = false;
                m_From.Hidden = true;

                m_From.MoveToWorld(arena.GateIn, arena.Facet);

                Effects.PlaySound(arena.GateIn, arena.Facet, 0x1FE);
            }
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        private void AddBorderedText(int x, int y, int width, string text, int color, int borderColor)
        {
            /*AddColoredText( x - 1, y, width, text, borderColor );
            AddColoredText( x + 1, y, width, text, borderColor );
            AddColoredText( x, y - 1, width, text, borderColor );
            AddColoredText( x, y + 1, width, text, borderColor );*/
            /*AddColoredText( x - 1, y - 1, width, text, borderColor );
            AddColoredText( x + 1, y + 1, width, text, borderColor );*/
            AddColoredText(x, y, width, text, color);
        }

        private void AddColoredText(int x, int y, int width, string text, int color)
        {
            if (color == 0)
            {
                AddHtml(x, y, width, 20, text);
            }
            else
            {
                AddHtml(x, y, width, 20, Color(text, color));
            }
        }

        private void AddColumnHeader(int width, string name)
        {
            AddBackground(m_ColumnX, 12, width, 20, 0x242C);
            AddImageTiled(m_ColumnX + 2, 14, width - 4, 16, 0x2430);

            if (name != null)
            {
                AddBorderedText(m_ColumnX, 13, width, Center(name), 0xFFFFFF, 0);
            }

            m_ColumnX += width;
        }
    }
}
