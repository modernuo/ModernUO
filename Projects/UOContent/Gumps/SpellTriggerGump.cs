using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Server.Spells;
using Server.Spells.Mysticism;

namespace Server.Gumps;

public sealed class SpellTriggerGump : DynamicGump
{
    private readonly SpellTriggerSpell _spell;
    private readonly SpellTriggerDefinition[] _definitions;

    internal SpellTriggerGump(SpellTriggerSpell spell, IReadOnlyList<SpellTriggerDefinition> definitions) : base(20, 20)
    {
        _spell = spell;
        _definitions = new SpellTriggerDefinition[definitions.Count];

        for (var i = 0; i < definitions.Count; i++)
        {
            _definitions[i] = definitions[i];
        }
    }

    public override bool Singleton => true;

    public static bool DisplayTo(
        Mobile from,
        SpellTriggerSpell spell,
        IReadOnlyList<SpellTriggerDefinition> definitions
    )
    {
        if (from == null || from.Deleted || from.NetState == null || spell == null || spell.Caster != from ||
            from.Spell != spell || spell.State != SpellState.Sequencing || definitions == null || definitions.Count == 0)
        {
            return false;
        }

        from.CloseGump<SpellTriggerGump>();
        from.SendGump(new SpellTriggerGump(spell, definitions));
        return true;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 520, 404, 0x13BE);
        builder.AddImageTiled(10, 10, 500, 20, 0xA40);
        builder.AddImageTiled(10, 40, 500, 324, 0xA40);
        builder.AddImageTiled(10, 374, 500, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 500, 384);
        builder.AddHtmlLocalized(14, 12, 500, 20, 1080151, 0x7FFF); // <center>Spell Trigger Selection Menu</center>

        builder.AddButton(10, 374, 0xFB1, 0xFB2, 0);
        builder.AddLabel(45, 376, 0x7FFF, "Cancel");

        for (var i = 0; i < _definitions.Length; i++)
        {
            var definition = _definitions[i];
            var row = i / 2;
            var column = i % 2;
            var x = 14 + column * 250;
            var y = 44 + row * 64;

            builder.AddImageTiledButton(x, y, 0x918, 0x919, 100 + i, GumpButtonType.Reply, 0, definition.ItemId, 0, 15, 20);
            builder.AddLabel(x + 84, y, 0x7FFF, definition.Name);
            builder.AddLabel(x + 84, y + 22, 0x7FFF, $"Circle {definition.Rank}");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender != null && sender.Mobile != _spell.Caster)
        {
            return;
        }

        var index = info.ButtonID - 100;

        if (index < 0 || index >= _definitions.Length)
        {
            _spell.CancelSelection();
            return;
        }

        _spell.FinishSelection(_definitions[index].SpellId);
    }
}
