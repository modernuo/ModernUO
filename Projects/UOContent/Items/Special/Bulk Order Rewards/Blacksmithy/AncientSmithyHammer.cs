using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
[Flippable(0x13E4, 0x13E3)]
public partial class AncientSmithyHammer : BaseTool
{
    private SkillMod _skillMod;

    [Constructible]
    public AncientSmithyHammer(int bonus, int uses = 600) : base(uses, 0x13E4)
    {
        _bonus = bonus;
        Weight = 8.0;
        Layer = Layer.OneHanded;
        Hue = 0x482;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Bonus
    {
        get => _bonus;
        set
        {
            _bonus = value;
            InvalidateProperties();
            this.MarkDirty();

            if (_bonus == 0)
            {
                _skillMod?.Remove();

                _skillMod = null;
            }
            else if (_skillMod == null && Parent is Mobile mobile)
            {
                _skillMod = new DefaultSkillMod(SkillName.Blacksmith, "AncientSmithyHammer", true, _bonus);
                mobile.AddSkillMod(_skillMod);
            }
            else if (_skillMod != null)
            {
                _skillMod.Value = _bonus;
            }
        }
    }

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
    public override int LabelNumber => 1045127; // ancient smithy hammer

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (_bonus != 0 && parent is Mobile mobile)
        {
            _skillMod?.Remove();

            _skillMod = new DefaultSkillMod(SkillName.Blacksmith, "AncientSmithyHammer", true, _bonus);
            mobile.AddSkillMod(_skillMod);
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        _skillMod?.Remove();
        _skillMod = null;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_bonus != 0)
        {
            list.Add(1060451, $"{1042354:#}\t{_bonus}"); // ~1_skillname~ +~2_val~
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_bonus != 0 && Parent is Mobile mobile)
        {
            _skillMod = new DefaultSkillMod(SkillName.Blacksmith, "AncientSmithyHammer", true, _bonus);
            mobile.AddSkillMod(_skillMod);
        }
    }
}
