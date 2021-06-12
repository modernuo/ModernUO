using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server
{
    public interface IPropertyListObject : IEntity
    {
        ObjectPropertyList PropertyList { get; }

        void GetProperties(ObjectPropertyList list);
    }

    public sealed class ObjectPropertyList
    {
        // Each of these are localized to "~1_NOTHING~" which allows the string argument to be used
        private static readonly int[] m_StringNumbers =
        {
            1042971,
            1070722
        };

        private int _hash;
        private int _strings;
        private byte[] _buffer;
        private int _position;

        public ObjectPropertyList(IEntity e)
        {
            Entity = e;
            _buffer = GC.AllocateUninitializedArray<byte>(64);

            var writer = new SpanWriter(_buffer);
            writer.Write((byte)0xD6); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write((ushort)1);
            writer.Write(e.Serial);
            writer.Write((ushort)0);
            _position = writer.Position + 4; // Hash
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
            Header = 0;
            HeaderArgs = null;
        }

        public void Flush()
        {
            Resize(_buffer.Length * 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int amount)
        {
            var newBuffer = GC.AllocateUninitializedArray<byte>(amount);
            _buffer.AsSpan(0, Math.Min(amount, _buffer.Length)).CopyTo(newBuffer);
            _buffer = newBuffer;
        }

        public void Terminate()
        {
            int length = _position + 4;
            if (length != _buffer.Length)
            {
                Resize(length);
            }

            var writer = new SpanWriter(_buffer);
            writer.Seek(_position, SeekOrigin.Begin);
            writer.Write(0);

            writer.Seek(11, SeekOrigin.Begin);
            writer.Write(_hash);
            writer.WritePacketLength();
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

            int strLength = arguments.Length * 2;
            int length = _position + 6 + strLength;
            while (length > _buffer.Length)
            {
                Flush();
            }

            var writer = new SpanWriter(_buffer.AsSpan(_position));
            writer.Write(number);
            writer.Write((ushort)strLength);
            writer.WriteLittleUni(arguments);

            _position += writer.BytesWritten;
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
