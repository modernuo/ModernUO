/***************************************************************************
 *                               Container.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Server.Network;

namespace Server.Items
{
	public delegate void OnItemConsumed( Item item, int amount );
	public delegate int CheckItemGroup( Item a, Item b );

	public delegate void ContainerSnoopHandler( Container cont, Mobile from );

	public class Container : Item
	{
		private static ContainerSnoopHandler m_SnoopHandler;

		public static ContainerSnoopHandler SnoopHandler
		{
			get => m_SnoopHandler;
			set => m_SnoopHandler = value;
		}

		private ContainerData m_ContainerData;

		private int m_DropSound;
		private int m_GumpID;
		private int m_MaxItems;

		private int m_TotalItems;
		private int m_TotalWeight;
		private int m_TotalGold;

		private bool m_LiftOverride;

		internal List<Item> m_Items;

		public ContainerData ContainerData
		{
			get
			{
				if ( m_ContainerData == null )
					UpdateContainerData();

				return m_ContainerData;
			}
			set => m_ContainerData = value;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public override int ItemID
		{
			get => base.ItemID;
			set
			{
				int oldID = ItemID;

				base.ItemID = value;

				if ( ItemID != oldID )
					UpdateContainerData();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int GumpID
		{
			get => ( m_GumpID == -1 ? DefaultGumpID : m_GumpID );
			set => m_GumpID = value;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int DropSound
		{
			get => ( m_DropSound == -1 ? DefaultDropSound : m_DropSound );
			set => m_DropSound = value;
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxItems
		{
			get => ( m_MaxItems == -1 ? DefaultMaxItems : m_MaxItems );
			set{ m_MaxItems = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int MaxWeight
		{
			get
			{
				if ( Parent is Container container && container.MaxWeight == 0 )
				{
					return 0;
				}

				return DefaultMaxWeight;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool LiftOverride
		{
			get => m_LiftOverride;
			set => m_LiftOverride = value;
		}

		public virtual void UpdateContainerData()
		{
			ContainerData = ContainerData.GetData( ItemID );
		}

		public virtual Rectangle2D Bounds => ContainerData.Bounds;

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int DefaultGumpID => ContainerData.GumpID;

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int DefaultDropSound => ContainerData.DropSound;

		public virtual int DefaultMaxItems => m_GlobalMaxItems;
		public virtual int DefaultMaxWeight => m_GlobalMaxWeight;

		public virtual bool IsDecoContainer => !Movable && !IsLockedDown && !IsSecure && Parent == null && !m_LiftOverride;

		public virtual int GetDroppedSound( Item item )
		{
			int dropSound = item.GetDropSound();

			return dropSound != -1 ? dropSound : DropSound;
		}

		public override void OnSnoop( Mobile from )
		{
			m_SnoopHandler?.Invoke( this, from );
		}

		public override bool CheckLift( Mobile from, Item item, ref LRReason reject )
		{
			if ( from.AccessLevel < AccessLevel.GameMaster && IsDecoContainer )
			{
				reject = LRReason.CannotLift;
				return false;
			}

			return base.CheckLift( from, item, ref reject );
		}

		public override bool CheckItemUse( Mobile from, Item item )
		{
			if ( item != this && from.AccessLevel < AccessLevel.GameMaster && IsDecoContainer )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
				return false;
			}

			return base.CheckItemUse( from, item );
		}

		public bool CheckHold( Mobile m, Item item, bool message )
		{
			return CheckHold( m, item, message, true, 0, 0 );
		}

		public bool CheckHold( Mobile m, Item item, bool message, bool checkItems )
		{
			return CheckHold( m, item, message, checkItems, 0, 0 );
		}

		public virtual bool CheckHold( Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight )
		{
			if ( m != null && m.AccessLevel < AccessLevel.GameMaster )
			{
				if ( IsDecoContainer )
				{
					if ( message )
						SendCantStoreMessage( m, item );

					return false;
				}

				int maxItems = MaxItems;

				if ( checkItems && maxItems != 0 && (TotalItems + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems )
				{
					if ( message )
						SendFullItemsMessage( m, item );

					return false;
				}

				if ( MaxWeight != 0 && (TotalWeight + plusWeight + item.TotalWeight + item.PileWeight) > MaxWeight )
				{
					if ( message )
						SendFullWeightMessage( m, item );

					return false;
				}
			}

			object parent = Parent;

			while ( parent != null )
			{
				if ( parent is Container container )
					return container.CheckHold( m, item, message, checkItems, plusItems, plusWeight );

				if ( !(parent is Item parentItem) )
					break;

				parent = parentItem.Parent;
			}

			return true;
		}

		public virtual void SendFullItemsMessage( Mobile to, Item item )
		{
			to.SendMessage( "That container cannot hold more items." );
		}

		public virtual void SendFullWeightMessage( Mobile to, Item item )
		{
			to.SendMessage( "That container cannot hold more weight." );
		}

		public virtual void SendCantStoreMessage( Mobile to, Item item )
		{
			to.SendLocalizedMessage( 500176 ); // That is not your container, you can't store things here.
		}

		public virtual bool OnDragDropInto( Mobile from, Item item, Point3D p )
		{
			if ( !CheckHold( from, item, true, true ) )
				return false;

			item.Location = new Point3D( p.m_X, p.m_Y, 0 );
			AddItem( item );

			from.SendSound( GetDroppedSound( item ), GetWorldLocation() );

			return true;
		}

		private class GroupComparer : IComparer
		{
			private CheckItemGroup m_Grouper;

			public GroupComparer( CheckItemGroup grouper )
			{
				m_Grouper = grouper;
			}

			public int Compare( object x, object y )
			{
				Item a = (Item)x;
				Item b = (Item)y;

				return m_Grouper( a, b );
			}
		}

		#region Consume[...]

		public bool ConsumeTotalGrouped( Type type, int amount, bool recurse, OnItemConsumed callback, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			Item[] typedItems = FindItemsByType( type, recurse );

			List<List<Item>> groups = new List<List<Item>>();
			int idx = 0;

			while ( idx < typedItems.Length )
			{
				Item a = typedItems[idx++];
				List<Item> group = new List<Item>();

				group.Add( a );

				while ( idx < typedItems.Length )
				{
					Item b = typedItems[idx];
					int v = grouper( a, b );

					if ( v == 0 )
						group.Add( b );
					else
						break;

					++idx;
				}

				groups.Add( group );
			}

			Item[][] items = new Item[groups.Count][];
			int[] totals = new int[groups.Count];

			bool hasEnough = false;

			for ( int i = 0; i < groups.Count; ++i )
			{
				items[i] = groups[i].ToArray();
				//items[i] = (Item[])(((ArrayList)groups[i]).ToArray( typeof( Item ) ));

				for ( int j = 0; j < items[i].Length; ++j )
					totals[i] += items[i][j].Amount;

				if ( totals[i] >= amount )
					hasEnough = true;
			}

			if ( !hasEnough )
				return false;

			for ( int i = 0; i < items.Length; ++i )
			{
				if ( totals[i] >= amount )
				{
					int need = amount;

					for ( int j = 0; j < items[i].Length; ++j )
					{
						Item item = items[i][j];

						int theirAmount = item.Amount;

						if ( theirAmount < need )
						{
							callback?.Invoke( item, theirAmount );

							item.Consume( theirAmount );
							need -= theirAmount;
						}
						else
						{
							callback?.Invoke( item, need );

							item.Consume( need );
							break;
						}
					}

					break;
				}
			}

			return true;
		}

		public int ConsumeTotalGrouped( Type[] types, int[] amounts, bool recurse, OnItemConsumed callback, CheckItemGroup grouper )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();
			if ( grouper == null )
				throw new ArgumentNullException();

			Item[][][] items = new Item[types.Length][][];
			int[][] totals = new int[types.Length][];

			for ( int i = 0; i < types.Length; ++i )
			{
				Item[] typedItems = FindItemsByType( types[i], recurse );

				List<List<Item>> groups = new List<List<Item>>();
				int idx = 0;

				while ( idx < typedItems.Length )
				{
					Item a = typedItems[idx++];
					List<Item> group = new List<Item>();

					group.Add( a );

					while ( idx < typedItems.Length )
					{
						Item b = typedItems[idx];
						int v = grouper( a, b );

						if ( v == 0 )
							group.Add( b );
						else
							break;

						++idx;
					}

					groups.Add( group );
				}

				items[i] = new Item[groups.Count][];
				totals[i] = new int[groups.Count];

				bool hasEnough = false;

				for ( int j = 0; j < groups.Count; ++j )
				{
					items[i][j] = groups[j].ToArray();
					//items[i][j] = (Item[])(((ArrayList)groups[j]).ToArray( typeof( Item ) ));

					for ( int k = 0; k < items[i][j].Length; ++k )
						totals[i][j] += items[i][j][k].Amount;

					if ( totals[i][j] >= amounts[i] )
						hasEnough = true;
				}

				if ( !hasEnough )
					return i;
			}

			for ( int i = 0; i < items.Length; ++i )
			{
				for ( int j = 0; j < items[i].Length; ++j )
				{
					if ( totals[i][j] >= amounts[i] )
					{
						int need = amounts[i];

						for ( int k = 0; k < items[i][j].Length; ++k )
						{
							Item item = items[i][j][k];

							int theirAmount = item.Amount;

							if ( theirAmount < need )
							{
								callback?.Invoke( item, theirAmount );

								item.Consume( theirAmount );
								need -= theirAmount;
							}
							else
							{
								callback?.Invoke( item, need );

								item.Consume( need );
								break;
							}
						}

						break;
					}
				}
			}

			return -1;
		}

		public int ConsumeTotalGrouped( Type[][] types, int[] amounts, bool recurse, OnItemConsumed callback, CheckItemGroup grouper )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();
			if ( grouper == null )
				throw new ArgumentNullException();

			Item[][][] items = new Item[types.Length][][];
			int[][] totals = new int[types.Length][];

			for ( int i = 0; i < types.Length; ++i )
			{
				Item[] typedItems = FindItemsByType( types[i], recurse );

				List<List<Item>> groups = new List<List<Item>>();
				int idx = 0;

				while ( idx < typedItems.Length )
				{
					Item a = typedItems[idx++];
					List<Item> group = new List<Item>();

					group.Add( a );

					while ( idx < typedItems.Length )
					{
						Item b = typedItems[idx];
						int v = grouper( a, b );

						if ( v == 0 )
							group.Add( b );
						else
							break;

						++idx;
					}

					groups.Add( group );
				}

				items[i] = new Item[groups.Count][];
				totals[i] = new int[groups.Count];

				bool hasEnough = false;

				for ( int j = 0; j < groups.Count; ++j )
				{
					items[i][j] = groups[j].ToArray();

					for ( int k = 0; k < items[i][j].Length; ++k )
						totals[i][j] += items[i][j][k].Amount;

					if ( totals[i][j] >= amounts[i] )
						hasEnough = true;
				}

				if ( !hasEnough )
					return i;
			}

			for ( int i = 0; i < items.Length; ++i )
			{
				for ( int j = 0; j < items[i].Length; ++j )
				{
					if ( totals[i][j] >= amounts[i] )
					{
						int need = amounts[i];

						for ( int k = 0; k < items[i][j].Length; ++k )
						{
							Item item = items[i][j][k];

							int theirAmount = item.Amount;

							if ( theirAmount < need )
							{
								callback?.Invoke( item, theirAmount );

								item.Consume( theirAmount );
								need -= theirAmount;
							}
							else
							{
								callback?.Invoke( item, need );

								item.Consume( need );
								break;
							}
						}

						break;
					}
				}
			}

			return -1;
		}

		public int ConsumeTotal( Type[][] types, int[] amounts )
		{
			return ConsumeTotal( types, amounts, true, null );
		}

		public int ConsumeTotal( Type[][] types, int[] amounts, bool recurse )
		{
			return ConsumeTotal( types, amounts, recurse, null );
		}

		public int ConsumeTotal( Type[][] types, int[] amounts, bool recurse, OnItemConsumed callback )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();

			Item[][] items = new Item[types.Length][];
			int[] totals = new int[types.Length];

			for ( int i = 0; i < types.Length; ++i )
			{
				items[i] = FindItemsByType( types[i], recurse );

				for ( int j = 0; j < items[i].Length; ++j )
					totals[i] += items[i][j].Amount;

				if ( totals[i] < amounts[i] )
					return i;
			}

			for ( int i = 0; i < types.Length; ++i )
			{
				int need = amounts[i];

				for ( int j = 0; j < items[i].Length; ++j )
				{
					Item item = items[i][j];

					int theirAmount = item.Amount;

					if ( theirAmount < need )
					{
						callback?.Invoke( item, theirAmount );

						item.Consume( theirAmount );
						need -= theirAmount;
					}
					else
					{
						callback?.Invoke( item, need );

						item.Consume( need );
						break;
					}
				}
			}

			return -1;
		}

		public int ConsumeTotal( Type[] types, int[] amounts )
		{
			return ConsumeTotal( types, amounts, true, null );
		}

		public int ConsumeTotal( Type[] types, int[] amounts, bool recurse )
		{
			return ConsumeTotal( types, amounts, recurse, null );
		}

		public int ConsumeTotal( Type[] types, int[] amounts, bool recurse, OnItemConsumed callback )
		{
			if ( types.Length != amounts.Length )
				throw new ArgumentException();

			Item[][] items = new Item[types.Length][];
			int[] totals = new int[types.Length];

			for ( int i = 0; i < types.Length; ++i )
			{
				items[i] = FindItemsByType( types[i], recurse );

				for ( int j = 0; j < items[i].Length; ++j )
					totals[i] += items[i][j].Amount;

				if ( totals[i] < amounts[i] )
					return i;
			}

			for ( int i = 0; i < types.Length; ++i )
			{
				int need = amounts[i];

				for ( int j = 0; j < items[i].Length; ++j )
				{
					Item item = items[i][j];

					int theirAmount = item.Amount;

					if ( theirAmount < need )
					{
						callback?.Invoke( item, theirAmount );

						item.Consume( theirAmount );
						need -= theirAmount;
					}
					else
					{
						callback?.Invoke( item, need );

						item.Consume( need );
						break;
					}
				}
			}

			return -1;
		}

		public bool ConsumeTotal( Type type, int amount )
		{
			return ConsumeTotal( type, amount, true, null );
		}

		public bool ConsumeTotal( Type type, int amount, bool recurse )
		{
			return ConsumeTotal( type, amount, recurse, null );
		}

		public bool ConsumeTotal( Type type, int amount, bool recurse, OnItemConsumed callback )
		{
			Item[] items = FindItemsByType( type, recurse );

			// First pass, compute total
			int total = 0;

			for( int i = 0; i < items.Length; ++i )
				total += items[i].Amount;

			if ( total >= amount )
			{
				// We've enough, so consume it

				int need = amount;

				for( int i = 0; i < items.Length; ++i )
				{
					Item item = items[i];

					int theirAmount = item.Amount;

					if ( theirAmount < need )
					{
						callback?.Invoke( item, theirAmount );

						item.Consume( theirAmount );
						need -= theirAmount;
					}
					else
					{
						callback?.Invoke( item, need );

						item.Consume( need );

						return true;
					}
				}
			}

			return false;
		}

		public int ConsumeUpTo( Type type, int amount )
		{
			return ConsumeUpTo( type, amount, true );
		}

		public int ConsumeUpTo( Type type, int amount, bool recurse )
		{
			int consumed = 0;

			Queue<Item> toDelete = new Queue<Item>();

			RecurseConsumeUpTo( this, type, amount, recurse, ref consumed, toDelete );

			while( toDelete.Count > 0 )
				toDelete.Dequeue().Delete();

			return consumed;
		}

		private static void RecurseConsumeUpTo( Item current, Type type, int amount, bool recurse, ref int consumed, Queue<Item> toDelete )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				List<Item> list = current.Items;

				for( int i = 0; i < list.Count; ++i )
				{
					Item item = list[i];

					if ( type.IsAssignableFrom( item.GetType() ) )
					{
						int need = amount - consumed;
						int theirAmount = item.Amount;

						if ( theirAmount <= need )
						{
							toDelete.Enqueue( item );
							consumed += theirAmount;
						}
						else
						{
							item.Amount -= need;
							consumed += need;

							return;
						}
					}
					else if ( recurse && item is Container )
					{
						RecurseConsumeUpTo( item, type, amount, recurse, ref consumed, toDelete );
					}
				}
			}
		}

		#endregion

		#region Get[BestGroup]Amount
		public int GetBestGroupAmount( Type type, bool recurse, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			int best = 0;

			Item[] typedItems = FindItemsByType( type, recurse );

			List<List<Item>> groups = new List<List<Item>>();
			int idx = 0;

			while( idx < typedItems.Length )
			{
				Item a = typedItems[idx++];
				List<Item> group = new List<Item>();

				group.Add( a );

				while( idx < typedItems.Length )
				{
					Item b = typedItems[idx];
					int v = grouper( a, b );

					if ( v == 0 )
						group.Add( b );
					else
						break;

					++idx;
				}

				groups.Add( group );
			}

			for( int i = 0; i < groups.Count; ++i )
			{
				Item[] items = groups[i].ToArray();

				//Item[] items = (Item[])(((ArrayList)groups[i]).ToArray( typeof( Item ) ));
				int total = 0;

				for( int j = 0; j < items.Length; ++j )
					total += items[j].Amount;

				if ( total >= best )
					best = total;
			}

			return best;
		}

		public int GetBestGroupAmount( Type[] types, bool recurse, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			int best = 0;

			Item[] typedItems = FindItemsByType( types, recurse );

			List<List<Item>> groups = new List<List<Item>>();
			int idx = 0;

			while( idx < typedItems.Length )
			{
				Item a = typedItems[idx++];
				List<Item> group = new List<Item>();

				group.Add( a );

				while( idx < typedItems.Length )
				{
					Item b = typedItems[idx];
					int v = grouper( a, b );

					if ( v == 0 )
						group.Add( b );
					else
						break;

					++idx;
				}

				groups.Add( group );
			}

			for( int j = 0; j < groups.Count; ++j )
			{
				Item[] items = groups[j].ToArray();
				//Item[] items = (Item[])(((ArrayList)groups[j]).ToArray( typeof( Item ) ));
				int total = 0;

				for( int k = 0; k < items.Length; ++k )
					total += items[k].Amount;

				if ( total >= best )
					best = total;
			}

			return best;
		}

		public int GetBestGroupAmount( Type[][] types, bool recurse, CheckItemGroup grouper )
		{
			if ( grouper == null )
				throw new ArgumentNullException();

			int best = 0;

			for( int i = 0; i < types.Length; ++i )
			{
				Item[] typedItems = FindItemsByType( types[i], recurse );

				List<List<Item>> groups = new List<List<Item>>();
				int idx = 0;

				while( idx < typedItems.Length )
				{
					Item a = typedItems[idx++];
					List<Item> group = new List<Item>();

					group.Add( a );

					while( idx < typedItems.Length )
					{
						Item b = typedItems[idx];
						int v = grouper( a, b );

						if ( v == 0 )
							group.Add( b );
						else
							break;

						++idx;
					}

					groups.Add( group );
				}

				for( int j = 0; j < groups.Count; ++j )
				{
					Item[] items = groups[j].ToArray();
					//Item[] items = (Item[])(((ArrayList)groups[j]).ToArray( typeof( Item ) ));
					int total = 0;

					for( int k = 0; k < items.Length; ++k )
						total += items[k].Amount;

					if ( total >= best )
						best = total;
				}
			}

			return best;
		}

		public int GetAmount( Type type )
		{
			return GetAmount( type, true );
		}

		public int GetAmount( Type type, bool recurse )
		{
			Item[] items = FindItemsByType( type, recurse );

			int amount = 0;

			for ( int i = 0; i < items.Length; ++i )
				amount += items[i].Amount;

			return amount;
		}

		public int GetAmount( Type[] types )
		{
			return GetAmount( types, true );
		}

		public int GetAmount( Type[] types, bool recurse )
		{
			Item[] items = FindItemsByType( types, recurse );

			int amount = 0;

			for ( int i = 0; i < items.Length; ++i )
				amount += items[i].Amount;

			return amount;
		}
		#endregion

		private static List<Item> m_FindItemsList = new List<Item>();

		#region Non-Generic FindItem[s] by Type
		public Item[] FindItemsByType( Type type )
		{
			return FindItemsByType( type, true );
		}

		public Item[] FindItemsByType( Type type, bool recurse )
		{
			if ( m_FindItemsList.Count > 0 )
				m_FindItemsList.Clear();

			RecurseFindItemsByType( this, type, recurse, m_FindItemsList );

			return m_FindItemsList.ToArray();
		}

		private static void RecurseFindItemsByType( Item current, Type type, bool recurse, List<Item> list )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				List<Item> items = current.Items;

				for ( int i = 0; i < items.Count; ++i )
				{
					Item item = items[i];

					if ( type.IsAssignableFrom( item.GetType() ) )// item.GetType().IsAssignableFrom( type ) )
						list.Add( item );

					if ( recurse && item is Container )
						RecurseFindItemsByType( item, type, recurse, list );
				}
			}
		}

		public Item[] FindItemsByType( Type[] types )
		{
			return FindItemsByType( types, true );
		}

		public Item[] FindItemsByType( Type[] types, bool recurse )
		{
			if ( m_FindItemsList.Count > 0 )
				m_FindItemsList.Clear();

			RecurseFindItemsByType( this, types, recurse, m_FindItemsList );

			return m_FindItemsList.ToArray();
		}

		private static void RecurseFindItemsByType( Item current, Type[] types, bool recurse, List<Item> list )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				List<Item> items = current.Items;

				for( int i = 0; i < items.Count; ++i )
				{
					Item item = items[i];

					if ( InTypeList( item, types ) )
						list.Add( item );

					if ( recurse && item is Container )
						RecurseFindItemsByType( item, types, recurse, list );
				}
			}
		}

		public Item FindItemByType( Type type )
		{
			return FindItemByType( type, true );
		}

		public Item FindItemByType( Type type, bool recurse )
		{
			return RecurseFindItemByType( this, type, recurse );
		}

		private static Item RecurseFindItemByType( Item current, Type type, bool recurse )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				List<Item> list = current.Items;

				for( int i = 0; i < list.Count; ++i )
				{
					Item item = list[i];

					if ( type.IsAssignableFrom( item.GetType() ) )
					{
						return item;
					}

					if ( recurse && item is Container )
					{
						Item check = RecurseFindItemByType( item, type, recurse );

						if ( check != null )
							return check;
					}
				}
			}

			return null;
		}

		public Item FindItemByType( Type[] types )
		{
			return FindItemByType( types, true );
		}

		public Item FindItemByType( Type[] types, bool recurse )
		{
			return RecurseFindItemByType( this, types, recurse );
		}

		private static Item RecurseFindItemByType( Item current, Type[] types, bool recurse )
		{
			if ( current != null && current.Items.Count > 0 )
			{
				List<Item> list = current.Items;

				for( int i = 0; i < list.Count; ++i )
				{
					Item item = list[i];

					if ( InTypeList( item, types ) )
					{
						return item;
					}

					if ( recurse && item is Container )
					{
						Item check = RecurseFindItemByType( item, types, recurse );

						if ( check != null )
							return check;
					}
				}
			}

			return null;
		}

		#endregion

		#region Generic FindItem[s] by Type
		public List<T> FindItemsByType<T>() where T : Item
		{
			return FindItemsByType<T>( true, null );
		}

		public List<T> FindItemsByType<T>( bool recurse ) where T : Item
		{
			return FindItemsByType<T>( recurse, null );
		}

		public List<T> FindItemsByType<T>( Predicate<T> predicate ) where T : Item
		{
			return FindItemsByType<T>( true, predicate );
		}

		public List<T> FindItemsByType<T>( bool recurse, Predicate<T> predicate ) where T : Item
		{
			if ( m_FindItemsList.Count > 0 )
				m_FindItemsList.Clear();

			List<T> list = new List<T>();

			RecurseFindItemsByType<T>( this, recurse, list, predicate );

			return list;
		}

		private static void RecurseFindItemsByType<T>( Item current, bool recurse, List<T> list, Predicate<T> predicate ) where T : Item
		{
			if ( current != null && current.Items.Count > 0 )
			{
				List<Item> items = current.Items;

				for( int i = 0; i < items.Count; ++i )
				{
					Item item = items[i];

					if ( typeof( T ).IsAssignableFrom( item.GetType() ) )
					{
						T typedItem = (T)item;

						if ( predicate == null || predicate( typedItem ) )
							list.Add( typedItem );
					}

					if ( recurse && item is Container )
						RecurseFindItemsByType<T>( item, recurse, list, predicate );
				}
			}
		}

		public T FindItemByType<T>() where T : Item
		{
			return FindItemByType<T>( true );
		}


		public T FindItemByType<T>( Predicate<T> predicate ) where T : Item
		{
			return FindItemByType<T>( true, predicate );
		}

		public T FindItemByType<T>( bool recurse ) where T : Item
		{
			return FindItemByType<T>( recurse, null );
		}

		public T FindItemByType<T>( bool recurse, Predicate<T> predicate ) where T : Item
		{
			return RecurseFindItemByType<T>( this, recurse, predicate );
		}

		private static T RecurseFindItemByType<T>( Item current, bool recurse, Predicate<T> predicate ) where T : Item
		{
			if ( current != null && current.Items.Count > 0 )
			{
				List<Item> list = current.Items;

				for( int i = 0; i < list.Count; ++i )
				{
					Item item = list[i];

					if ( typeof( T ).IsAssignableFrom( item.GetType() ) )
					{
						T typedItem = (T)item;

						if ( predicate == null || predicate( typedItem ) )
							return typedItem;
					}
					else if ( recurse && item is Container )
					{
						T check = RecurseFindItemByType<T>( item, recurse, predicate );

						if ( check != null )
							return check;
					}
				}
			}

			return null;
		}
		#endregion


		private static bool InTypeList( Item item, Type[] types )
		{
			Type t = item.GetType();

			for ( int i = 0; i < types.Length; ++i )
				if ( types[i].IsAssignableFrom( t ) )
					return true;

			return false;
		}

		private static void SetSaveFlag( ref SaveFlag flags, SaveFlag toSet, bool setIf )
		{
			if ( setIf )
				flags |= toSet;
		}

		private static bool GetSaveFlag( SaveFlag flags, SaveFlag toGet )
		{
			return ( (flags & toGet) != 0 );
		}

		[Flags]
		private enum SaveFlag : byte
		{
			None					= 0x00000000,
			MaxItems				= 0x00000001,
			GumpID					= 0x00000002,
			DropSound				= 0x00000004,
			LiftOverride			= 0x00000008
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 ); // version

			SaveFlag flags = SaveFlag.None;

			SetSaveFlag( ref flags, SaveFlag.MaxItems,		m_MaxItems != -1 );
			SetSaveFlag( ref flags, SaveFlag.GumpID,		m_GumpID != -1 );
			SetSaveFlag( ref flags, SaveFlag.DropSound,		m_DropSound != -1 );
			SetSaveFlag( ref flags, SaveFlag.LiftOverride,	m_LiftOverride );

			writer.Write( (byte) flags );

			if ( GetSaveFlag( flags, SaveFlag.MaxItems ) )
				writer.WriteEncodedInt( (int) m_MaxItems );

			if ( GetSaveFlag( flags, SaveFlag.GumpID ) )
				writer.WriteEncodedInt( (int) m_GumpID );

			if ( GetSaveFlag( flags, SaveFlag.DropSound ) )
				writer.WriteEncodedInt( (int) m_DropSound );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 2:
				{
					SaveFlag flags = (SaveFlag)reader.ReadByte();

					if ( GetSaveFlag( flags, SaveFlag.MaxItems ) )
						m_MaxItems = reader.ReadEncodedInt();
					else
						m_MaxItems = -1;

					if ( GetSaveFlag( flags, SaveFlag.GumpID ) )
						m_GumpID = reader.ReadEncodedInt();
					else
						m_GumpID = -1;

					if ( GetSaveFlag( flags, SaveFlag.DropSound ) )
						m_DropSound = reader.ReadEncodedInt();
					else
						m_DropSound = -1;

					m_LiftOverride = GetSaveFlag( flags, SaveFlag.LiftOverride );

					break;
				}
				case 1:
				{
					m_MaxItems = reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					if ( version < 1 )
						m_MaxItems = m_GlobalMaxItems;

					m_GumpID = reader.ReadInt();
					m_DropSound = reader.ReadInt();

					if ( m_GumpID == DefaultGumpID )
						m_GumpID = -1;

					if ( m_DropSound == DefaultDropSound )
						m_DropSound = -1;

					if ( m_MaxItems == DefaultMaxItems )
						m_MaxItems = -1;

					//m_Bounds = new Rectangle2D( reader.ReadPoint2D(), reader.ReadPoint2D() );
					reader.ReadPoint2D();
					reader.ReadPoint2D();

					break;
				}
			}

			UpdateContainerData();
		}

		private static int m_GlobalMaxItems = 125;
		private static int m_GlobalMaxWeight = 400;

		public static int GlobalMaxItems{ get => m_GlobalMaxItems;
			set => m_GlobalMaxItems = value;
		}
		public static int GlobalMaxWeight{ get => m_GlobalMaxWeight;
			set => m_GlobalMaxWeight = value;
		}

		public Container( int itemID ) : base( itemID )
		{
			m_GumpID = -1;
			m_DropSound = -1;
			m_MaxItems = -1;

			UpdateContainerData();
		}

		public override int GetTotal( TotalType type )
		{
			switch ( type )
			{
				case TotalType.Gold:
					return m_TotalGold;

				case TotalType.Items:
					return m_TotalItems;

				case TotalType.Weight:
					return m_TotalWeight;
			}

			return base.GetTotal( type );
		}

		public override void UpdateTotal( Item sender, TotalType type, int delta )
		{
			if ( sender != this && delta != 0 && !sender.IsVirtualItem )
			{
				switch ( type )
				{
					case TotalType.Gold:
						m_TotalGold += delta;
						break;

					case TotalType.Items:
						m_TotalItems += delta;
						InvalidateProperties();
						break;

					case TotalType.Weight:
						m_TotalWeight += delta;
						InvalidateProperties();
						break;
				}
			}

			base.UpdateTotal( sender, type, delta );
		}

		public override void UpdateTotals()
		{
			m_TotalGold = 0;
			m_TotalItems = 0;
			m_TotalWeight = 0;

			List<Item> items = m_Items;

			if ( items == null )
				return;

			for ( int i = 0; i < items.Count; ++i )
			{
				Item item = items[i];

				item.UpdateTotals();

				if ( item.IsVirtualItem )
					continue;

				m_TotalGold += item.TotalGold;
				m_TotalItems += item.TotalItems + 1;
				m_TotalWeight += item.TotalWeight + item.PileWeight;
			}
		}

		public Container( Serial serial ) : base( serial )
		{
		}

		public virtual bool OnStackAttempt( Mobile from, Item stack, Item dropped )
		{
			return CheckHold( from, dropped, true, false ) && stack.StackWith( from, dropped );
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( TryDropItem( from, dropped, true ) )
			{
				from.SendSound( GetDroppedSound( dropped ), GetWorldLocation() );

				return true;
			}

			return false;
		}

		public virtual bool TryDropItem( Mobile from, Item dropped, bool sendFullMessage )
		{
			return TryDropItem( from, dropped, sendFullMessage, false );
		}

		public virtual bool TryDropItem( Mobile from, Item dropped, bool sendFullMessage, bool playSound )
		{
			List<Item> list = Items;

			for ( int i = 0; i < list.Count; ++i )
			{
				Item item = list[i];

				if ( !(item is Container) && CheckHold( from, dropped, false, false ) && item.StackWith( from, dropped, playSound ) )
					return true;
			}

			if ( CheckHold( from, dropped, sendFullMessage, true ) )
			{
				DropItem( dropped );
				return true;
			}

			return false;
		}

		public virtual bool TryDropItems( Mobile from, bool sendFullMessage, params Item[] droppedItems )
		{
			List<Item> dropItems = new List<Item>();
			List<ItemStackEntry> stackItems = new List<ItemStackEntry>();

			int extraItems = 0;
			int extraWeight = 0;

//			from.SendMessage( String.Format( "There are {0} items in this container.", this.Items.Count ) );
//			from.SendMessage( String.Format( "There are {0} items being dropped into this container.", droppedItems.Length ) );

			for ( int i = 0; i < droppedItems.Length; i++ )
			{
				Item dropped = droppedItems[i];

				List<Item> list = Items;

				bool stacked = false;

				for ( int j = 0; j < list.Count; ++j )
				{
					Item item = list[j];

					if ( !(item is Container) && CheckHold( from, dropped, false, false, 0, extraWeight ) && item.CanStackWith( dropped ) )
					{
						stackItems.Add( new ItemStackEntry( item, dropped ) );
						extraWeight += (int)Math.Ceiling( item.Weight * (item.Amount + dropped.Amount) ) - item.PileWeight; //extra weight delta, do not need TotalWeight as we do not have hybrid stackable container types
						stacked = true;
						break;
					}
				}

				if ( !stacked && CheckHold( from, dropped, false, true, extraItems, extraWeight ) )
				{
					dropItems.Add( dropped );
					extraItems++;
					extraWeight += dropped.TotalWeight + dropped.PileWeight;
				}
			}

			if ( dropItems.Count + stackItems.Count == droppedItems.Length ) //All good
			{
				for ( int i = 0; i < dropItems.Count; i++ )
					DropItem( dropItems[i] );

				for ( int i = 0; i < stackItems.Count; i++ )
					stackItems[i].m_StackItem.StackWith( from, stackItems[i].m_DropItem, false );

				return true;
			}

			return false;
		}

		private struct ItemStackEntry
		{
			public Item m_StackItem;
			public Item m_DropItem;

			public ItemStackEntry( Item stack, Item drop )
			{
				m_StackItem = stack;
				m_DropItem = drop;
			}
		}

		public virtual void Destroy()
		{
			Point3D loc = GetWorldLocation();
			Map map = Map;

			for ( int i = Items.Count - 1; i >= 0; --i )
			{
				if ( i < Items.Count )
				{
					Items[i].SetLastMoved();
					Items[i].MoveToWorld( loc, map );
				}
			}

			Delete();
		}

		public virtual void DropItem( Item dropped )
		{
			if ( dropped == null )
				return;

			AddItem( dropped );

			Rectangle2D bounds = dropped.GetGraphicBounds();
			Rectangle2D ourBounds = Bounds;

			int x, y;

			if ( bounds.Width >= ourBounds.Width )
				x = (ourBounds.Width - bounds.Width) / 2;
			else
				x = Utility.Random( ourBounds.Width - bounds.Width );

			if ( bounds.Height >= ourBounds.Height )
				y = (ourBounds.Height - bounds.Height) / 2;
			else
				y = Utility.Random( ourBounds.Height - bounds.Height );

			x += ourBounds.X;
			x -= bounds.X;

			y += ourBounds.Y;
			y -= bounds.Y;

			dropped.Location = new Point3D( x, y, 0 );
		}

		public override void OnDoubleClickSecureTrade( Mobile from )
		{
			if ( from.InRange( GetWorldLocation(), 2 ) )
			{
				DisplayTo( from );

				SecureTradeContainer cont = GetSecureTradeCont();

				if ( cont != null )
				{
					SecureTrade trade = cont.Trade;

					if ( trade != null && trade.From.Mobile == from )
						DisplayTo( trade.To.Mobile );
					else if ( trade != null && trade.To.Mobile == from )
						DisplayTo( trade.From.Mobile );
				}
			}
			else
			{
				from.SendLocalizedMessage( 500446 ); // That is too far away.
			}
		}

		public virtual bool DisplaysContent => true;

		public virtual bool CheckContentDisplay( Mobile from )
		{
			if ( DisplaysContent )
			{
				object root = RootParent;

				if ( root == null || root is Item || root == from || from.AccessLevel > AccessLevel.Player )
					return true;
			}

			return false;
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			if ( CheckContentDisplay( from ) )
				LabelTo( from, "({0} item{2}, {1} stones)", TotalItems, TotalWeight, TotalItems != 1 ? "s" : string.Empty );
				//LabelTo( from, 1050044, String.Format( "{0}\t{1}", TotalItems.ToString(), TotalWeight.ToString() ) );
		}

		private List<Mobile> m_Openers;

		public List<Mobile> Openers
		{
			get => m_Openers;
			set => m_Openers = value;
		}

		public virtual bool IsPublicContainer => false;

		public override void OnDelete()
		{
			base.OnDelete();

			m_Openers = null;
		}

		public virtual void DisplayTo( Mobile to )
		{
			ProcessOpeners( to );

			NetState ns = to.NetState;

			if ( ns != null )
			{
				if ( ns.HighSeas )
					to.Send( new ContainerDisplayHS( this ) );
				else
					to.Send( new ContainerDisplay( this ) );

				SendContentTo( ns );

				if ( ObjectPropertyList.Enabled )
				{
					List<Item> items = Items;

					for ( int i = 0; i < items.Count; ++i )
						to.Send( items[i].OPLPacket );
				}
			}
		}

		public void ProcessOpeners( Mobile opener )
		{
			if ( !IsPublicContainer )
			{
				bool contains = false;

				if ( m_Openers != null )
				{
					Point3D worldLoc = GetWorldLocation();
					Map map = Map;

					for ( int i = 0; i < m_Openers.Count; ++i )
					{
						Mobile mob = m_Openers[i];

						if ( mob == opener )
							contains = true;
						else
						{
							int range = GetUpdateRange( mob );

							if ( mob.Map != map || !mob.InRange( worldLoc, range ) )
								m_Openers.RemoveAt( i-- );
						}
					}
				}

				if ( !contains )
				{
					if ( m_Openers == null )
						m_Openers = new List<Mobile>();

					m_Openers.Add( opener );
				}
				else if ( m_Openers != null && m_Openers.Count == 0 )
					m_Openers = null;
			}
		}

		public virtual void SendContentTo( NetState state )
		{
			if ( state != null && state.ContainerGridLines )
				state.Send( new ContainerContent6017( state.Mobile, this ) );
			else
				state.Send( new ContainerContent( state.Mobile, this ) );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( DisplaysContent )//CheckContentDisplay( from ) )
			{
				if ( Core.ML )
				{
					if ( ParentsContain<BankBox>() )	//Root Parent is the Mobile.  Parent could be another containter.
						list.Add( 1073841, "{0}\t{1}\t{2}", TotalItems, MaxItems, TotalWeight ); // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~ stones
					else
						list.Add( 1072241, "{0}\t{1}\t{2}\t{3}", TotalItems, MaxItems, TotalWeight, MaxWeight ); // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones

					//TODO: Where do the other clilocs come into play? 1073839 & 1073840?
				}
				else
					list.Add( 1050044, "{0}\t{1}", TotalItems, TotalWeight ); // ~1_COUNT~ items, ~2_WEIGHT~ stones
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel > AccessLevel.Player || from.InRange( GetWorldLocation(), 2 ) )
				DisplayTo( from );
			else
				from.SendLocalizedMessage( 500446 ); // That is too far away.
		}
	}

	public class ContainerData
	{
		static ContainerData()
		{
			m_Table = new Dictionary<int, ContainerData>();

			string path = Path.Combine( Core.BaseDirectory, "Data/containers.cfg" );

			if ( !File.Exists( path ) )
			{
				m_Default = new ContainerData( 0x3C, new Rectangle2D( 44, 65, 142, 94 ), 0x48 );
				return;
			}

			using ( StreamReader reader = new StreamReader( path ) )
			{
				string line;

				while ( (line = reader.ReadLine()) != null )
				{
					line = line.Trim();

					if ( line.Length == 0 || line.StartsWith( "#" ) )
						continue;

					try
					{
						string[] split = line.Split( '\t' );

						if ( split.Length >= 3 )
						{
							int gumpID = Utility.ToInt32( split[0] );

							string[] aRect = split[1].Split( ' ' );
							if ( aRect.Length < 4 )
								continue;

							int x = Utility.ToInt32( aRect[0] );
							int y = Utility.ToInt32( aRect[1] );
							int width = Utility.ToInt32( aRect[2] );
							int height = Utility.ToInt32( aRect[3] );

							Rectangle2D bounds = new Rectangle2D( x, y, width, height );

							int dropSound = Utility.ToInt32( split[2] );

							ContainerData data = new ContainerData( gumpID, bounds, dropSound );

							if ( m_Default == null )
								m_Default = data;

							if ( split.Length >= 4 )
							{
								string[] aIDs = split[3].Split( ',' );

								for ( int i = 0; i < aIDs.Length; i++ )
								{
									int id = Utility.ToInt32( aIDs[i] );

									if ( m_Table.ContainsKey( id ) )
									{
										Console.WriteLine( @"Warning: double ItemID entry in Data\containers.cfg" );
									}
									else
									{
										m_Table[id] = data;
									}
								}
							}
						}
					}
					catch
					{
					}
				}
			}

			if ( m_Default == null )
				m_Default = new ContainerData( 0x3C, new Rectangle2D( 44, 65, 142, 94 ), 0x48 );
		}

		private static ContainerData m_Default;
		private static Dictionary<int, ContainerData> m_Table;

		public static ContainerData Default
		{
			get => m_Default;
			set => m_Default = value;
		}

		public static ContainerData GetData( int itemID )
		{
			ContainerData data = null;
			m_Table.TryGetValue( itemID, out data );

			if ( data != null )
				return data;
			return m_Default;
		}

		private int m_GumpID;
		private Rectangle2D m_Bounds;
		private int m_DropSound;

		public int GumpID => m_GumpID;
		public Rectangle2D Bounds => m_Bounds;
		public int DropSound => m_DropSound;

		public ContainerData( int gumpID, Rectangle2D bounds, int dropSound )
		{
			m_GumpID = gumpID;
			m_Bounds = bounds;
			m_DropSound = dropSound;
		}
	}
}
