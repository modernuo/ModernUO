namespace Server.Network
{
    public sealed class Swing : Packet
    {
        public Swing(Serial attacker, Serial defender) : base(0x2F, 10)
        {
            Stream.Write((byte)0);
            Stream.Write(attacker);
            Stream.Write(defender);
        }
    }

    public sealed class SetWarMode : Packet
    {
        public SetWarMode(bool mode) : base(0x72, 5)
        {
            Stream.Write(mode);
            Stream.Write((byte)0x00);
            Stream.Write((byte)0x32);
            Stream.Write((byte)0x00);
        }
    }

    public sealed class ChangeCombatant : Packet
    {
        public ChangeCombatant(Serial combatant) : base(0xAA, 5)
        {
            Stream.Write(combatant);
        }
    }
}
