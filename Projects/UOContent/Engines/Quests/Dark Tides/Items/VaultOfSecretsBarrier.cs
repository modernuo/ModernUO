using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class VaultOfSecretsBarrier : Item
{
    [Constructible]
    public VaultOfSecretsBarrier() : base(0x49E)
    {
        Movable = false;
        Visible = false;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        if (m is PlayerMobile pm && pm.Profession == 4)
        {
            m.SendLocalizedMessage(1060188, "", 0x24); // The wicked may not enter!
            return false;
        }

        return base.OnMoveOver(m);
    }
}
