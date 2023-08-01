using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Multis
{
    public class ComponentVerification
    {
        private readonly int[] m_ItemTable;
        private readonly int[] m_MultiTable;

        public ComponentVerification()
        {
            m_ItemTable = CreateTable(TileData.MaxItemValue);
            m_MultiTable = CreateTable(0x4000);

            LoadItems(
                "Data/Components/walls.txt",
                "South1",
                "South2",
                "South3",
                "Corner",
                "East1",
                "East2",
                "East3",
                "Post",
                "WindowS",
                "AltWindowS",
                "WindowE",
                "AltWindowE",
                "SecondAltWindowS",
                "SecondAltWindowE"
            );
            LoadItems(
                "Data/Components/teleprts.txt",
                "F1",
                "F2",
                "F3",
                "F4",
                "F5",
                "F6",
                "F7",
                "F8",
                "F9",
                "F10",
                "F11",
                "F12",
                "F13",
                "F14",
                "F15",
                "F16"
            );
            LoadItems(
                "Data/Components/stairs.txt",
                "Block",
                "North",
                "East",
                "South",
                "West",
                "Squared1",
                "Squared2",
                "Rounded1",
                "Rounded2"
            );
            LoadItems(
                "Data/Components/roof.txt",
                "North",
                "East",
                "South",
                "West",
                "NSCrosspiece",
                "EWCrosspiece",
                "NDent",
                "EDent",
                "SDent",
                "WDent",
                "NTPiece",
                "ETPiece",
                "STPiece",
                "WTPiece",
                "XPiece",
                "Extra Piece"
            );
            LoadItems(
                "Data/Components/floors.txt",
                "F1",
                "F2",
                "F3",
                "F4",
                "F5",
                "F6",
                "F7",
                "F8",
                "F9",
                "F10",
                "F11",
                "F12",
                "F13",
                "F14",
                "F15",
                "F16"
            );
            LoadItems(
                "Data/Components/misc.txt",
                "Piece1",
                "Piece2",
                "Piece3",
                "Piece4",
                "Piece5",
                "Piece6",
                "Piece7",
                "Piece8"
            );
            LoadItems(
                "Data/Components/doors.txt",
                "Piece1",
                "Piece2",
                "Piece3",
                "Piece4",
                "Piece5",
                "Piece6",
                "Piece7",
                "Piece8"
            );

            LoadMultis("Data/Components/stairs.txt", "MultiNorth", "MultiEast", "MultiSouth", "MultiWest");
        }

        public bool IsItemValid(int itemID) =>
            itemID > 0 && itemID < m_ItemTable.Length && CheckValidity(m_ItemTable[itemID]);

        public bool IsMultiValid(int multiID) =>
            multiID > 0 && multiID < m_MultiTable.Length && CheckValidity(m_MultiTable[multiID]);

        public bool CheckValidity(int val) =>
            val != -1 && (val == 0 || ((int)ExpansionInfo.CoreExpansion.HousingFlags & val) != 0);

        private int[] CreateTable(int length)
        {
            var table = new int[length];

            for (var i = 0; i < table.Length; ++i)
            {
                table[i] = -1;
            }

            return table;
        }

        private void LoadItems(string path, params string[] itemColumns)
        {
            LoadSpreadsheet(m_ItemTable, path, itemColumns);
        }

        private void LoadMultis(string path, params string[] multiColumns)
        {
            LoadSpreadsheet(m_MultiTable, path, multiColumns);
        }

        private void LoadSpreadsheet(int[] table, string path, params string[] tileColumns)
        {
            var ss = new Spreadsheet(path);

            var tileCIDs = new int[tileColumns.Length];

            for (var i = 0; i < tileColumns.Length; ++i)
            {
                tileCIDs[i] = ss.GetColumnID(tileColumns[i]);
            }

            var featureCID = ss.GetColumnID("FeatureMask");

            for (var i = 0; i < ss.Records.Length; ++i)
            {
                var record = ss.Records[i];

                var fid = record.GetInt32(featureCID);

                for (var j = 0; j < tileCIDs.Length; ++j)
                {
                    var itemID = record.GetInt32(tileCIDs[j]);

                    if (itemID <= 0 || itemID >= table.Length)
                    {
                        continue;
                    }

                    table[itemID] = fid;
                }
            }
        }
    }

    public class Spreadsheet
    {
        private readonly ColumnInfo[] m_Columns;

        public Spreadsheet(string path)
        {
            using var ip = new StreamReader(path);
            var types = ReadLine(ip);
            var names = ReadLine(ip);

            m_Columns = new ColumnInfo[types.Length];

            for (var i = 0; i < m_Columns.Length; ++i)
            {
                m_Columns[i] = new ColumnInfo(i, types[i], names[i]);
            }

            var records = new List<DataRecord>();

            string[] values;

            while ((values = ReadLine(ip)) != null)
            {
                var data = new object[m_Columns.Length];

                for (var i = 0; i < m_Columns.Length; ++i)
                {
                    var ci = m_Columns[i];

                    switch (ci.m_Type)
                    {
                        case "int":
                            {
                                data[i] = Utility.ToInt32(values[ci.m_DataIndex]);
                                break;
                            }
                        case "string":
                            {
                                data[i] = values[ci.m_DataIndex];
                                break;
                            }
                    }
                }

                records.Add(new DataRecord(this, data));
            }

            Records = records.ToArray();
        }

        public DataRecord[] Records { get; }

        public int GetColumnID(string name)
        {
            for (var i = 0; i < m_Columns.Length; ++i)
            {
                if (m_Columns[i].m_Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        private string[] ReadLine(StreamReader ip)
        {
            string line;

            while ((line = ip.ReadLine()) != null)
            {
                if (line.Length > 0)
                {
                    return line.Split('\t');
                }
            }

            return null;
        }

        private class ColumnInfo
        {
            public readonly int m_DataIndex;
            public readonly string m_Name;

            public readonly string m_Type;

            public ColumnInfo(int dataIndex, string type, string name)
            {
                m_DataIndex = dataIndex;

                m_Type = type;
                m_Name = name;
            }
        }
    }

    public class DataRecord
    {
        public DataRecord(Spreadsheet ss, object[] data)
        {
            Spreadsheet = ss;
            Data = data;
        }

        public Spreadsheet Spreadsheet { get; }

        public object[] Data { get; }

        public object this[string name] => this[Spreadsheet.GetColumnID(name)];

        public object this[int id] => id < 0 ? null : Data[id];

        public int GetInt32(string name) => GetInt32(this[name]);

        public int GetInt32(int id) => GetInt32(this[id]);

        public int GetInt32(object obj) => Convert.ToInt32(obj);

        public string GetString(string name) => this[name] as string;
    }
}
