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
using Server.Targeting;

namespace Badlands.Items.Rewards.Dyes;

[SerializationGenerator( 0 )]
public partial class RandomDarksideDye : Item, IUsesRemaining
{
    private readonly int[] _hueList =
    [
        1910,
        1911,
        1912,
        1914,
        1915,
        1916,
        1917,
        1918,
        1919,
        1920,
        1925,
        1928,
        1930,
        1932,
        1938,
        1939,
        1940,
        1950,
        1954,
        1961,
        1974,
        1976,
        2498
    ];

    [SerializedCommandProperty( AccessLevel.GameMaster )] [SerializableField( 2 )]
    private string _account;

    [SerializableField( 1 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private bool _isAccountBound;

    [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private int _usesRemaining;

    [Constructible]
    public RandomDarksideDye() : base( 0xEFF )
    {
        Weight = 1.0;
        UsesRemaining = 5;
        Hue = _hueList[Utility.Random( _hueList.Length )];
        Name = $"Demise Darkside Dye - {Hue}";
        LootType = LootType.Blessed;
    }

    [Constructible]
    public RandomDarksideDye( IAccount account ) : this()
    {
        _account = account.Username;
        _isAccountBound = true;
    }

    public bool ShowUsesRemaining { get; set; } = true;

    public override void GetProperties( IPropertyList list )
    {
        base.GetProperties( list );

        if ( _isAccountBound )
        {
            list.Add( 1155526 ); // Account Bound
        }

        //if ( LootType == LootType.Blessed )
        //{
        //    list.Add( 1038021 ); // Blessed
        //}

        list.Add( 1060584, _usesRemaining ); // uses remaining: ~1_val~
    }

    [SerializableFieldSaveFlag( 2 )]
    private bool ShouldSerializeAccount() => _isAccountBound;

    public override void OnDoubleClick( Mobile from )
    {
        if ( _isAccountBound && from.Account.Username != _account )
        {
            from.SendLocalizedMessage(
                1071296
            ); /*This item is Account Bound and your character is not bound to it. You cannot use this item.*/

            return;
        }

        if ( IsAccessibleTo( from ) && from.InRange( GetWorldLocation(), 3 ) )
        {
            from.SendLocalizedMessage( 1070929 ); // Select the artifact or enhanced magic item to dye.
            from.BeginTarget( 3, false, TargetFlags.None, InternalCallback );
        }
        else
        {
            from.SendLocalizedMessage( 502436 ); // That is not accessible.
        }
    }

    private void InternalCallback( Mobile from, object targeted )
    {
        if ( Deleted || UsesRemaining <= 0 || !from.InRange( GetWorldLocation(), 3 ) ||
             !IsAccessibleTo( from ) )
        {
            return;
        }

        if ( targeted is not Item i )
        {
            from.SendLocalizedMessage( 1070931 ); // You can only dye artifacts and enhanced magic items with this tub.
        }
        else if ( !from.InRange( i.GetWorldLocation(), 3 ) || !IsAccessibleTo( from ) )
        {
            from.SendLocalizedMessage( 502436 ); // That is not accessible.
        }
        else if ( from.Items.Contains( i ) )
        {
            from.SendLocalizedMessage( 1070930 ); // Can't dye artifacts or enhanced magic items that are being worn.
        }
        else if ( i.IsLockedDown )
        {
            // You may not dye artifacts and enhanced magic items which are locked down.
            from.SendLocalizedMessage( 1070932 );
        }
        else if ( i.QuestItem )
        {
            from.SendLocalizedMessage( 1151836 ); // You may not dye toggled quest items.
        }
        else if ( i is MetalPigmentsOfTokuno )
        {
            from.SendLocalizedMessage( 1042417 ); // You cannot dye that.
        }
        else if ( i is LesserPigmentsOfTokuno )
        {
            from.SendLocalizedMessage( 1042417 ); // You cannot dye that.
        }
        else if ( i is PigmentsOfTokuno )
        {
            from.SendLocalizedMessage( 1042417 ); // You cannot dye that.
        }
        else if ( !IsValidItem( i ) )
        {
            // You can only dye artifacts and enhanced magic items with this tub.	//Yes, it says tub on OSI.  Don't ask me why ;p
            from.SendLocalizedMessage( 1070931 );
        }
        else
        {
            // Notes: on OSI there IS no hue check to see if it's already hued.  and no messages on successful hue either
            i.Hue = Hue;

            if ( --UsesRemaining <= 0 )
            {
                Delete();
            }

            from.PlaySound( 0x23E ); // As per OSI TC1
        }
    }

    private static bool IsValidItem( Item i ) => BasePigmentsOfTokuno.IsValidItem( i ) || i.IsArtifact;
}
