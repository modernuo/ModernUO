using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Multis
{
	public class ComponentVerification
	{
		private byte[] m_ItemTable;
		private byte[] m_MultiTable;

		public bool IsItemValid( int itemID )
		{
			if ( itemID <= 0 || itemID >= m_ItemTable.Length )
				return false;

			return CheckValidity( m_ItemTable[itemID] );
		}

		public bool IsMultiValid( int multiID )
		{
			if ( multiID <= 0 || multiID >= m_MultiTable.Length )
				return false;

			return CheckValidity( m_MultiTable[multiID] );
		}

		public bool CheckValidity( byte val )
		{
			return ( val == 0 || (ExpansionInfo.CurrentExpansion.CustomHousingFlag & val) != 0 );
		}

		public ComponentVerification()
		{
			m_ItemTable = CreateTable( 0x4000 );
			m_MultiTable = CreateTable( 0x4000 );

			LoadItems( "Data/Components/walls.txt", "South1", "South2", "South3", "Corner", "East1", "East2", "East3", "Post", "WindowS", "AltWindowS", "WindowE", "AltWindowE", "SecondAltWindowS", "SecondAltWindowE" );
			LoadItems( "Data/Components/teleprts.txt", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "F13", "F14", "F15", "F16" );
			LoadItems( "Data/Components/stairs.txt", "Block", "North", "East", "South", "West", "Squared1", "Squared2", "Rounded1", "Rounded2" );
			LoadItems( "Data/Components/roof.txt", "North", "East", "South", "West", "NSCrosspiece", "EWCrosspiece", "NDent", "EDent", "SDent", "WDent", "NTPiece", "ETPiece", "STPiece", "WTPiece", "XPiece", "Extra Piece" );
			LoadItems( "Data/Components/floors.txt", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "F13", "F14", "F15", "F16" );
			LoadItems( "Data/Components/misc.txt", "Piece1", "Piece2", "Piece3", "Piece4", "Piece5", "Piece6", "Piece7", "Piece8" );
			LoadItems( "Data/Components/doors.txt", "Piece1", "Piece2", "Piece3", "Piece4", "Piece5", "Piece6", "Piece7", "Piece8" );

			LoadMultis( "Data/Components/stairs.txt", "MultiNorth", "MultiEast", "MultiSouth", "MultiWest" );
		}

		private byte[] CreateTable( int length )
		{
			byte[] table = new byte[length];

			for ( int i = 0; i < table.Length; ++i )
				table[i] = 0xFF;

			return table;
		}

		private void LoadItems( string path, params string[] itemColumns )
		{
			LoadSpreadsheet( m_ItemTable, path, itemColumns );
		}

		private void LoadMultis( string path, params string[] multiColumns )
		{
			LoadSpreadsheet( m_MultiTable, path, multiColumns );
		}

		private void LoadSpreadsheet( byte[] table, string path, params string[] tileColumns )
		{
			Spreadsheet ss = new Spreadsheet( path );

			int[] tileCIDs = new int[tileColumns.Length];

			for ( int i = 0; i < tileColumns.Length; ++i )
				tileCIDs[i] = ss.GetColumnID( tileColumns[i] );

			int featureCID = ss.GetColumnID( "FeatureMask" );

			for ( int i = 0; i < ss.Records.Length; ++i )
			{
				DataRecord record = ss.Records[i];

				byte fid = (byte) record.GetInt32( featureCID );

				for ( int j = 0; j < tileCIDs.Length; ++j )
				{
					int itemID = record.GetInt32( tileCIDs[j] );

					if ( itemID <= 0 || itemID >= table.Length )
						continue;

					table[itemID] = fid;
				}
			}
		}
	}

	public class Spreadsheet
	{
		private class ColumnInfo
		{
			public int m_DataIndex;

			public string m_Type;
			public string m_Name;

			public ColumnInfo( int dataIndex, string type, string name )
			{
				m_DataIndex = dataIndex;

				m_Type = type;
				m_Name = name;
			}
		}

		private ColumnInfo[] m_Columns;
		private DataRecord[] m_Records;

		public DataRecord[] Records { get { return m_Records; } }

		public int GetColumnID( string name )
		{
			for ( int i = 0; i < m_Columns.Length; ++i )
			{
				if ( m_Columns[i].m_Name == name )
					return i;
			}

			return -1;
		}

		public Spreadsheet( string path )
		{
			using ( StreamReader ip = new StreamReader( path ) )
			{
				string[] types = ReadLine( ip );
				string[] names = ReadLine( ip );

				m_Columns = new ColumnInfo[types.Length];

				for ( int i = 0; i < m_Columns.Length; ++i )
					m_Columns[i] = new ColumnInfo( i, types[i], names[i] );

				List<DataRecord> records = new List<DataRecord>();

				string[] values;

				while ( ( values = ReadLine( ip ) ) != null )
				{
					object[] data = new object[m_Columns.Length];

					for ( int i = 0; i < m_Columns.Length; ++i )
					{
						ColumnInfo ci = m_Columns[i];

						switch ( ci.m_Type )
						{
							case "int":
								{
									data[i] = Utility.ToInt32( values[ci.m_DataIndex] );
									break;
								}
							case "string":
								{
									data[i] = values[ci.m_DataIndex];
									break;
								}
						}
					}

					records.Add( new DataRecord( this, data ) );
				}

				m_Records = records.ToArray();
			}
		}

		private string[] ReadLine( StreamReader ip )
		{
			string line;

			while ( ( line = ip.ReadLine() ) != null )
			{
				if ( line.Length == 0 )
					continue;

				return line.Split( '\t' );
			}

			return null;
		}
	}

	public class DataRecord
	{
		private Spreadsheet m_Spreadsheet;
		private object[] m_Data;

		public Spreadsheet Spreadsheet { get { return m_Spreadsheet; } }
		public object[] Data { get { return m_Data; } }

		public DataRecord( Spreadsheet ss, object[] data )
		{
			m_Spreadsheet = ss;
			m_Data = data;
		}

		public int GetInt32( string name )
		{
			return GetInt32( this[name] );
		}

		public int GetInt32( int id )
		{
			return GetInt32( this[id] );
		}

		public int GetInt32( object obj )
		{
			if ( obj is int )
				return (int) obj;

			return 0;
		}

		public string GetString( string name )
		{
			return this[name] as string;
		}

		public object this[string name]
		{
			get
			{
				return this[m_Spreadsheet.GetColumnID( name )];
			}
		}

		public object this[int id]
		{
			get
			{
				if ( id < 0 )
					return null;

				return m_Data[id];
			}
		}
	}
}