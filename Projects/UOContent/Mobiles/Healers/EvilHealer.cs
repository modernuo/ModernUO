using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class EvilHealer : BaseHealer
{
    [Constructible]
    public EvilHealer()
    {
        Title = "the healer";

        Karma = -10000;

        SetSkill(SkillName.Forensics, 80.0, 100.0);
        SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
        SetSkill(SkillName.Swords, 80.0, 100.0);
    }

    public override bool CanTeach => true;

    public override bool AlwaysMurderer => true;
    public override bool IsActiveVendor => true;

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
        if (Core.AOS && m.Criminal)
        {
            Say(501222); // Thou art a criminal.  I shall not resurrect thee.
            return false;
        }

        return true;
    }
}
