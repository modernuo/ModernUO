using Server.Network;

namespace Server.Engines.MLQuests
{
    public sealed class RaceChanger : Packet
    {
        public RaceChanger(bool female, Race targetRace) : base(0xBF)
        {
            EnsureCapacity(7);

            Stream.Write((short)0x2A);
            Stream.Write((byte)(female ? 1 : 0));
            Stream.Write((byte)(targetRace.RaceID + 1));
        }
    }

    public sealed class CloseRaceChanger : Packet
    {
        public CloseRaceChanger() : base(0xBF)
        {
            EnsureCapacity(7);

            Stream.Write((short)0x2A);
            Stream.Write((byte)0);
            Stream.Write((byte)0xFF);
        }
    }
}
