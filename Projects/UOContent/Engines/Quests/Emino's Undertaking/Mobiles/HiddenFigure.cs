using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0)]
public partial class HiddenFigure : BaseQuester
{
    private static int[] _messages =
    {
        1063191, // They won't find me here.
        1063192  // Ah, a quiet hideout.
    };

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _message;

    [Constructible]
    public HiddenFigure() => Message = _messages.RandomElement();

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
        {
            AddItem(new Shoes(GetShoeHue()));
        }
        else
        {
            AddItem(new Sandals(GetShoeHue()));
        }
    }

    public override int GetAutoTalkRange(PlayerMobile pm) => 3;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        PrivateOverheadMessage(MessageType.Regular, 0x3B2, Message, player.NetState);
    }
}
