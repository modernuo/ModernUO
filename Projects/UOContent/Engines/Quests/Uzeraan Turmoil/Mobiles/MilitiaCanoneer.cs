using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class MilitiaCanoneer : BaseQuester
{
    private static readonly int[] _cannonFireClilocs = [
        500651, // You're evil, and must die!
        1049098, // I shall make short work of thee.
        1049320, // FIRE!
        1043149 // Thou deservest to die!
    ];

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _active;

    [Constructible]
    public MilitiaCanoneer() : base("the Militia Canoneer") => _active = true;

    public override void InitBody()
    {
        InitStats(100, 125, 25);

        Hue = Race.Human.RandomSkinHue();

        Female = false;
        Body = 0x190;
        Name = NameList.RandomName("male");
    }

    public override void InitOutfit()
    {
        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this, HairHue);

        AddItem(new PlateChest());
        AddItem(new PlateArms());
        AddItem(new PlateGloves());
        AddItem(new PlateLegs());

        var torch = new Torch
        {
            Movable = false
        };

        AddItem(torch);
        torch.Ignite();
    }

    public override bool CanTalkTo(PlayerMobile to) => false;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }

    public override bool IsEnemy(Mobile m)
    {
        while (!m.Player && m is not BaseVendor)
        {
            if (m is BaseCreature bc)
            {
                var master = bc.GetMaster();
                if (master != null)
                {
                    m = master;
                    continue;
                }
            }

            return m.Karma < 0;
        }

        return false;
    }

    public bool WillFire(Cannon cannon, Mobile target)
    {
        if (_active && IsEnemy(target))
        {
            Direction = GetDirectionTo(target);
            Say(_cannonFireClilocs.RandomElement());
            return true;
        }

        return false;
    }
}
