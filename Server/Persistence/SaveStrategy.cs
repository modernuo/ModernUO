/***************************************************************************
 *                             SaveStrategy.cs
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
	public abstract class SaveStrategy {
		public static SaveStrategy Acquire() {
			if ( Core.MultiProcessor ) {
				int processorCount = Core.ProcessorCount;

				if ( processorCount > 8 ) {
					return new ParallelSaveStrategy( processorCount );
				} else {
					return new DualSaveStrategy();
				}
			} else {
				return new StandardSaveStrategy();
			}
		}

		public abstract string Name { get; }
		public abstract void Save( SaveMetrics metrics );
	}
}