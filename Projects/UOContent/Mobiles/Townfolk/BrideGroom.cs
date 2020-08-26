using Server.Items;

namespace Server.Mobiles
{
  public class BrideGroom : BaseEscortable
  {
    [Constructible]
    public BrideGroom()
    {
      if (Female)
        Title = "the bride";
      else
        Title = "the groom";
    }

    public BrideGroom(Serial serial) : base(serial)
    {
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false; // Do not display 'the groom' when single-clicking

    private static int GetRandomHue()
    {
      return Utility.Random(6) switch
      {
        0 => 0,
        1 => Utility.RandomBlueHue(),
        2 => Utility.RandomGreenHue(),
        3 => Utility.RandomRedHue(),
        4 => Utility.RandomYellowHue(),
        5 => Utility.RandomNeutralHue(),
        _ => 0
      };
    }

    public override void InitOutfit()
    {
      if (Female)
        AddItem(new FancyDress());
      else
        AddItem(new FancyShirt());

      int lowHue = GetRandomHue();

      AddItem(new LongPants(lowHue));

      if (Female)
        AddItem(new Shoes(lowHue));
      else
        AddItem(new Boots(lowHue));

      if (Utility.RandomBool())
        HairItemID = 0x203B;
      else
        HairItemID = 0x203C;

      HairHue = Race.RandomHairHue();

      PackGold(200, 250);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}