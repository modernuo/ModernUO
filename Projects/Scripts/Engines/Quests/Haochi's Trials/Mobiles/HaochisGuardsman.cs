using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
  public class HaochisGuardsman : BaseQuester
  {
    [Constructible]
    public HaochisGuardsman() : base("the Guardsman of Daimyo Haochi")
    {
    }

    public HaochisGuardsman(Serial serial) : base(serial)
    {
    }

    public override int TalkNumber => -1;

    public override void InitBody()
    {
      InitStats(100, 100, 25);

      Hue = Race.Human.RandomSkinHue();

      Female = false;
      Body = 0x190;
      Name = NameList.RandomName("male");
    }

    public override void InitOutfit()
    {
      Utility.AssignRandomHair(this);

      AddItem(new LeatherDo());
      AddItem(new LeatherHiroSode());
      AddItem(new SamuraiTabi(Utility.RandomNondyedHue()));

      switch (Utility.Random(3))
      {
        case 0:
          AddItem(new StuddedHaidate());
          break;
        case 1:
          AddItem(new PlateSuneate());
          break;
        default:
          AddItem(new LeatherSuneate());
          break;
      }

      switch (Utility.Random(4))
      {
        case 0:
          AddItem(new DecorativePlateKabuto());
          break;
        case 1:
          AddItem(new ChainHatsuburi());
          break;
        case 2:
          AddItem(new LightPlateJingasa());
          break;
        default:
          AddItem(new LeatherJingasa());
          break;
      }

      var weapon = Utility.Random(3) switch
      {
        0 => (Item)new NoDachi(),
        1 => new Lajatang(),
        _ => new Wakizashi()
      };

      weapon.Movable = false;
      AddItem(weapon);
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}