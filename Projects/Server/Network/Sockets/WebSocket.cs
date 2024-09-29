using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Server.Network.Sockets
{
    public sealed class WebSocket : ISocket
    {
        private const int MinHeaderSize = 6; // 2 byte header + 4 byte mask
        private const int MaxheaderSize = 14; // 2 byte header + 8 byte long length + 4 byte mask

        private WriteStatus status;
        private Mask mask;
        private int headerLength;
        private int remainingPayloadLength;
        private int payloadMaskIndex;

        public Socket Socket { get; }
        public bool Connected => Socket.Connected;
        public IPEndPoint LocalEndPoint => Socket.LocalEndPoint as IPEndPoint;
        public IPEndPoint RemoteEndPoint => Socket.RemoteEndPoint as IPEndPoint;

        public WebSocket(Socket socket)
        {
            Socket = socket;
        }

        public int Receive(Span<byte> buffer, out bool forceClose)
        {
            forceClose = false;
            int available = Socket.Available;

            if (available < MinHeaderSize)
            {
                if (available == 0)
                {
                    forceClose = true;
                }

                return 0;
            }

            Span<byte> header = stackalloc byte[MaxheaderSize];
            int bytesWritten = 0;

            while (available >= MinHeaderSize)
            {
                int iterationHeaderLength = Math.Min(available, MaxheaderSize);
                Span<byte> iterationHeader = header[..iterationHeaderLength];

                int bytesRead = Socket.Receive(iterationHeader, SocketFlags.Peek);
                if (iterationHeaderLength != bytesRead)
                {
                    throw new Exception("WebSocket header length mismatch");
                }

                int iterationBytesWritten = 0;
                OpCode opCode = (OpCode)(header[0] & 0b0000_1111);

                bool completed = opCode switch
                {
                    OpCode.BinaryFrame => HandleDataFrame(header, available, buffer, out iterationBytesWritten),
                    OpCode.ContinueFrame => HandleDataFrame(header, available, buffer, out iterationBytesWritten),
                    OpCode.Ping => HandlePing(header, available),
                    OpCode.Pong => HandlePong(header, available),
                    OpCode.Close => HandleClose(header, available, out forceClose),
                    _ => throw new NotSupportedException($"WebSocket OpCode {opCode} not supported"),
                };

                bytesWritten += iterationBytesWritten;

                if (!completed)
                {
                    break;
                }

                available -= headerLength + iterationBytesWritten;
                buffer = buffer[iterationBytesWritten..];
                status = WriteStatus.WaitingForHeader;
                payloadMaskIndex = 0;
            }

            return bytesWritten;
        }

        private bool HandlePing(Span<byte> header, int available)
        {
            // packets from client contains always a mask even for Control OpCodes
            int totalLength = (byte)(header[1] & 0b0111_1111) + MinHeaderSize;
            if (totalLength > available)
            {
                return false;
            }

            Span<byte> payload = stackalloc byte[totalLength];

            int bytesRead = Socket.Receive(payload);
            if (bytesRead != totalLength)
            {
                throw new Exception("WebSocket ping length mismatch");
            }

            const byte header1 = 0b1000_0000 | (byte)OpCode.Pong;

            Socket.Send([header1, 0, .. payload[MinHeaderSize..]]);

            return true;
        }

        private bool HandleClose(Span<byte> header, int available, out bool forceClose)
        {
            forceClose = true;

            // packets from client contains always a mask even for Control OpCodes
            int totalLength = (byte)(header[1] & 0b0111_1111) + MinHeaderSize;
            if (totalLength > available)
            {
                return false;
            }

            Span<byte> payload = stackalloc byte[totalLength];

            int bytesRead = Socket.Receive(payload);
            if (bytesRead != totalLength)
            {
                throw new Exception("WebSocket close length mismatch");
            }

            const byte header1 = 0b1000_0000 | (byte)OpCode.Close;

            Socket.Send([header1, 0, .. payload[MinHeaderSize..]]);

            return true;
        }

        private bool HandlePong(Span<byte> header, int available)
        {
            // packets from client contains always a mask even for Control OpCodes
            int totalLength = (byte)(header[1] & 0b0111_1111) + MinHeaderSize;
            if (totalLength > available)
            {
                return false;
            }

            Span<byte> payload = stackalloc byte[totalLength];

            int bytesRead = Socket.Receive(payload);
            if (bytesRead != totalLength)
            {
                throw new Exception("WebSocket pong length mismatch");
            }

            return true;
        }

        private bool HandleDataFrame(Span<byte> header, int available, Span<byte> buffer, out int bytesWritten)
        {
            bytesWritten = 0;

            if (status == WriteStatus.WaitingForHeader)
            {
                if (!ReadDataHeader(header))
                {
                    return false;
                }
            }

            if (status == WriteStatus.WaitingForMask)
            {
                if (!ReadMask(header))
                {
                    return false;
                }
            }

            if (status == WriteStatus.WritingPayload)
            {
                return WritePayload(buffer, available, out bytesWritten);
            }

            return false;
        }

        private bool ReadDataHeader(Span<byte> header)
        {
            bool masked = (header[1] & 0b1000_0000) != 0;
            int length = (byte)(header[1] & 0b0111_1111);

            if (!masked)
            {
                throw new NotSupportedException("WebSocket packet must be masked");
            }

            switch (length)
            {
                case <= 125:
                    {
                        headerLength = MinHeaderSize;
                        break;
                    }
                case 126:
                    {
                        const int lengthSize = 2;

                        if (header.Length < lengthSize + 2)
                        {
                            return false;
                        }

                        length = BinaryPrimitives.ReadUInt16BigEndian(header[2..4]);
                        headerLength = lengthSize + MinHeaderSize;
                        break;
                    }
                default:
                    {
                        const int lengthSize = 8;

                        if (header.Length < lengthSize + 2)
                        {
                            return false;
                        }

                        length = (int)BinaryPrimitives.ReadUInt64BigEndian(header[2..10]);
                        headerLength = lengthSize + MinHeaderSize;
                        break;
                    }
            }

            status = WriteStatus.WaitingForMask;
            remainingPayloadLength = length;

            return true;
        }

        private bool ReadMask(Span<byte> header)
        {
            if (header.Length < headerLength)
            {
                return false;
            }

            mask = new(header[(headerLength - 4)..]);
            status = WriteStatus.WritingPayload;

            int bytesRead = Socket.Receive(header[..headerLength]); // advance socket buffer to payload
            if (bytesRead != headerLength)
            {
                throw new Exception("WebSocket header length mismatch");
            }

            return true;
        }

        private bool WritePayload(Span<byte> buffer, int available, out int bytesWritten)
        {
            int length = Math.Min(remainingPayloadLength, Math.Min(buffer.Length, available - headerLength));
            if (length == 0)
            {
                bytesWritten = 0;
                return false;
            }

            buffer = buffer[..length];

            int bytesRead = Socket.Receive(buffer);
            if (bytesRead != length)
            {
                throw new Exception("WebSocket payload length mismatch");
            }

            ApplyMask(buffer);

            remainingPayloadLength -= bytesRead;
            bytesWritten = bytesRead;

            return remainingPayloadLength == 0;
        }

        private void ApplyMask(Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ mask[payloadMaskIndex++]);

                if (payloadMaskIndex == 4)
                {
                    payloadMaskIndex = 0;
                }
            }
        }

        public int Send(ReadOnlySpan<byte> buffer)
        {
            const byte header1 = 0b1000_0000 | (byte)OpCode.BinaryFrame;

            switch (buffer.Length)
            {
                case <= 125:
                    {
                        Socket.Send([header1, (byte)buffer.Length, .. buffer]);

                        break;
                    }
                case < ushort.MaxValue:
                    {
                        ushort length = (ushort)buffer.Length;

                        Socket.Send([header1, 126, (byte)(length >> 8), (byte)length, .. buffer]);

                        break;
                    }
                default:
                    {
                        int length = buffer.Length;

                        Socket.Send([header1, 127, 0, 0, 0, 0, (byte)(length >> 24), (byte)(length >> 16),
                            (byte)(length >> 8), (byte)length, .. buffer]);

                        break;
                    }
            }

            return buffer.Length;
        }

        public void Close()
        {
            try
            {
                const byte header1 = 0b1000_0000 | (byte)OpCode.Close;
                Socket.Send([header1, 0]);
                Socket.Close();
            }
            catch
            {
                /* ignore already closed sockets */
            }
        }

        private enum OpCode : byte
        {
            ContinueFrame = 0,
            TextFrame = 1,
            BinaryFrame = 2,
            Close = 8,
            Ping = 9,
            Pong = 10,
        }

        [InlineArray(Size)]
        private struct Mask
        {
            public const int Size = 4;

            [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Inline array")]
            private byte element0;

            public Mask(Span<byte> mask)
            {
                mask[..4].CopyTo(this);
            }
        }

        private enum WriteStatus
        {
            WaitingForHeader,
            WaitingForMask,
            WritingPayload,
        }
    }
}
