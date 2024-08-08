using System.Runtime.CompilerServices;
using Server.Guilds;
using Server.Multis;
using Server.Network;

namespace Server.Gumps;

public interface ISecurable
{
    SecureLevel Level { get; set; }
}

public class SetSecureLevelGump : DynamicGump
{
    private readonly ISecurable _info;
    private readonly BaseHouse _house;

    public override bool Singleton => true;

    public SetSecureLevelGump(ISecurable info, BaseHouse house) : base(50, 50)
    {
        _house = house;
        _info = info;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        var offset = Guild.NewGuildSystem ? 20 : 0;

        builder.AddBackground(0, 0, 220, 160 + offset, 5054);

        builder.AddImageTiled(10, 10, 200, 20, 5124);
        builder.AddImageTiled(10, 40, 200, 20, 5124);
        builder.AddImageTiled(10, 70, 200, 80 + offset, 5124);

        builder.AddAlphaRegion(10, 10, 200, 140);

        builder.AddHtmlLocalized(10, 10, 200, 20, 1061276, 32767); // <CENTER>SET ACCESS</CENTER>
        builder.AddHtmlLocalized(10, 40, 100, 20, 1041474, 32767); // Owner:

        builder.AddLabel(110, 40, 1152, _house.Owner.Name.DefaultIfNullOrEmpty(""));

        builder.AddButton(10, 70, GetFirstID(SecureLevel.Owner), 4007, 1);
        builder.AddHtmlLocalized(45, 70, 150, 20, 1061277, GetColor(SecureLevel.Owner)); // Owner Only

        builder.AddButton(10, 90, GetFirstID(SecureLevel.CoOwners), 4007, 2);
        builder.AddHtmlLocalized(45, 90, 150, 20, 1061278, GetColor(SecureLevel.CoOwners)); // Co-Owners

        builder.AddButton(10, 110, GetFirstID(SecureLevel.Friends), 4007, 3);
        builder.AddHtmlLocalized(45, 110, 150, 20, 1061279, GetColor(SecureLevel.Friends)); // Friends

        var houseOwner = _house.Owner;
        // Only the actual House owner AND guild master can set guild secures
        if (Guild.NewGuildSystem && houseOwner?.Guild is Guild guild && guild.Leader == houseOwner)
        {
            builder.AddButton(10, 130, GetFirstID(SecureLevel.Guild), 4007, 5);
            builder.AddHtmlLocalized(45, 130, 150, 20, 1063455, GetColor(SecureLevel.Guild)); // Guild Members
        }

        builder.AddButton(10, 130 + offset, GetFirstID(SecureLevel.Anyone), 4007, 4);
        builder.AddHtmlLocalized(45, 130 + offset, 150, 20, 1061626, GetColor(SecureLevel.Anyone)); // Anyone
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetColor(SecureLevel level) => _info.Level == level ? 0x7F18 : 0x7FFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetFirstID(SecureLevel level) => _info.Level == level ? 4006 : 4005;

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var level = info.ButtonID switch
        {
            1 => SecureLevel.Owner,
            2 => SecureLevel.CoOwners,
            3 => SecureLevel.Friends,
            4 => SecureLevel.Anyone,
            5 => SecureLevel.Guild,
            _ => _info.Level
        };

        if (_info.Level == level)
        {
            state.Mobile.SendLocalizedMessage(1061281); // Access level unchanged.
        }
        else
        {
            _info.Level = level;
            state.Mobile.SendLocalizedMessage(1061280); // New access level set.
        }
    }
}
