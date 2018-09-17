using Server.Items;
using EDI = Server.Mobiles.EscortDestinationInfo;

namespace Server.Mobiles
{
  public class EscortableMage : BaseEscortable
  {
    [Constructible]
    public EscortableMage()
    {
      Title = "the mage";

      SetSkill(SkillName.EvalInt, 80.0, 100.0);
      SetSkill(SkillName.Inscribe, 80.0, 100.0);
      SetSkill(SkillName.Magery, 80.0, 100.0);
      SetSkill(SkillName.Meditation, 80.0, 100.0);
      SetSkill(SkillName.MagicResist, 80.0, 100.0);
    }

    public EscortableMage(Serial serial) : base(serial)
    {
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false; // Do not display 'the mage' when single-clicking

    private static int GetRandomHue()
    {
      switch (Utility.Random(5))
      {
        default:
        case 0: return Utility.RandomBlueHue();
        case 1: return Utility.RandomGreenHue();
        case 2: return Utility.RandomRedHue();
        case 3: return Utility.RandomYellowHue();
        case 4: return Utility.RandomNeutralHue();
      }
    }

    public override void InitOutfit()
    {
      AddItem(new Robe(GetRandomHue()));

      int lowHue = GetRandomHue();

      AddItem(new ShortPants(lowHue));

      if (Female)
        AddItem(new ThighBoots(lowHue));
      else
        AddItem(new Boots(lowHue));

      Utility.AssignRandomHair(this);

      PackGold(200, 250);
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}