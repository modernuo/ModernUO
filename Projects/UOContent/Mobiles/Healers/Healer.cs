using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Healer : BaseHealer
{
    [Constructible]
    public Healer()
    {
        Title = "the healer";

        if (!Core.AOS)
        {
            NameHue = 0x35;
        }

        SetSkill(SkillName.Forensics, 80.0, 100.0);
        SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
        SetSkill(SkillName.Swords, 80.0, 100.0);
    }

    public override bool CanTeach => true;

    public override bool IsActiveVendor => true;
    public override bool IsInvulnerable => true;

    public override bool CheckTeach(SkillName skill, Mobile from)
    {
        if (!base.CheckTeach(skill, from))
        {
            return false;
        }

        return skill is SkillName.Forensics or SkillName.Healing or SkillName.SpiritSpeak or SkillName.Swords;
    }

    public override void InitSBInfo()
    {
        SBInfos.Add(new SBHealer());
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
