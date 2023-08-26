using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class WanderingHealer : BaseHealer
{
    [Constructible]
    public WanderingHealer()
    {
        Title = "the wandering healer";

        AddItem(new GnarledStaff());

        SetSkill(SkillName.Camping, 80.0, 100.0);
        SetSkill(SkillName.Forensics, 80.0, 100.0);
        SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
    }

    public override bool CanTeach => true;

    public override bool ClickTitle => false; // Do not display title in OnSingleClick

    public override bool CheckTeach(SkillName skill, Mobile from)
    {
        if (!base.CheckTeach(skill, from))
        {
            return false;
        }

        return skill is SkillName.Anatomy or SkillName.Camping or SkillName.Forensics or SkillName.Healing or SkillName.SpiritSpeak;
    }

    public override bool CheckResurrect(Mobile m)
    {
        if (m.Criminal)
        {
            Say(501222); // Thou art a criminal.  I shall not resurrect thee.
            return false;
        }

        if (m.Kills >= 5)
        {
            Say(501223); // Thou'rt not a decent and good person. I shall not resurrect thee.
            return false;
        }

        if (m.Karma < 0)
        {
            Say(501224); // Thou hast strayed from the path of virtue, but thou still deservest a second chance.
        }

        return true;
    }
}
