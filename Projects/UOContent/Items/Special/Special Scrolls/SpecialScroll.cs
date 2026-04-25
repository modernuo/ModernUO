using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class SpecialScroll : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SkillName _skill;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private double _value;

    public SpecialScroll(SkillName skill, double value) : base(0x14F0)
    {
        LootType = LootType.Cursed;

        _skill = skill;
        _value = value;
    }

    public override double DefaultWeight => 1.0;

    public abstract int Message { get; }
    public virtual int Title => 0;
    public abstract string DefaultTitle { get; }

    public virtual int SkillLabel => AosSkillBonuses.GetLabel(Skill);

    public virtual string GetSkillName()
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

        SpecialScrollGump.DisplayTo(from, this);
    }

    public class SpecialScrollGump : DynamicGump
    {
        private readonly Mobile _mobile;
        private readonly SpecialScroll _scroll;

        public override bool Singleton => true;

        private SpecialScrollGump(Mobile mobile, SpecialScroll scroll) : base(25, 50)
        {
            _mobile = mobile;
            _scroll = scroll;
        }

        public static void DisplayTo(Mobile from, SpecialScroll scroll)
        {
            if (from?.NetState == null || scroll?.Deleted != false)
            {
                return;
            }

            from.SendGump(new SpecialScrollGump(from, scroll));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(25, 10, 420, 200, 5054);

            builder.AddImageTiled(33, 20, 401, 181, 2624);
            builder.AddAlphaRegion(33, 20, 401, 181);

            builder.AddHtmlLocalized(40, 48, 387, 100, _scroll.Message, true, true);

            builder.AddHtmlLocalized(125, 148, 200, 20, 1049478, 0x7FFF); // Do you wish to use this scroll?

            builder.AddButton(100, 172, 4005, 4007, 1);
            builder.AddHtmlLocalized(135, 172, 120, 20, 1046362, 0x7FFF); // Yes

            builder.AddButton(275, 172, 4005, 4007, 0);
            builder.AddHtmlLocalized(310, 172, 120, 20, 1046363, 0x7FFF); // No

            if (_scroll.Title != 0)
            {
                builder.AddHtmlLocalized(40, 20, 260, 20, _scroll.Title, 0x7FFF);
            }
            else
            {
                builder.AddHtml(40, 20, 260, 20, _scroll.DefaultTitle);
            }

            var skillLabel = _scroll.SkillLabel;

            if (skillLabel > 0)
            {
                builder.AddHtmlLocalized(310, 20, 120, 20, skillLabel, 0x7FFF);
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                _scroll.Use(_mobile);
            }
        }
    }
}
