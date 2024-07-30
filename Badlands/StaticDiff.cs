// Copyright (C) 2024 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Buffers;
using Server;
using Server.Logging;
using Server.Network;

namespace Badlands;

public static class StaticDiff
{
    private static readonly ILogger logger = LogFactory.GetLogger( typeof( StaticDiff ) );

    public static void Initialize()
    {
        logger.Information( "Initialize StaticDiff" );

        //EventSink.Login += EventSink_Login;
    }

    private static void EventSink_Login( Mobile obj )
    {
        obj.NetState.SendLoginConfirmationPacket();
        obj.NetState.SendMapDefinitionUpdatePacket();
    }

    public static void SendMapDefinitionUpdatePacket( this NetState ns )
    {
        var buffer = stackalloc byte[100].InitializePacket();

        var writer = new SpanWriter( buffer );
        writer.Write( ( byte )0x3F );
        writer.Write( ( ushort )100 );
        writer.Seek( 13, SeekOrigin.Begin );
        writer.Write( ( byte )0x01 );
        writer.Write( ( byte )0x00 );
        writer.Seek( 7, SeekOrigin.Begin );
        writer.Write( 8 );

        writer.Seek( 0, SeekOrigin.End );

        foreach ( var map in Map.Maps )
        {
            if ( map == null || map.MapID == 0x7F )
            {
                continue;
            }

            writer.Write( ( byte )map.MapIndex );
            writer.Write( ( short )map.Width );
            writer.Write( ( short )map.Height );
            writer.Write( ( short )map.Width );
            writer.Write( ( short )map.Height );
        }

        ns.Send( buffer[..100] );
    }

    public static void SendLoginConfirmationPacket( this NetState ns )
    {
        if ( ns.CannotSendPackets() )
        {
            return;
        }

        var buffer = stackalloc byte[43].InitializePacket();

        var writer = new SpanWriter( buffer );
        writer.Write( ( byte )0x3F ); // Packet ID
        writer.Write( ( ushort )43 );
        writer.Seek( 13, SeekOrigin.Begin );
        writer.Write( ( byte )0x02 );
        writer.Write( ( byte )0x00 );
        writer.WriteAsciiNull( "The Crossroads" );
        writer.Seek( 0, SeekOrigin.Begin );

        ns.Send( buffer[..43] );
    }
}
