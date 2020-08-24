using System;
using System.Buffers;
using Server.Network;

namespace Server.Tests.Network.Packets
{
    public static class AttributeNormalizerUtilities
    {
        public static void WriteAttribute(this Span<byte> data, ref int pos, int cur, int max, bool normalize)
        {
            if (normalize && AttributeNormalizer.Enabled && max != 0)
            {
                int maximum = AttributeNormalizer.Maximum;

                data.Write(ref pos, (ushort)maximum);
                data.Write(ref pos, (ushort)(cur * maximum / max));
                return;
            }

            data.Write(ref pos, (ushort)max);
            data.Write(ref pos, (ushort)cur);
        }

        public static void WriteReverseAttribute(this Span<byte> data, ref int pos, int cur, int max, bool normalize)
        {
            if (normalize && AttributeNormalizer.Enabled && max != 0)
            {
                int maximum = AttributeNormalizer.Maximum;

                data.Write(ref pos, (ushort)(cur * maximum / max));
                data.Write(ref pos, (ushort)maximum);
                return;
            }

            data.Write(ref pos, (ushort)cur);
            data.Write(ref pos, (ushort)max);
        }
    }
}
