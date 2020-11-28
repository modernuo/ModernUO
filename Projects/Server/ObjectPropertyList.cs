using System;
using System.Buffers.Binary;
using System.Text;

namespace Server
{
    public interface IPropertyListObject : IEntity
    {
        ObjectPropertyList PropertyList { get; }

        void GetProperties(ObjectPropertyList list);
    }

    public sealed class ObjectPropertyList
    {
        private static readonly Encoding m_Encoding = Encoding.Unicode;

        // Each of these are localized to "~1_NOTHING~" which allows the string argument to be used
        private static readonly int[] m_StringNumbers =
        {
            1042971,
            1070722
        };

        private int _hash;
        private int _strings;
        private byte[] _buffer = new byte[256];
        private int _position;

        public ObjectPropertyList(IEntity e)
        {
            Entity = e;
            _buffer[0] = 0xD6; // Packet ID
            // Skip Length

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(3, 2), 1);
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(5, 4), e.Serial);
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(9, 2), 0);
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(11, 4), e.Serial);

            _position = 15;
        }

        public IEntity Entity { get; }

        public int Hash => 0x40000000 + _hash;

        public int Header { get; set; }

        public string HeaderArgs { get; set; }

        public static bool Enabled { get; set; }

        public byte[] Buffer => _buffer;

        public void Reset()
        {
            _position = 15;
            _hash = 0;
            _strings = 0;
        }

        public void Add(int number)
        {
            if (number == 0)
            {
                return;
            }

            AddHash(number);

            if (Header == 0)
            {
                Header = number;
                HeaderArgs = "";
            }

            if (_position + 6 > _buffer.Length)
            {
                Flush();
            }

            BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(_position, 4), number);
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position + 4, 2), 0);
            _position += 6;
        }

        public void Flush()
        {
            Resize(_buffer.Length * 2);
        }

        private void Resize(int amount)
        {
            Array.Resize(ref _buffer, amount);
        }

        public void Terminate()
        {
            int length = _position + 4;
            if (length != _buffer.Length)
            {
                Resize(length);
            }

            BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_position, 4), 0);
            BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(11, 4), _hash);
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(1, 2), (ushort)length);
        }

        public void AddHash(int val)
        {
            _hash ^= val & 0x3FFFFFF;
            _hash ^= (val >> 26) & 0x3F;
        }

        public void Add(int number, string arguments)
        {
            if (number == 0)
            {
                return;
            }

            arguments ??= "";

            if (Header == 0)
            {
                Header = number;
                HeaderArgs = arguments;
            }

            AddHash(number);
            AddHash(arguments.GetHashCode(StringComparison.Ordinal));

            int strLength = m_Encoding.GetByteCount(arguments);
            int length = _position + 6 + strLength;
            if (length > _buffer.Length)
            {
                Flush();
            }

            BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(_position, 4), number);
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position + 4, 2), (ushort)strLength);
            m_Encoding.GetBytes(arguments, _buffer.AsSpan(_position + 6));
            _position += strLength;
        }

        public void Add(int number, string format, object arg0)
        {
            Add(number, string.Format(format, arg0));
        }

        public void Add(int number, string format, object arg0, object arg1)
        {
            Add(number, string.Format(format, arg0, arg1));
        }

        public void Add(int number, string format, object arg0, object arg1, object arg2)
        {
            Add(number, string.Format(format, arg0, arg1, arg2));
        }

        public void Add(int number, string format, params object[] args)
        {
            Add(number, string.Format(format, args));
        }

        private int GetStringNumber() => m_StringNumbers[_strings++ % m_StringNumbers.Length];

        public void Add(string text)
        {
            Add(GetStringNumber(), text);
        }

        public void Add(string format, string arg0)
        {
            Add(GetStringNumber(), string.Format(format, arg0));
        }

        public void Add(string format, string arg0, string arg1)
        {
            Add(GetStringNumber(), string.Format(format, arg0, arg1));
        }

        public void Add(string format, string arg0, string arg1, string arg2)
        {
            Add(GetStringNumber(), string.Format(format, arg0, arg1, arg2));
        }

        public void Add(string format, params object[] args)
        {
            Add(GetStringNumber(), string.Format(format, args));
        }
    }
}
