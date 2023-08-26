using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class EvilWanderingHealer : BaseHealer
{
    [Constructible]
    public EvilWanderingHealer()
    {
        Title = Core.AOS ? "the priest Of Mondain" : "the evil wandering healer";
        Karma = -10000;

        AddItem(new GnarledStaff());

        SetSkill(SkillName.Camping, 80.0, 100.0);
        SetSkill(SkillName.Forensics, 80.0, 100.0);
        SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
    }

    public override bool CanTeach => true;

    public override bool AlwaysMurderer => true;
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
        if (Core.AOS && m.Criminal)
        {
            Say(501222); // Thou art a criminal.  I shall not resurrect thee.
            return false;
        }

        return true;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomBool())
        {
            c.DropItem(new FragmentOfAMap());
        }
    }
}
