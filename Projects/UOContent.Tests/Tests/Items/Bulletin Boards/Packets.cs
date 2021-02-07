using Server.Items;
using Server.Text;

namespace Server.Network
{
    public class BBDisplayBoard : Packet
    {
        public BBDisplayBoard(BaseBulletinBoard board) : base(0x71)
        {
            EnsureCapacity(38);

            var buffer = (board.BoardName ?? "").GetBytesUtf8();

            Stream.Write((byte)0x00);   // Packet ID
            Stream.Write(board.Serial); // Bulletin board serial

            // Bulletin board name
            if (buffer.Length >= 29)
            {
                Stream.Write(buffer, 0, 29);
                Stream.Write((byte)0);
            }
            else
            {
                Stream.Write(buffer, 0, buffer.Length);
                Stream.Fill(30 - buffer.Length);
            }
        }
    }

    public class BBMessageHeader : Packet
    {
        public BBMessageHeader(BaseBulletinBoard board, BulletinMessage msg) : base(0x71)
        {
            var poster = SafeString(msg.PostedName);
            var subject = SafeString(msg.Subject);
            var time = SafeString(msg.GetTimeAsString());

            EnsureCapacity(22 + poster.Length + subject.Length + time.Length);

            Stream.Write((byte)0x01);   // Packet ID
            Stream.Write(board.Serial); // Bulletin board serial
            Stream.Write(msg.Serial);   // Message serial

            Stream.Write(msg.Thread?.Serial ?? Serial.Zero); // Thread serial--parent

            WriteString(poster);
            WriteString(subject);
            WriteString(time);
        }

        public void WriteString(string v)
        {
            var buffer = v.GetBytesUtf8();
            var len = buffer.Length + 1;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);
            Stream.Write(buffer, 0, len - 1);
            Stream.Write((byte)0);
        }

        public string SafeString(string v) => v ?? string.Empty;
    }

    public class BBMessageContent : Packet
    {
        public BBMessageContent(BaseBulletinBoard board, BulletinMessage msg) : base(0x71)
        {
            var poster = SafeString(msg.PostedName);
            var subject = SafeString(msg.Subject);
            var time = SafeString(msg.GetTimeAsString());

            EnsureCapacity(22 + poster.Length + subject.Length + time.Length);

            Stream.Write((byte)0x02);   // Packet ID
            Stream.Write(board.Serial); // Bulletin board serial
            Stream.Write(msg.Serial);   // Message serial

            WriteString(poster);
            WriteString(subject);
            WriteString(time);

            Stream.Write((short)msg.PostedBody);
            Stream.Write((short)msg.PostedHue);

            var len = msg.PostedEquip.Length;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);

            for (var i = 0; i < len; ++i)
            {
                var eq = msg.PostedEquip[i];

                Stream.Write((short)eq.itemID);
                Stream.Write((short)eq.hue);
            }

            len = msg.Lines.Length;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);

            for (var i = 0; i < len; ++i)
            {
                WriteString(msg.Lines[i], true);
            }
        }

        public void WriteString(string v, bool padding = false)
        {
            var buffer = v.GetBytesUtf8();
            var tail = padding ? 2 : 1;
            var len = buffer.Length + tail;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);
            Stream.Write(buffer, 0, len - tail);

            if (padding)
            {
                Stream.Write((short)0); // padding compensates for a client bug
            }
            else
            {
                Stream.Write((byte)0);
            }
        }

        public string SafeString(string v) => v ?? string.Empty;
    }
}
