using Server.Prompts;

namespace Server.Network
{
    public sealed class MessageLocalized : Packet
    {
        public MessageLocalized(
            Serial serial, int graphic, MessageType type, int hue, int font, int number, string name, string args
        ) : base(0xC1)
        {
            name ??= "";
            args ??= "";

            if (hue == 0)
            {
                hue = 0x3B2;
            }

            EnsureCapacity(50 + args.Length * 2);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)type);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.Write(number);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteLittleUniNull(args);
        }
    }

    public sealed class MessageLocalizedAffix : Packet
    {
        public MessageLocalizedAffix(
            Serial serial, int graphic, MessageType messageType, int hue, int font, int number,
            string name, AffixType affixType, string affix, string args
        ) : base(0xCC)
        {
            name ??= "";
            affix ??= "";
            args ??= "";

            if (hue == 0)
            {
                hue = 0x3B2;
            }

            EnsureCapacity(52 + affix.Length + args.Length * 2);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)messageType);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.Write(number);
            Stream.Write((byte)affixType);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteAsciiNull(affix);
            Stream.WriteBigUniNull(args);
        }
    }

    public sealed class AsciiMessage : Packet
    {
        public AsciiMessage(
            Serial serial, int graphic, MessageType type, int hue, int font, string name, string text
        ) : base(0x1C)
        {
            name ??= "";
            text ??= "";

            if (hue == 0)
            {
                hue = 0x3B2;
            }

            EnsureCapacity(45 + text.Length);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)type);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteAsciiNull(text);
        }
    }

    public sealed class UnicodeMessage : Packet
    {
        public UnicodeMessage(
            Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name,
            string text
        ) : base(0xAE)
        {
            if (string.IsNullOrEmpty(lang))
            {
                lang = "ENU";
            }

            name ??= "";
            text ??= "";

            if (hue == 0)
            {
                hue = 0x3B2;
            }

            EnsureCapacity(50 + text.Length * 2);

            Stream.Write(serial);
            Stream.Write((short)graphic);
            Stream.Write((byte)type);
            Stream.Write((short)hue);
            Stream.Write((short)font);
            Stream.WriteAsciiFixed(lang, 4);
            Stream.WriteAsciiFixed(name, 30);
            Stream.WriteBigUniNull(text);
        }
    }

    public sealed class FollowMessage : Packet
    {
        public FollowMessage(Serial serial1, Serial serial2) : base(0x15, 9)
        {
            Stream.Write(serial1);
            Stream.Write(serial2);
        }
    }

    public sealed class UnicodePrompt : Packet
    {
        public UnicodePrompt(Prompt prompt) : base(0xC2)
        {
            EnsureCapacity(21);

            Stream.Write(prompt.Serial); // TODO: Does this value even matter?
            Stream.Write(prompt.Serial);
            Stream.Write(0);
            Stream.Write(0);
            Stream.Write((short)0);
        }
    }

    public sealed class ObjectHelpResponse : Packet
    {
        public ObjectHelpResponse(Serial e, string text) : base(0xB7)
        {
            EnsureCapacity(9 + text.Length * 2);

            Stream.Write(e);
            Stream.WriteBigUniNull(text);
        }
    }
}
