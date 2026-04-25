using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Text;

namespace Server.Engines.ConPVP;

[SerializationGenerator(0, false)]
public partial class ArenasMoongate : Item
{
    [Constructible]
    public ArenasMoongate() : base(0x1FD4)
    {
        Movable = false;
        Light = LightType.Circle300;
    }

    public override string DefaultName => "arena moongate";

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

        ArenaGump.DisplayTo(from, this);

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

public class ArenaGump : DynamicGump
{
    private readonly List<Arena> _arenas;
    private readonly Mobile _from;
    private readonly ArenasMoongate _gate;

    private int _columnX;

    public override bool Singleton => true;

    private ArenaGump(Mobile from, ArenasMoongate gate) : base(50, 50)
    {
        _from = from;
        _gate = gate;
        _arenas = Arena.Arenas;
    }

    public static void DisplayTo(Mobile from, ArenasMoongate gate)
    {
        if (from?.NetState == null || gate == null)
        {
            return;
        }

        from.SendGump(new ArenaGump(from, gate));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        _columnX = 12;

        builder.AddPage();

        var height = 12 + 20 + _arenas.Count * 31 + 24 + 12;

        builder.AddBackground(0, 0, 499 + 40, height, 0x2436);

        var list = _arenas;

        for (var i = 1; i < list.Count; i += 2)
        {
            builder.AddImageTiled(12, 32 + i * 31, 475 + 40, 30, 0x2430);
        }

        builder.AddAlphaRegion(10, 10, 479 + 40, height - 20);

        AddColumnHeader(ref builder, 35, null);
        AddColumnHeader(ref builder, 115, "Arena");
        AddColumnHeader(ref builder, 325, "Participants");
        AddColumnHeader(ref builder, 40, "Obs");

        builder.AddButton(499 + 40 - 12 - 63 - 4 - 63, height - 12 - 24, 247, 248, 1);
        builder.AddButton(499 + 40 - 12 - 63, height - 12 - 24, 241, 242, 2);

        var sb = new ValueStringBuilder(stackalloc char[256]);

        for (var i = 0; i < list.Count; ++i)
        {
            var ar = list[i];

            var x = 12;
            var y = 32 + i * 31;

            var color = ar.Players.Count > 0 ? 0xCCFFCC : 0xCCCCCC;

            builder.AddRadio(x + 3, y + 1, 9727, 9730, false, i);
            x += 35;

            AddBorderedText(ref builder, x + 5, y + 5, 115 - 5, ar.Name ?? "(no name)", color, 0);
            x += 115;

            sb.Reset();

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

                Append(ref sb, p1);
                Append(ref sb, p2);
                Append(ref sb, p3);
                Append(ref sb, p4);

                if (ar.Players.Count > 4)
                {
                    sb.Append(", ...");
                }
            }
            else
            {
                sb.Append("Empty");
            }

            AddBorderedText(ref builder, x + 5, y + 5, 325 - 5, sb.ToString(), color, 0);
            x += 325;

            AddBorderedText(ref builder, x, y + 5, 40, Html.Center($"{ar.Spectators}"), color, 0);
        }

        sb.Dispose();
    }

    private static void Append(ref ValueStringBuilder sb, LadderEntry le)
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

    public override void OnResponse(NetState sender, in RelayInfo info)
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

        if (opt < 0 || opt >= _arenas.Count)
        {
            return;
        }

        var arena = _arenas[opt];

        if (!_from.InRange(_gate.GetWorldLocation(), 1) || _from.Map != _gate.Map)
        {
            _from.SendLocalizedMessage(1019002); // You are too far away to use the gate.
        }
        else if (DuelContext.CheckCombat(_from))
        {
            _from.SendMessage(
                0x22,
                "You have recently been in combat with another player and cannot use this moongate."
            );
        }
        else if (_from.Spell != null)
        {
            _from.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
        }
        else if (_from.Map == arena.Facet && arena.Zone.Contains(_from.Location))
        {
            _from.SendLocalizedMessage(1019003); // You are already there.
        }
        else
        {
            BaseCreature.TeleportPets(_from, arena.GateIn, arena.Facet);

            _from.Combatant = null;
            _from.Warmode = false;
            _from.Hidden = true;

            _from.MoveToWorld(arena.GateIn, arena.Facet);

            Effects.PlaySound(arena.GateIn, arena.Facet, 0x1FE);
        }
    }

    private static void AddBorderedText(ref DynamicGumpBuilder builder, int x, int y, int width, string text, int color, int borderColor)
    {
        AddColoredText(ref builder, x, y, width, text, color);
    }

    private static void AddColoredText(ref DynamicGumpBuilder builder, int x, int y, int width, string text, int color)
    {
        builder.AddHtml(x, y, width, 20, color == 0 ? text : text.Color(color));
    }

    private void AddColumnHeader(ref DynamicGumpBuilder builder, int width, string name)
    {
        builder.AddBackground(_columnX, 12, width, 20, 0x242C);
        builder.AddImageTiled(_columnX + 2, 14, width - 4, 16, 0x2430);

        if (name != null)
        {
            AddBorderedText(ref builder, _columnX, 13, width, name.Center(), 0xFFFFFF, 0);
        }

        _columnX += width;
    }
}
