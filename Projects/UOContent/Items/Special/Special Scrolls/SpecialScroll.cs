using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class SpecialScroll : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SkillName _skill;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private double _value;

    public SpecialScroll(SkillName skill, double value) : base(0x14F0)
    {
        LootType = LootType.Cursed;
        Weight = 1.0;

        _skill = skill;
        _value = value;
    }

    public abstract int Message { get; }
    public virtual int Title => 0;
    public abstract string DefaultTitle { get; }

    public virtual string GetNameLocalized() => $"#{AosSkillBonuses.GetLabel(Skill)}";

    public virtual string GetName()
    {
        var index = (int)Skill;
        var table = SkillInfo.Table;

        if (index >= 0 && index < table.Length)
        {
            return table[index].Name.ToLower();
        }

        return "???";
    }

    public virtual bool CanUse(Mobile from)
    {
        if (Deleted)
        {
            return false;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        return true;
    }

    public virtual void Use(Mobile from)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!CanUse(from))
        {
            return;
        }

        from.CloseGump<InternalGump>();
        from.SendGump(new InternalGump(from, this));
    }

    public class InternalGump : Gump
    {
        private readonly Mobile _mobile;
        private readonly SpecialScroll _scroll;

        public InternalGump(Mobile mobile, SpecialScroll scroll) : base(25, 50)
        {
            _mobile = mobile;
            _scroll = scroll;

            AddPage(0);

            AddBackground(25, 10, 420, 200, 5054);

            AddImageTiled(33, 20, 401, 181, 2624);
            AddAlphaRegion(33, 20, 401, 181);

            AddHtmlLocalized(40, 48, 387, 100, _scroll.Message, true, true);

            AddHtmlLocalized(125, 148, 200, 20, 1049478, 0xFFFFFF); // Do you wish to use this scroll?

            AddButton(100, 172, 4005, 4007, 1);
            AddHtmlLocalized(135, 172, 120, 20, 1046362, 0xFFFFFF); // Yes

            AddButton(275, 172, 4005, 4007, 0);
            AddHtmlLocalized(310, 172, 120, 20, 1046363, 0xFFFFFF); // No

            if (_scroll.Title != 0)
            {
                AddHtmlLocalized(40, 20, 260, 20, _scroll.Title, 0xFFFFFF);
            }
            else
            {
                AddHtml(40, 20, 260, 20, _scroll.DefaultTitle);
            }

            var skillLabel = _scroll is StatCapScroll ? 1038019 : AosSkillBonuses.GetLabel(_scroll.Skill);
            AddHtmlLocalized(310, 20, 120, 20, skillLabel, 0xFFFFFF); // Power
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                _scroll.Use(_mobile);
            }
        }
    }
}
