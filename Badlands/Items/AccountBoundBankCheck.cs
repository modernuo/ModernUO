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

namespace Badlands.Items;

[SerializationGenerator( 0, false )]
public partial class AccountBoundBankCheck : BankCheck
{
    [InvalidateProperties] [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private string _account;


    [Constructible]
    public AccountBoundBankCheck( IAccount account, int worth ) : base( worth ) => _account = account.Username;

    public override void GetProperties( IPropertyList list )
    {
        base.GetProperties( list );

        list.Add( 1155526 ); // Account Bound
    }

    public override void OnDoubleClick( Mobile from )
    {
        if ( !CheckAccess( from ) )
        {
            return;
        }

        base.OnDoubleClick( from );
    }

    public override void OnAdded( IEntity parent )
    {
        if ( parent is BankBox bankBox && bankBox.Owner.Account.Username != _account )
        {
            return;
        }

        base.OnAdded( parent );
    }

    private bool CheckAccess( Mobile from )
    {
        if ( from.Account.Username == _account )
        {
            return true;
        }

        from.SendLocalizedMessage(
            1071296
        ); /*This item is Account Bound and your character is not bound to it. You cannot use this item.*/

        return false;
    }
}
