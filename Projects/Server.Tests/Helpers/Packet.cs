using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using Server.Diagnostics;

namespace Server.Network
{
    public abstract class Packet
    {
        private const int CompressorBufferSize = 0x10000;

        private readonly int m_Length;

        private byte[] m_CompiledBuffer;
        private int m_CompiledLength;
        private State m_State;

        protected Packet(int packetID)
        {
            PacketID = packetID;

            if (Core.Profiling)
            {
                var prof = PacketSendProfile.Acquire(PacketID);
                prof.Increment();
            }
        }

        protected Packet(int packetID, int length)
        {
            PacketID = packetID;
            m_Length = length;

            Stream = PacketWriter.CreateInstance(length); // new PacketWriter( length );
            Stream.Write((byte)packetID);

            if (Core.Profiling)
            {
                var prof = PacketSendProfile.Acquire(PacketID);
                prof.Increment();
            }
        }

        public int PacketID { get; }

        public PacketWriter Stream { get; protected set; }

        public void EnsureCapacity(int length)
        {
            Stream = PacketWriter.CreateInstance(length); // new PacketWriter( length );
            Stream.Write((byte)PacketID);
            Stream.Write((short)0);
        }

        public static Packet SetStatic(Packet p)
        {
            p.SetStatic();
            return p;
        }

        public static Packet Acquire(Packet p)
        {
            p.Acquire();
            return p;
        }

        public static void Release(ref Packet p)
        {
            p?.Release();
            p = null;
        }

        public static void Release(Packet p)
        {
            p?.Release();
        }

        public void SetStatic()
        {
            m_State |= State.Static | State.Acquired;
        }

        public void Acquire()
        {
            m_State |= State.Acquired;
        }

        public void OnSend()
        {
            if ((m_State & (State.Acquired | State.Static)) == 0)
            {
                Free();
            }
        }

        private void Free()
        {
            if (m_CompiledBuffer == null)
            {
                return;
            }

            if ((m_State & State.Buffered) != 0)
            {
                ArrayPool<byte>.Shared.Return(m_CompiledBuffer);
            }

            m_State &= ~(State.Static | State.Acquired | State.Buffered);

            m_CompiledBuffer = null;
        }

        public void Release()
        {
            if ((m_State & State.Acquired) != 0)
            {
                Free();
            }
        }

        private readonly object _object = new();

        public byte[] Compile(bool compress, out int length)
        {
            lock (_object)
            {
                if (m_CompiledBuffer == null)
                {
                    if ((m_State & State.Accessed) == 0)
                    {
                        m_State |= State.Accessed;
                    }
                    else
                    {
                        if ((m_State & State.Warned) == 0)
                        {
                            m_State |= State.Warned;

                            try
                            {
                                using var op = new StreamWriter("net_opt.log", true);
                                op.WriteLine("Redundant compile for packet {0}, use Acquire() and Release()", GetType());
                                op.WriteLine(new StackTrace());
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        m_CompiledBuffer = Array.Empty<byte>();
                        m_CompiledLength = 0;

                        length = m_CompiledLength;
                        return m_CompiledBuffer;
                    }

                    InternalCompile(compress);
                }

                length = m_CompiledLength;
                return m_CompiledBuffer;
            }
        }

        private void InternalCompile(bool compress)
        {
            if (m_Length == 0)
            {
                var streamLen = Stream.Length;

                Stream.Seek(1, SeekOrigin.Begin);
                Stream.Write((ushort)streamLen);
            }
            else if (Stream.Length != m_Length)
            {
                var diff = (int)Stream.Length - m_Length;

                Console.WriteLine(
                    "Packet: 0x{0:X2}: Bad packet length! ({1}{2} bytes)",
                    PacketID,
                    diff >= 0 ? "+" : "",
                    diff
                );
            }

            var ms = Stream.UnderlyingStream;

            m_CompiledBuffer = ms.GetBuffer();
            var length = (int)ms.Length;

            if (compress)
            {
                var compressorBuffer = ArrayPool<byte>.Shared.Rent(CompressorBufferSize);
                var compressedLength = NetworkCompression.Compress(m_CompiledBuffer.AsSpan(0, length), compressorBuffer);

                if (length <= 0)
                {
                    Console.WriteLine(
                        "Warning: Compression buffer overflowed on packet 0x{0:X2} ('{1}') (length={2})",
                        PacketID,
                        GetType().Name,
                        length
                    );
                    using var op = new StreamWriter("compression_overflow.log", true);
                    op.WriteLine(
                        "{0} Warning: Compression buffer overflowed on packet 0x{1:X2} ('{2}') (length={3})",
                        Core.Now,
                        PacketID,
                        GetType().Name,
                        length
                    );
                    op.WriteLine(new StackTrace());

                    ArrayPool<byte>.Shared.Return(compressorBuffer);
                }
                else
                {
                    m_CompiledBuffer = compressorBuffer;
                    m_CompiledLength = compressedLength;
                }
            }
            else
            {
                m_CompiledLength = length;
            }

            if (m_CompiledLength > 0)
            {
                var old = m_CompiledBuffer;

                if ((m_State & State.Static) != 0)
                {
                    m_CompiledBuffer = new byte[m_CompiledLength];
                }
                else
                {
                    // Release it later using Release()
                    m_CompiledBuffer = ArrayPool<byte>.Shared.Rent(m_CompiledLength);
                    m_State |= State.Buffered;
                }

                Buffer.BlockCopy(old, 0, m_CompiledBuffer, 0, m_CompiledLength);

                if (compress)
                {
                    ArrayPool<byte>.Shared.Return(old);
                }
            }

            PacketWriter.ReleaseInstance(Stream);
            Stream = null;
        }

        [Flags]
        private enum State
        {
            Inactive = 0x00,
            Static = 0x01,
            Acquired = 0x02,
            Accessed = 0x04,
            Buffered = 0x08,
            Warned = 0x10
        }
    }
}
