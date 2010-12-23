/***************************************************************************
 *                              PacketProfile.cs
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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace Server.Diagnostics {
	public abstract class BaseProfile {
		public static void WriteAll<T>( TextWriter op, IEnumerable<T> profiles ) where T : BaseProfile {
			List<T> list = new List<T>( profiles );

			list.Sort( delegate( T a, T b ) {
				return -a.TotalTime.CompareTo( b.TotalTime );
			} );

			foreach ( T prof in list ) {
				prof.WriteTo( op );
				op.WriteLine();
			}
		}

		private string _name;

		private long _count;

		private TimeSpan _totalTime;
		private TimeSpan _peakTime;

		private Stopwatch _stopwatch;

		public string Name {
			get {
				return _name;
			}
		}

		public long Count {
			get {
				return _count;
			}
		}

		public TimeSpan AverageTime {
			get {
				return TimeSpan.FromTicks( _totalTime.Ticks / Math.Max( 1, _count ) );
			}
		}

		public TimeSpan PeakTime {
			get {
				return _peakTime;
			}
		}

		public TimeSpan TotalTime {
			get {
				return _totalTime;
			}
		}

		protected BaseProfile( string name ) {
			_name = name;

			_stopwatch = new Stopwatch();
		}

		public virtual void Start() {
			if ( _stopwatch.IsRunning ) {
				_stopwatch.Reset();
			}

			_stopwatch.Start();
		}

		public virtual void Finish() {
			TimeSpan elapsed = _stopwatch.Elapsed;

			_totalTime += elapsed;

			if ( elapsed > _peakTime ) {
				_peakTime = elapsed;
			}

			_count++;

			_stopwatch.Reset();
		}

		public virtual void WriteTo( TextWriter op ) {
			op.Write( "{0,-100} {1,12:N0} {2,12:F5} {3,-12:F5} {4,12:F5}", Name, Count, AverageTime.TotalSeconds, PeakTime.TotalSeconds, TotalTime.TotalSeconds );
		}
	}
}