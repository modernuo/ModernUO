using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class StaffRobe : BaseSuit
{
    public static int GetStaffRobeHue(AccessLevel accessLevel)
    {
        return accessLevel switch
        {
            AccessLevel.Counselor     => 0x3,
            AccessLevel.GameMaster    => 0x26,
            AccessLevel.Seer          => 0x1D3,
            AccessLevel.Administrator => 0x488,
            AccessLevel.Developer     => 11,
            AccessLevel.Owner         => 0x496,
        };
    }

    public StaffRobe(AccessLevel level) : base(level, GetStaffRobeHue(level), 0x204F)
    {
    }

    public override void OnAccessLevelChanged(AccessLevel oldAccessLevel, AccessLevel accessLevel)
    {
        var oldHue = GetStaffRobeHue(oldAccessLevel);

        if (oldHue == Hue)
        {
            Hue = GetStaffRobeHue(accessLevel);
        }
    }
}
