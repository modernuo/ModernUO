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

using ModernUO.Serialization;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;

namespace Badlands.Items;

public partial class EtherealRottweiler : EtherealMount, IAccountBound
{
    [CommandProperty( AccessLevel.GameMaster )]
    public bool IsAccountBound { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string Account { get; set; }

    [Constructible]
    public EtherealRottweiler() : base( 0xA770, 0x3ED9 )
    {
    }

    public EtherealRottweiler(Serial serial) : base(serial)
    {
    }

    [Constructible]
    public EtherealRottweiler(IAccount account) : this()
    {
        IsAccountBound = true;
        Account = account.Username;
    }

    public override string DefaultName => "an ethereal rottweiler";

    public override void Serialize( IGenericWriter writer )
    {
        base.Serialize( writer );

        writer.Write( 1 );
        writer.Write( IsAccountBound );

        if ( IsAccountBound )
        {
            writer.Write( Account );
        }
    }

    public override void Deserialize( IGenericReader reader )
    {
        base.Deserialize( reader );

        int version = reader.ReadInt();

        if ( version >= 1 )
        {
            IsAccountBound = reader.ReadBool();

            if ( IsAccountBound )
            {
                Account = reader.ReadString();
            }
        }
    }
}
