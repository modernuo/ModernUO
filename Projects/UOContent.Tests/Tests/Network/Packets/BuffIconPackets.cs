using System;

namespace Server.Network
{
    public sealed class AddBuffPacket : Packet
    {
        public AddBuffPacket(Serial m, BuffInfo info)
            : this(
                m,
                info.ID,
                info.TitleCliloc,
                info.SecondaryCliloc,
                info.Args,
                info.TimeStart != 0 ?
                    TimeSpan.FromMilliseconds(info.TimeStart + (long)info.TimeLength.TotalMilliseconds - Core.TickCount) :
                    TimeSpan.Zero
            )
        {
        }

        public AddBuffPacket(
            Serial mob, BuffIcon iconID, int titleCliloc, int secondaryCliloc, TextDefinition args,
            TimeSpan length
        )
            : base(0xDF)
        {
            var hasArgs = args != null;

            EnsureCapacity(hasArgs ? 48 + args.ToString().Length * 2 : 44);
            Stream.Write(mob);

            Stream.Write((short)iconID); // ID
            Stream.Write((short)0x1);    // Type 0 for removal. 1 for add 2 for Data

            Stream.Fill(4);

            Stream.Write((short)iconID); // ID
            Stream.Write((short)0x01);   // Type 0 for removal. 1 for add 2 for Data

            Stream.Fill(4);

            Stream.Write((short)Math.Max(length.TotalSeconds, 0)); // Time in seconds

            Stream.Fill(3);
            Stream.Write(titleCliloc);
            Stream.Write(secondaryCliloc);

            if (!hasArgs)
            {
                // m_Stream.Fill( 2 );
                Stream.Fill(10);
            }
            else
            {
                Stream.Fill(4);
                Stream.Write((short)0x1); // Unknown -> Possibly something saying 'hey, I have more data!'?
                Stream.Fill(2);

                // m_Stream.WriteLittleUniNull( "\t#1018280" );
                Stream.WriteLittleUniNull($"\t{args}");

                Stream.Write((short)0x1); // Even more Unknown -> Possibly something saying 'hey, I have more data!'?
                Stream.Fill(2);
            }
        }
    }

    public sealed class RemoveBuffPacket : Packet
    {
        public RemoveBuffPacket(Serial mob, BuffInfo info) : this(mob, info.ID)
        {
        }

        public RemoveBuffPacket(Serial mob, BuffIcon iconID) : base(0xDF)
        {
            EnsureCapacity(13);
            Stream.Write(mob);

            Stream.Write((short)iconID); // ID
            Stream.Write((short)0x0);    // Type 0 for removal. 1 for add 2 for Data

            Stream.Fill(4);
        }
    }
}
