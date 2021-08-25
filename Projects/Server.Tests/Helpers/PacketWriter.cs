using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace Server.Network
{
    /// <summary>
    ///     Provides functionality for writing primitive binary data.
    /// </summary>
    public class PacketWriter
    {
        private static readonly ConcurrentQueue<PacketWriter> m_Pool = new();

        /// <summary>
        ///     Internal format buffer.
        /// </summary>
        private readonly byte[] m_Buffer = new byte[4];

        private int m_Capacity;

        /// <summary>
        ///     Instantiates a new PacketWriter instance with a given capacity.
        /// </summary>
        /// <param name="capacity">Initial capacity for the internal stream.</param>
        public PacketWriter(int capacity = 32)
        {
            UnderlyingStream = new MemoryStream(capacity);
            m_Capacity = capacity;
        }

        /// <summary>
        ///     Gets the total stream length.
        /// </summary>
        public long Length => UnderlyingStream.Length;

        /// <summary>
        ///     Gets or sets the current stream position.
        /// </summary>
        public long Position
        {
            get => UnderlyingStream.Position;
            set => UnderlyingStream.Position = value;
        }

        /// <summary>
        ///     The internal stream used by this PacketWriter instance.
        /// </summary>
        public MemoryStream UnderlyingStream { get; }

        public static PacketWriter CreateInstance(int capacity = 32)
        {
            if (m_Pool.TryDequeue(out var pw))
            {
                pw.m_Capacity = capacity;
                pw.UnderlyingStream.SetLength(0);
                return pw;
            }

            return new PacketWriter(capacity);
        }

        public static void ReleaseInstance(PacketWriter pw)
        {
            m_Pool.Enqueue(pw);
        }

        /// <summary>
        ///     Writes a 1-byte boolean value to the underlying stream. False is represented by 0, true by 1.
        /// </summary>
        public void Write(bool value)
        {
            UnderlyingStream.WriteByte((byte)(value ? 1 : 0));
        }

        /// <summary>
        ///     Writes a 1-byte unsigned integer value to the underlying stream.
        /// </summary>
        public void Write(byte value)
        {
            UnderlyingStream.WriteByte(value);
        }

        /// <summary>
        ///     Writes a 1-byte signed integer value to the underlying stream.
        /// </summary>
        public void Write(sbyte value)
        {
            UnderlyingStream.WriteByte((byte)value);
        }

        /// <summary>
        ///     Writes a 2-byte signed integer value to the underlying stream.
        /// </summary>
        public void Write(short value)
        {
            m_Buffer[0] = (byte)(value >> 8);
            m_Buffer[1] = (byte)value;

            UnderlyingStream.Write(m_Buffer, 0, 2);
        }

        /// <summary>
        ///     Writes a 2-byte unsigned integer value to the underlying stream.
        /// </summary>
        public void Write(ushort value)
        {
            m_Buffer[0] = (byte)(value >> 8);
            m_Buffer[1] = (byte)value;

            UnderlyingStream.Write(m_Buffer, 0, 2);
        }

        /// <summary>
        ///     Writes a 4-byte signed integer value to the underlying stream.
        /// </summary>
        public void Write(int value)
        {
            m_Buffer[0] = (byte)(value >> 24);
            m_Buffer[1] = (byte)(value >> 16);
            m_Buffer[2] = (byte)(value >> 8);
            m_Buffer[3] = (byte)value;

            UnderlyingStream.Write(m_Buffer, 0, 4);
        }

        /// <summary>
        ///     Writes a 4-byte unsigned integer value to the underlying stream.
        /// </summary>
        public void Write(uint value)
        {
            m_Buffer[0] = (byte)(value >> 24);
            m_Buffer[1] = (byte)(value >> 16);
            m_Buffer[2] = (byte)(value >> 8);
            m_Buffer[3] = (byte)value;

            UnderlyingStream.Write(m_Buffer, 0, 4);
        }

        public void Write(Serial serial) => Write(serial.Value);

        /// <summary>
        ///     Writes a sequence of bytes to the underlying stream
        /// </summary>
        public void Write(byte[] buffer, int offset, int size)
        {
            UnderlyingStream.Write(buffer, offset, size);
        }

        /// <summary>
        ///     Writes a fixed-length ASCII-encoded string value to the underlying stream. To fit (size), the string content is either
        ///     truncated or padded with null characters.
        /// </summary>
        public void WriteAsciiFixed(string value, int size)
        {
            if (value == null)
            {
                Console.WriteLine("Network: Attempted to WriteAsciiFixed() with null value");
                value = string.Empty;
            }

            var length = value.Length;

            UnderlyingStream.SetLength(UnderlyingStream.Length + size);

            if (length >= size)
            {
                UnderlyingStream.Position +=
                    Encoding.ASCII.GetBytes(value, 0, size, UnderlyingStream.GetBuffer(), (int)UnderlyingStream.Position);
            }
            else
            {
                Encoding.ASCII.GetBytes(value, 0, length, UnderlyingStream.GetBuffer(), (int)UnderlyingStream.Position);
                UnderlyingStream.Position += size;
            }
        }

        /// <summary>
        ///     Writes a dynamic-length ASCII-encoded string value to the underlying stream, followed by a 1-byte null character.
        /// </summary>
        public void WriteAsciiNull(string value)
        {
            if (value == null)
            {
                Console.WriteLine("Network: Attempted to WriteAsciiNull() with null value");
                value = string.Empty;
            }

            var length = value.Length;

            UnderlyingStream.SetLength(UnderlyingStream.Length + length + 1);

            Encoding.ASCII.GetBytes(value, 0, length, UnderlyingStream.GetBuffer(), (int)UnderlyingStream.Position);
            UnderlyingStream.Position += length + 1;
        }

        /// <summary>
        ///     Writes a dynamic-length little-endian unicode string value to the underlying stream, followed by a 2-byte null
        ///     character.
        /// </summary>
        public void WriteLittleUniNull(string value)
        {
            if (value == null)
            {
                Console.WriteLine("Network: Attempted to WriteLittleUniNull() with null value");
                value = string.Empty;
            }

            var length = value.Length;

            UnderlyingStream.SetLength(UnderlyingStream.Length + (length + 1) * 2);

            UnderlyingStream.Position +=
                Encoding.Unicode.GetBytes(value, 0, length, UnderlyingStream.GetBuffer(), (int)UnderlyingStream.Position);
            UnderlyingStream.Position += 2;
        }

        /// <summary>
        ///     Writes a fixed-length little-endian unicode string value to the underlying stream. To fit (size), the string content is
        ///     either truncated or padded with null characters.
        /// </summary>
        public void WriteLittleUniFixed(string value, int size)
        {
            if (value == null)
            {
                Console.WriteLine("Network: Attempted to WriteLittleUniFixed() with null value");
                value = string.Empty;
            }

            var length = value.Length;
            size *= 2;

            UnderlyingStream.SetLength(UnderlyingStream.Length + size);

            if (length * 2 >= size)
            {
                UnderlyingStream.Position +=
                    Encoding.Unicode.GetBytes(
                        value,
                        0,
                        size / 2,
                        UnderlyingStream.GetBuffer(),
                        (int)UnderlyingStream.Position
                    );
            }
            else
            {
                Encoding.Unicode.GetBytes(value, 0, length, UnderlyingStream.GetBuffer(), (int)UnderlyingStream.Position);
                UnderlyingStream.Position += size;
            }
        }

        /// <summary>
        ///     Writes a dynamic-length big-endian unicode string value to the underlying stream, followed by a 2-byte null character.
        /// </summary>
        public void WriteBigUniNull(string value)
        {
            if (value == null)
            {
                Console.WriteLine("Network: Attempted to WriteBigUniNull() with null value");
                value = string.Empty;
            }

            var length = value.Length;

            UnderlyingStream.SetLength(UnderlyingStream.Length + (length + 1) * 2);

            UnderlyingStream.Position +=
                Encoding.BigEndianUnicode.GetBytes(
                    value,
                    0,
                    length,
                    UnderlyingStream.GetBuffer(),
                    (int)UnderlyingStream.Position
                );
            UnderlyingStream.Position += 2;
        }

        /// <summary>
        ///     Writes a fixed-length big-endian unicode string value to the underlying stream. To fit (size), the string content is
        ///     either truncated or padded with null characters.
        /// </summary>
        public void WriteBigUniFixed(string value, int size)
        {
            if (value == null)
            {
                Console.WriteLine("Network: Attempted to WriteBigUniFixed() with null value");
                value = string.Empty;
            }

            var length = value.Length;
            size *= 2;

            UnderlyingStream.SetLength(UnderlyingStream.Length + size);

            if (length * 2 >= size)
            {
                UnderlyingStream.Position +=
                    Encoding.BigEndianUnicode.GetBytes(
                        value,
                        0,
                        size / 2,
                        UnderlyingStream.GetBuffer(),
                        (int)UnderlyingStream.Position
                    );
            }
            else
            {
                Encoding.BigEndianUnicode.GetBytes(
                    value,
                    0,
                    length,
                    UnderlyingStream.GetBuffer(),
                    (int)UnderlyingStream.Position
                );
                UnderlyingStream.Position += size;
            }
        }

        /// <summary>
        ///     Fills the stream from the current position up to (capacity) with 0x00's
        /// </summary>
        public void Fill()
        {
            Fill(m_Capacity - UnderlyingStream.Length);
        }

        /// <summary>
        ///     Writes a number of 0x00 byte values to the underlying stream.
        /// </summary>
        public void Fill(long length)
        {
            if (UnderlyingStream.Position == UnderlyingStream.Length)
            {
                UnderlyingStream.SetLength(UnderlyingStream.Length + length);
                UnderlyingStream.Seek(0, SeekOrigin.End);
            }
            else
            {
                UnderlyingStream.Write(new byte[length], 0, (int)length);
            }
        }

        /// <summary>
        ///     Offsets the current position from an origin.
        /// </summary>
        public long Seek(long offset, SeekOrigin origin) => UnderlyingStream.Seek(offset, origin);

        /// <summary>
        ///     Gets the entire stream content as a byte array.
        /// </summary>
        public byte[] ToArray() => UnderlyingStream.ToArray();
    }
}
