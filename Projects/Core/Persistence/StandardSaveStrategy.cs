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
using System.Threading.Tasks;
using Server.Guilds;

namespace Server
{
    public class StandardSaveStrategy : SaveStrategy
    {
        public enum SaveOption
        {
            Normal,
            Threaded
        }

        private readonly Queue<Item> _decayQueue;

        public StandardSaveStrategy() => _decayQueue = new Queue<Item>();

        // TODO: Move to configuration
        public static SaveOption SaveType => SaveOption.Normal;

        public override string Name => "Standard";

        protected bool PermitBackgroundWrite { get; set; }

        protected bool UseSequentialWriters => SaveType == SaveOption.Normal || !PermitBackgroundWrite;

        public override void Save(bool permitBackgroundWrite)
        {
            PermitBackgroundWrite = permitBackgroundWrite;

#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler        *
            Task.WaitAll(
                Task.Factory.StartNew(SaveMobiles),
                Task.Factory.StartNew(SaveItems),
                Task.Factory.StartNew(SaveGuilds)
            );
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler        *

            if (permitBackgroundWrite && UseSequentialWriters
            ) // If we're permitted to write in the background, but we don't anyways, then notify.
                World.NotifyDiskWriteComplete();
        }

        protected void SaveMobiles()
        {
            var mobiles = World.Mobiles;

            IGenericWriter idx;
            IGenericWriter tdb;
            IGenericWriter bin;

            if (UseSequentialWriters)
            {
                idx = new BinaryFileWriter(World.MobileIndexPath, false);
                tdb = new BinaryFileWriter(World.MobileTypesPath, false);
                bin = new BinaryFileWriter(World.MobileDataPath, true);
            }
            else
            {
                idx = new AsyncWriter(World.MobileIndexPath, false);
                tdb = new AsyncWriter(World.MobileTypesPath, false);
                bin = new AsyncWriter(World.MobileDataPath, true);
            }
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler        *
            Task.Factory.StartNew(
                () =>
                {
                    tdb.Write(World.m_MobileTypes.Count);

                    for (var i = 0; i < World.m_MobileTypes.Count; ++i)
                        tdb.Write(World.m_MobileTypes[i].FullName);

                    tdb.Close();
                }
            );
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler        *

            Parallel.ForEach(mobiles.Values, mobile => mobile.Serialize());
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler        *
            Task.Factory.StartNew(
                () =>
                {
                    idx.Write(mobiles.Count);
                    foreach (var m in mobiles.Values)
                    {
                        var start = bin.Position;

                        idx.Write(m.TypeRef);
                        idx.Write(m.Serial);
                        idx.Write(start);
                        idx.Write((int)m.SaveBuffer.Position);

                        m.SaveBuffer.WriteTo(bin);
                        m.FreeCache();
                    }

                    idx.Close();
                    bin.Close();
                }
            );
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler        *
        }

        protected void SaveItems()
        {
            var items = World.Items;

            IGenericWriter idx;
            IGenericWriter tdb;
            IGenericWriter bin;

            if (UseSequentialWriters)
            {
                idx = new BinaryFileWriter(World.ItemIndexPath, false);
                tdb = new BinaryFileWriter(World.ItemTypesPath, false);
                bin = new BinaryFileWriter(World.ItemDataPath, true);
            }
            else
            {
                idx = new AsyncWriter(World.ItemIndexPath, false);
                tdb = new AsyncWriter(World.ItemTypesPath, false);
                bin = new AsyncWriter(World.ItemDataPath, true);
            }

#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler        *
            Task.Factory.StartNew(
                () =>
                {
                    tdb.Write(World.m_ItemTypes.Count);

                    for (var i = 0; i < World.m_ItemTypes.Count; ++i)
                        tdb.Write(World.m_ItemTypes[i].FullName);

                    tdb.Close();
                }
            );
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler        *
            Parallel.ForEach(items.Values, item => item.Serialize());

            idx.Write(items.Count);

            var n = DateTime.UtcNow;

#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler        *
            Task.Factory.StartNew(
                () =>
                {
                    foreach (var item in items.Values)
                    {
                        if (item.Decays && item.Parent == null && item.Map != Map.Internal &&
                            item.LastMoved + item.DecayTime <= n)
                        {
                            Console.WriteLine($"Decay Item {item.Name ?? item.DefaultName} ({item.GetType().FullName})");
                            _decayQueue.Enqueue(item);
                        }

                        var start = bin.Position;

                        idx.Write(item.TypeRef);
                        idx.Write(item.Serial);
                        idx.Write(start);
                        idx.Write((int)item.SaveBuffer.Position);

                        item.SaveBuffer.WriteTo(bin);
                        item.FreeCache();
                    }

                    idx.Close();
                    bin.Close();
                }
            );
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler        *
        }

        protected void SaveGuilds()
        {
            IGenericWriter idx;
            IGenericWriter bin;

            if (UseSequentialWriters)
            {
                idx = new BinaryFileWriter(World.GuildIndexPath, false);
                bin = new BinaryFileWriter(World.GuildDataPath, true);
            }
            else
            {
                idx = new AsyncWriter(World.GuildIndexPath, false);
                bin = new AsyncWriter(World.GuildDataPath, true);
            }

            Parallel.ForEach(BaseGuild.List.Values, guild => guild.Serialize());

            idx.Write(BaseGuild.List.Count);
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler        *
            Task.Factory.StartNew(
                () =>
                {
                    foreach (var guild in BaseGuild.List.Values)
                    {
                        var start = bin.Position;

                        idx.Write(0); // guilds have no typeid
                        idx.Write(guild.Serial);
                        idx.Write(start);
                        idx.Write((int)guild.SaveBuffer.Position);

                        guild.SaveBuffer.WriteTo(bin);
                    }

                    idx.Close();
                    bin.Close();
                }
            );
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler        *
        }

        public override void ProcessDecay()
        {
            while (_decayQueue.Count > 0)
            {
                var item = _decayQueue.Dequeue();

                if (item.OnDecay())
                    item.Delete();
            }
        }
    }
}
