using System;
using System.Buffers;
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
        private byte[] _buffer = new byte[64];
        private int _position;

        public ObjectPropertyList(IEntity e)
        {
            Entity = e;
            Span<byte> buffer = _buffer;
            buffer.Write(ref _position, (byte)0xD6); // Packet ID
            _position += 2; // Length
            buffer.Write(ref _position, (ushort)1);
            buffer.Write(ref _position, e.Serial);
            buffer.Write(ref _position, (ushort)0);
            _position += 4; // Hash
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

            Span<byte> buffer = _buffer;
            buffer.Write(ref _position, 0);
            buffer.Slice(11, 4).Write(_hash);
            buffer.Slice(1, 2).Write((ushort)_position);
        }

        public void AddHash(int val)
        {
            _hash ^= val & 0x3FFFFFF;
            _hash ^= (val >> 26) & 0x3F;
        }

        public void Add(int number, string arguments = null)
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
            if (arguments.Length > 0)
            {
                AddHash(arguments.GetHashCode(StringComparison.Ordinal));
            }

            int strLength = m_Encoding.GetByteCount(arguments);

            int length = _position + 6 + strLength;
            if (length > _buffer.Length)
            {
                Flush();
            }

            Span<byte> buffer = _buffer;
            buffer.Write(ref _position, number);
            buffer.Write(ref _position, (ushort)strLength);
            if (strLength > 0)
            {
                m_Encoding.GetBytes(arguments, buffer.Slice(_position));
                _position += strLength;
            }
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
