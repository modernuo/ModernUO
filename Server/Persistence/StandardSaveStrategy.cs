/***************************************************************************
 *                          StandardSaveStrategy.cs
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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

using Server;
using Server.Guilds;

namespace Server {
	public class StandardSaveStrategy : SaveStrategy {
		public enum SaveOption
		{
			Normal,
			Threaded
		}

		public static SaveOption SaveType = SaveOption.Normal;

		public override string Name {
			get { return "Standard"; }
		}

		private Queue<Item> _decayQueue;
		private bool _permitBackgroundWrite;

		public StandardSaveStrategy() {
			_decayQueue = new Queue<Item>();
		}

		protected bool PermitBackgroundWrite { get { return _permitBackgroundWrite; } set { _permitBackgroundWrite = value; } }

		protected bool UseSequentialWriters { get { return (StandardSaveStrategy.SaveType == SaveOption.Normal || !_permitBackgroundWrite); } }

		public override void Save(SaveMetrics metrics, bool permitBackgroundWrite)
		{
			_permitBackgroundWrite = permitBackgroundWrite;

			SaveMobiles(metrics);
			SaveItems(metrics);
			SaveGuilds(metrics);

			if (permitBackgroundWrite && UseSequentialWriters)	//If we're permitted to write in the background, but we don't anyways, then notify.
				World.NotifyDiskWriteComplete();
		}

		protected void SaveMobiles(SaveMetrics metrics)
		{
			Dictionary<Serial, Mobile> mobiles = World.Mobiles;

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if (UseSequentialWriters)
			{
				idx = new BinaryFileWriter( World.MobileIndexPath, false );
				tdb = new BinaryFileWriter( World.MobileTypesPath, false );
				bin = new BinaryFileWriter( World.MobileDataPath, true );
			} else {
				idx = new AsyncWriter( World.MobileIndexPath, false );
				tdb = new AsyncWriter( World.MobileTypesPath, false );
				bin = new AsyncWriter( World.MobileDataPath, true );
			}

			idx.Write( ( int ) mobiles.Count );
			foreach ( Mobile m in mobiles.Values ) {
				long start = bin.Position;

				idx.Write( ( int ) m.m_TypeRef );
				idx.Write( ( int ) m.Serial );
				idx.Write( ( long ) start );

				m.Serialize( bin );

				if ( metrics != null ) {
					metrics.OnMobileSaved( ( int ) ( bin.Position - start ) );
				}

				idx.Write( ( int ) ( bin.Position - start ) );

				m.FreeCache();
			}

			tdb.Write( ( int ) World.m_MobileTypes.Count );

			for ( int i = 0; i < World.m_MobileTypes.Count; ++i )
				tdb.Write( World.m_MobileTypes[i].FullName );

			idx.Close();
			tdb.Close();
			bin.Close();
		}

		protected void SaveItems(SaveMetrics metrics)
		{
			Dictionary<Serial, Item> items = World.Items;

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if (UseSequentialWriters)
			{
				idx = new BinaryFileWriter( World.ItemIndexPath, false );
				tdb = new BinaryFileWriter( World.ItemTypesPath, false );
				bin = new BinaryFileWriter( World.ItemDataPath, true );
			} else {
				idx = new AsyncWriter( World.ItemIndexPath, false );
				tdb = new AsyncWriter( World.ItemTypesPath, false );
				bin = new AsyncWriter( World.ItemDataPath, true );
			}

			idx.Write( ( int ) items.Count );

			DateTime n = DateTime.UtcNow;

			foreach ( Item item in items.Values ) {
				if (item.Decays && item.Parent == null && item.Map != Map.Internal && (item.LastMoved + item.DecayTime) <= n)
				{
					_decayQueue.Enqueue( item );
				}

				long start = bin.Position;

				idx.Write( ( int ) item.m_TypeRef );
				idx.Write( ( int ) item.Serial );
				idx.Write( ( long ) start );

				item.Serialize( bin );

				if ( metrics != null ) {
					metrics.OnItemSaved( ( int ) ( bin.Position - start ) );
				}

				idx.Write( ( int ) ( bin.Position - start ) );

				item.FreeCache();
			}

			tdb.Write( ( int ) World.m_ItemTypes.Count );
			for ( int i = 0; i < World.m_ItemTypes.Count; ++i )
				tdb.Write( World.m_ItemTypes[i].FullName );

			idx.Close();
			tdb.Close();
			bin.Close();
		}

		protected void SaveGuilds(SaveMetrics metrics)
		{
			GenericWriter idx;
			GenericWriter bin;

			if (UseSequentialWriters)
			{
				idx = new BinaryFileWriter( World.GuildIndexPath, false );
				bin = new BinaryFileWriter( World.GuildDataPath, true );
			} else {
				idx = new AsyncWriter( World.GuildIndexPath, false );
				bin = new AsyncWriter( World.GuildDataPath, true );
			}

			idx.Write( ( int ) BaseGuild.List.Count );
			foreach ( BaseGuild guild in BaseGuild.List.Values ) {
				long start = bin.Position;

				idx.Write( ( int ) 0 );//guilds have no typeid
				idx.Write( ( int ) guild.Id );
				idx.Write( ( long ) start );

				guild.Serialize( bin );

				if ( metrics != null ) {
					metrics.OnGuildSaved( ( int ) ( bin.Position - start ) );
				}

				idx.Write( ( int ) ( bin.Position - start ) );
			}

			idx.Close();
			bin.Close();
		}

		public override void ProcessDecay() {
			while ( _decayQueue.Count > 0 ) {
				Item item = _decayQueue.Dequeue();

				if ( item.OnDecay() ) {
					item.Delete();
				}
			}
		}
	}
}