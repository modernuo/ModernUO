using System;
using System.Collections.Generic;

namespace Server.Engines.BulkOrders
{
	public class LargeBulkEntry
	{
		private LargeBOD m_Owner;
		private int m_Amount;
		private SmallBulkEntry m_Details;

		public LargeBOD Owner{ get => m_Owner;
			set => m_Owner = value;
		}
		public int Amount{ get => m_Amount;
			set{ m_Amount = value;
				m_Owner?.InvalidateProperties();
			} }
		public SmallBulkEntry Details => m_Details;

		public static SmallBulkEntry[] LargeRing => GetEntries( "Blacksmith", "largering" );

		public static SmallBulkEntry[] LargePlate => GetEntries( "Blacksmith", "largeplate" );

		public static SmallBulkEntry[] LargeChain => GetEntries( "Blacksmith", "largechain" );

		public static SmallBulkEntry[] LargeAxes => GetEntries( "Blacksmith", "largeaxes" );

		public static SmallBulkEntry[] LargeFencing => GetEntries( "Blacksmith", "largefencing" );

		public static SmallBulkEntry[] LargeMaces => GetEntries( "Blacksmith", "largemaces" );

		public static SmallBulkEntry[] LargePolearms => GetEntries( "Blacksmith", "largepolearms" );

		public static SmallBulkEntry[] LargeSwords => GetEntries( "Blacksmith", "largeswords" );


		public static SmallBulkEntry[] BoneSet => GetEntries( "Tailoring", "boneset" );

		public static SmallBulkEntry[] Farmer => GetEntries( "Tailoring", "farmer" );

		public static SmallBulkEntry[] FemaleLeatherSet => GetEntries( "Tailoring", "femaleleatherset" );

		public static SmallBulkEntry[] FisherGirl => GetEntries( "Tailoring", "fishergirl" );

		public static SmallBulkEntry[] Gypsy => GetEntries( "Tailoring", "gypsy" );

		public static SmallBulkEntry[] HatSet => GetEntries( "Tailoring", "hatset" );

		public static SmallBulkEntry[] Jester => GetEntries( "Tailoring", "jester" );

		public static SmallBulkEntry[] Lady => GetEntries( "Tailoring", "lady" );

		public static SmallBulkEntry[] MaleLeatherSet => GetEntries( "Tailoring", "maleleatherset" );

		public static SmallBulkEntry[] Pirate => GetEntries( "Tailoring", "pirate" );

		public static SmallBulkEntry[] ShoeSet => GetEntries( "Tailoring", "shoeset" );

		public static SmallBulkEntry[] StuddedSet => GetEntries( "Tailoring", "studdedset" );

		public static SmallBulkEntry[] TownCrier => GetEntries( "Tailoring", "towncrier" );

		public static SmallBulkEntry[] Wizard => GetEntries( "Tailoring", "wizard" );


		private static Dictionary<string,Dictionary<string,SmallBulkEntry[]>> m_Cache;

		public static SmallBulkEntry[] GetEntries( string type, string name )
		{
			if (m_Cache == null)
				m_Cache = new Dictionary<string, Dictionary<string, SmallBulkEntry[]>>();

			if (!m_Cache.TryGetValue( type, out Dictionary<string, SmallBulkEntry[]> table ))
				m_Cache[type] = table = new Dictionary<string, SmallBulkEntry[]>();

			if (!table.TryGetValue( name, out SmallBulkEntry[] entries ))
				table[name] = entries = SmallBulkEntry.LoadEntries(type, name);

			return entries;
		}

		public static LargeBulkEntry[] ConvertEntries( LargeBOD owner, SmallBulkEntry[] small )
		{
			LargeBulkEntry[] large = new LargeBulkEntry[small.Length];

			for ( int i = 0; i < small.Length; ++i )
				large[i] = new LargeBulkEntry( owner, small[i] );

			return large;
		}

		public LargeBulkEntry( LargeBOD owner, SmallBulkEntry details )
		{
			m_Owner = owner;
			m_Details = details;
		}

		public LargeBulkEntry( LargeBOD owner, GenericReader reader )
		{
			m_Owner = owner;
			m_Amount = reader.ReadInt();

			Type realType = null;

			string type = reader.ReadString();

			if ( type != null )
				realType = ScriptCompiler.FindTypeByFullName( type );

			m_Details = new SmallBulkEntry( realType, reader.ReadInt(), reader.ReadInt() );
		}

		public void Serialize( GenericWriter writer )
		{
			writer.Write( m_Amount );
			writer.Write( m_Details.Type == null ? null : m_Details.Type.FullName );
			writer.Write( m_Details.Number );
			writer.Write( m_Details.Graphic );
		}
	}
}
