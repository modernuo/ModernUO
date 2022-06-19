using ModernUO.Serialization;
using Server.Guilds;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GuildTeleporter : Item
{
    [SerializableField(0)]
    private Item _stone;

    [Constructible]
    public GuildTeleporter(Item stone = null) : base(0x1869)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        _stone = stone;
    }

    public override int LabelNumber => 1041054; // guildstone teleporter

    public override bool DisplayLootType => false;

    public override void OnDoubleClick(Mobile from)
    {
        if (Guild.NewGuildSystem)
        {
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (_stone is not Guildstone gs || gs.Deleted || gs.Guild?.Teleporter != this)
        {
            from.SendLocalizedMessage(501197); // This teleporting object can not determine what guildstone to teleport
            return;
        }

        var house = BaseHouse.FindHouseAt(from);

        if (house == null)
        {
            from.SendLocalizedMessage(501138); // You can only place a guildstone in a house.
        }
        else if (!house.IsOwner(from))
        {
            from.SendLocalizedMessage(501141); // You can only place a guildstone in a house you own!
        }
        else if (house.FindGuildstone() != null)
        {
            from.SendLocalizedMessage(501142); // Only one guildstone may reside in a given house.
        }
        else
        {
            gs.MoveToWorld(from.Location, from.Map);
            Delete();
            gs.Guild.Teleporter = null;
        }
    }
}
