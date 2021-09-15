using Server.Network;

namespace UOContent.Tests
{
    public sealed class ToggleSpecialAbility : Packet
    {
        public ToggleSpecialAbility(int abilityID, bool active) : base(0xBF)
        {
            EnsureCapacity(7);

            Stream.Write((short)0x25);

            Stream.Write((short)abilityID);
            Stream.Write(active);
        }
    }

    public sealed class ClearWeaponAbility : Packet
    {
        public static readonly Packet Instance = SetStatic(new ClearWeaponAbility());

        public ClearWeaponAbility() : base(0xBF)
        {
            EnsureCapacity(5);

            Stream.Write((short)0x21);
        }
    }
}
