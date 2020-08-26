/***************************************************************************
 *                            DualSaveStrategy.cs
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

using System.Threading;

namespace Server
{
    public sealed class DualSaveStrategy : StandardSaveStrategy
    {
        public override string Name => "Dual";

        public override void Save(bool permitBackgroundWrite)
        {
            PermitBackgroundWrite = permitBackgroundWrite;

            var saveThread = new Thread(SaveItems);

            saveThread.Name = "Item Save Subset";
            saveThread.Start();

            SaveMobiles();
            SaveGuilds();

            saveThread.Join();

            if (permitBackgroundWrite && UseSequentialWriters
            ) // If we're permitted to write in the background, but we don't anyways, then notify.
                World.NotifyDiskWriteComplete();
        }
    }
}
