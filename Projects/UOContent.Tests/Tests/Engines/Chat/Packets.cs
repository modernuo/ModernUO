using Server.Network;

namespace Server.Engines.Chat
{
    public sealed class ChatMessagePacket : Packet
    {
        public ChatMessagePacket(string lang, int number, string param1, string param2) : base(0xB2)
        {
            param1 ??= string.Empty;
            param2 ??= string.Empty;

            EnsureCapacity(13 + (param1.Length + param2.Length) * 2);

            Stream.Write((ushort)(number - 20));

            if (lang != null)
            {
                Stream.WriteAsciiFixed(lang, 4);
            }
            else
            {
                Stream.Write(0);
            }

            Stream.WriteBigUniNull(param1);
            Stream.WriteBigUniNull(param2);
        }
    }
}
