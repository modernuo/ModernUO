using Server.Network;
using Server.Spells;
using Server.Spells.Mysticism;
using Server.Items;

namespace Server.Gumps;

public sealed class EnchantGump : StaticGump<EnchantGump>
{
    private readonly EnchantSpell _spell;
    private readonly BaseWeapon _weapon;

    public EnchantGump(EnchantSpell spell, BaseWeapon weapon) : base(20, 20)
    {
        _spell = spell;
        _weapon = weapon;
    }

    public override bool Singleton => true;

    public static void DisplayTo(Mobile from, EnchantSpell spell, BaseWeapon weapon)
    {
        if (from == null || from.Deleted || from.NetState == null || spell == null || spell.Caster != from ||
            from.Spell != spell || spell.State != SpellState.Sequencing || weapon == null || weapon.Deleted ||
            weapon.Parent != from)
        {
            return;
        }

        from.CloseGump<EnchantGump>();
        from.SendGump(new EnchantGump(spell, weapon));
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        const int font = 0x07FF;

        builder.AddPage();
        builder.AddBackground(0, 0, 260, 187, 3600);
        builder.AddAlphaRegion(5, 15, 242, 170);
        builder.AddImageTiled(220, 15, 30, 162, 10464);

        builder.AddItem(0, 3, 6882);
        builder.AddItem(-8, 170, 6880);
        builder.AddItem(185, 3, 6883);
        builder.AddItem(192, 170, 6881);

        builder.AddHtmlLocalized(20, 22, 150, 16, 1080133, font, false, false); // Select Enchant

        AddOption(ref builder, 20, 50, 1, 1079705);  // Hit Lightning
        AddOption(ref builder, 20, 75, 2, 1079703);  // Hit Fireball
        AddOption(ref builder, 20, 100, 3, 1079704); // Hit Harm
        AddOption(ref builder, 20, 125, 4, 1079706); // Hit Magic Arrow
        AddOption(ref builder, 20, 150, 5, 1079702); // Hit Dispel
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var attribute = info.ButtonID switch
        {
            1 => AosWeaponAttribute.HitLightning,
            2 => AosWeaponAttribute.HitFireball,
            3 => AosWeaponAttribute.HitHarm,
            4 => AosWeaponAttribute.HitMagicArrow,
            5 => AosWeaponAttribute.HitDispel,
            _ => (AosWeaponAttribute?)null
        };

        if (attribute.HasValue)
        {
            _spell.FinishSelection(_weapon, attribute.Value);
        }
        else
        {
            _spell.CancelSelection();
        }
    }

    private static void AddOption(
        ref StaticGumpBuilder builder,
        int x,
        int y,
        int buttonId,
        int cliloc
    )
    {
        builder.AddButton(x, y, 9702, 9703, buttonId);
        builder.AddHtmlLocalized(x + 25, y, 200, 16, cliloc, 0x07FF, false, false);
    }
}
