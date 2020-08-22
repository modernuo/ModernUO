using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Ninja
{
  public class HiddenFigure : BaseQuester
  {
    public static int[] Messages =
    {
      1063191, // They won�t find me here.
      1063192 // Ah, a quiet hideout.
    };

    [Constructible]
    public HiddenFigure() => Message = Messages.RandomElement();

    public HiddenFigure(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Message { get; set; }

    public override int TalkNumber => -1;

    public override void InitBody()
    {
      InitStats(100, 100, 25);

      Hue = Race.Human.RandomSkinHue();

      Female = Utility.RandomBool();

      if (Female)
      {
        Body = 0x191;
        Name = NameList.RandomName("female");
      }
      else
      {
        Body = 0x190;
        Name = NameList.RandomName("male");
      }
    }

    public override void InitOutfit()
    {
      Utility.AssignRandomHair(this);

      AddItem(new TattsukeHakama(GetRandomHue()));
      AddItem(new Kasa());
      AddItem(new HakamaShita(GetRandomHue()));

      if (Utility.RandomBool())
        AddItem(new Shoes(GetShoeHue()));
      else
        AddItem(new Sandals(GetShoeHue()));
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 3;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
      PrivateOverheadMessage(MessageType.Regular, 0x3B2, Message, player.NetState);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version

      writer.Write(Message);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();

      Message = reader.ReadInt();
    }
  }
}
