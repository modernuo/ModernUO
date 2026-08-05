using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Server.Compression;
using Server.Logging;

namespace Server.Multis;

public static class ComponentVerification
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ComponentVerification));

    private static int[] _itemTable;
    private static int[] _multiTable;
    private static bool _loaded;

    // housing.bin stores each component's feature mask in the client's feature-flag bit space, which
    // tags pre-AOS base pieces (e.g. sandstone) with low bits - notably T2A (0x1) - that HousingFlags
    // does not model. CheckValidity validates against HousingFlags, so strip everything except the
    // housing-tier bits when loading: base pieces collapse to 0 (always valid, exactly as walls.txt
    // encodes them) while AOS/SE/ML/... line up unchanged.
    private const int HousingTierMask = (int)HousingFlags.HousingEJ;

    // Table sentinels. Slots start as NotAComponent, which CheckValidity rejects, so a piece that
    // never gets registered can never be placed. NoFeatureRequired is a piece with no expansion
    // requirement: walls.txt encodes pre-AOS base pieces as 0, housing.bin collapses to 0 under
    // HousingTierMask.
    private const int NotAComponent = -1;
    private const int NoFeatureRequired = 0;

    private const string FeatureMaskColumn = "FeatureMask";

    public static bool IsItemValid(int itemID)
    {
        EnsureLoaded();
        return itemID > 0 && itemID < _itemTable.Length && CheckValidity(_itemTable[itemID]);
    }

    public static bool IsMultiValid(int multiID)
    {
        EnsureLoaded();
        return multiID > 0 && multiID < _multiTable.Length && CheckValidity(_multiTable[multiID]);
    }

    private static bool CheckValidity(int val) =>
        val != NotAComponent &&
        (val == NoFeatureRequired || ((int)ExpansionInfo.CoreExpansion.HousingFlags & val) != 0);

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        // Set before loading: this runs from the design packet handler, so a bad file must not
        // re-read every sheet on each later placement attempt. Sheets below fail independently.
        _loaded = true;

        _itemTable = CreateTable(TileData.MaxItemValue);
        _multiTable = CreateTable(0x4000);

        var housingPath = MultiData.HousingUOPPath;
        if (housingPath != null && TryLoadFromHousingBin(housingPath))
        {
            return;
        }

        LoadFromTxtFiles();
    }

    private static bool TryLoadFromHousingBin(string path)
    {
        try
        {
            var data = ReadUOPEntry(path, MultiData.HousingEntry);
            if (data != null)
            {
                LoadFromHousingBin(data);
                return true;
            }

            logger.Warning(
                "Could not decompress housing.bin from {Path}. Falling back to the component sheets",
                path
            );
        }
        catch (Exception ex)
        {
            logger.Warning(
                ex,
                "Failed to read housing.bin from {Path}. Falling back to the component sheets",
                path
            );
        }

        return false;
    }

    private static byte[] ReadUOPEntry(string path, UOPEntry entry)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        stream.Seek(entry.Offset, SeekOrigin.Begin);

        if (entry.Compressed)
        {
            var compressedData = new byte[entry.CompressedSize];
            stream.ReadExactly(compressedData);
            var decompressedData = new byte[entry.Size];

            if (Deflate.Standard.Unpack(decompressedData, compressedData, out var bytesDecompressed) != LibDeflateResult.Success
                || entry.Size != bytesDecompressed)
            {
                return null;
            }

            return decompressedData;
        }

        var data = new byte[entry.Size];
        stream.ReadExactly(data);
        return data;
    }

    private static void LoadFromHousingBin(byte[] data)
    {
        var reader = new SpanReader(data);

        var fileTypesCount = (int)reader.ReadUInt32LE();

        for (var f = 0; f < fileTypesCount; f++)
        {
            var fileType = (int)reader.ReadUInt32LE();
            var entriesCount = (int)reader.ReadUInt32LE();
            var isWalls = fileType == 5;
            var isStairs = fileType == 1;

            for (var e = 0; e < entriesCount; e++)
            {
                reader.ReadUInt32LE(); // category_id
                reader.ReadUInt32LE(); // subcategory_id
                var featureMask = (int)reader.ReadUInt32LE() & HousingTierMask;
                reader.ReadUInt32LE(); // cliloc_id

                // fields_1
                var fieldsCount1 = (int)reader.ReadUInt32LE();
                for (var i = 0; i < fieldsCount1; i++)
                {
                    reader.ReadUInt32LE(); // direction
                    var staticId = (int)reader.ReadUInt32LE();

                    if (staticId > 0 && staticId < _itemTable.Length)
                    {
                        _itemTable[staticId] = featureMask;
                    }
                }

                // unknown1 (only for walls, between fields_1 and fields_count_2)
                if (isWalls)
                {
                    reader.ReadUInt32LE();
                }

                // fields_2
                var fieldsCount2 = (int)reader.ReadUInt32LE();
                for (var i = 0; i < fieldsCount2; i++)
                {
                    reader.ReadUInt32LE(); // direction
                    var staticId = (int)reader.ReadUInt32LE();

                    if (isStairs)
                    {
                        if (staticId > 0 && staticId < _multiTable.Length)
                        {
                            _multiTable[staticId] = featureMask;
                        }
                    }
                    else if (staticId > 0 && staticId < _itemTable.Length)
                    {
                        _itemTable[staticId] = featureMask;
                    }
                }

                // unknown2 (only for non-walls, after fields_2)
                if (!isWalls)
                {
                    reader.ReadUInt32LE();
                }
            }
        }
    }

    private static void LoadFromTxtFiles()
    {
        LoadItems(
            "walls.txt",
            "South1", "South2", "South3", "Corner",
            "East1", "East2", "East3", "Post",
            "WindowS", "AltWindowS", "WindowE", "AltWindowE",
            "SecondAltWindowS", "SecondAltWindowE"
        );
        LoadItems(
            "teleprts.txt",
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8",
            "F9", "F10", "F11", "F12", "F13", "F14", "F15", "F16"
        );
        LoadItems(
            "stairs.txt",
            "Block", "North", "East", "South", "West",
            "Squared1", "Squared2", "Rounded1", "Rounded2"
        );
        LoadItems(
            "roof.txt",
            "North", "East", "South", "West",
            "NSCrosspiece", "EWCrosspiece",
            "NDent", "EDent", "SDent", "WDent",
            "NTPiece", "ETPiece", "STPiece", "WTPiece",
            "XPiece", "Extra Piece"
        );
        LoadItems(
            "floors.txt",
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8",
            "F9", "F10", "F11", "F12", "F13", "F14", "F15", "F16"
        );
        LoadItems(
            "misc.txt",
            "Piece1", "Piece2", "Piece3", "Piece4",
            "Piece5", "Piece6", "Piece7", "Piece8"
        );
        LoadItems(
            "doors.txt",
            "Piece1", "Piece2", "Piece3", "Piece4",
            "Piece5", "Piece6", "Piece7", "Piece8"
        );

        LoadMultis("stairs.txt", "MultiNorth", "MultiEast", "MultiSouth", "MultiWest");
    }

    private static string ResolveTxtPath(string filename)
    {
        var clientPath = Core.FindDataFile(filename, false);
        if (clientPath != null)
        {
            return clientPath;
        }

        var bundledPath = Path.Combine("Data", "Components", filename);
        return File.Exists(bundledPath) ? bundledPath : null;
    }

    private static void LoadItems(string filename, params ReadOnlySpan<string> itemColumns) =>
        LoadSpreadsheet(_itemTable, filename, itemColumns);

    private static void LoadMultis(string filename, params ReadOnlySpan<string> multiColumns) =>
        LoadSpreadsheet(_multiTable, filename, multiColumns);

    private static void LoadSpreadsheet(int[] table, string filename, params ReadOnlySpan<string> tileColumns)
    {
        var path = ResolveTxtPath(filename);
        if (path == null)
        {
            return;
        }

        Spreadsheet ss;

        try
        {
            ss = new Spreadsheet(path);
        }
        catch (Exception ex)
        {
            // One unreadable sheet must not take the others down with it.
            logger.Error(ex, "Could not read house components from {Path}", path);
            return;
        }

        // GetInt32 on a missing column yields NoFeatureRequired, which would register every piece in
        // the sheet as unconditionally placeable. Refuse the sheet instead.
        var featureCID = ss.GetColumnID(FeatureMaskColumn);
        if (featureCID < 0)
        {
            logger.Error(
                "House component sheet {Path} has no {Column} column. Its pieces will not be registered",
                path,
                FeatureMaskColumn
            );
            return;
        }

        // An individual missing column is expected - older sheets predate walls.txt's
        // SecondAltWindowS/E - but a sheet matching none of them is not the sheet we expect.
        var tileCIDs = new int[tileColumns.Length];
        var matchedColumns = 0;

        for (var i = 0; i < tileColumns.Length; ++i)
        {
            tileCIDs[i] = ss.GetColumnID(tileColumns[i]);

            if (tileCIDs[i] >= 0)
            {
                matchedColumns++;
            }
        }

        if (matchedColumns == 0)
        {
            logger.Error(
                "House component sheet {Path} has none of its expected tile columns. Its pieces will not be registered",
                path
            );
            return;
        }

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

    private static int[] CreateTable(int length)
    {
        var table = new int[length];

        for (var i = 0; i < table.Length; ++i)
        {
            table[i] = NotAComponent;
        }

        return table;
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

        if (types == null || names == null)
        {
            throw new InvalidDataException($"House component sheet '{path}' is missing its header rows.");
        }

        m_Columns = new ColumnInfo[types.Length];

        for (var i = 0; i < m_Columns.Length; ++i)
        {
            // A names row shorter than the types row leaves the extras unnamed, so nothing resolves
            // to them.
            m_Columns[i] = new ColumnInfo(i, types[i], i < names.Length ? names[i] : "");
        }

        var records = new List<DataRecord>();

        while (ReadLine(ip) is { } values)
        {
            var data = new object[m_Columns.Length];

            for (var i = 0; i < m_Columns.Length; ++i)
            {
                var ci = m_Columns[i];

                // Client sheets write an empty trailing Comment as a plain newline, leaving the row
                // one field short. The client only requires the columns up to FeatureMask, so treat
                // the missing field as empty rather than dropping a row that lists real pieces.
                var value = ci.m_DataIndex < values.Length ? values[ci.m_DataIndex] : null;

                data[i] = ci.m_Type switch
                {
                    "int"    => Utility.ToInt32(value),
                    "string" => value,
                    _        => data[i]
                };
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

    private static string[] ReadLine(StreamReader ip)
    {
        while (ip.ReadLine() is { } line)
        {
            // Whitespace-only, not merely empty: the retail client's doors.txt separates its header
            // rows with lines of bare tabs, and accepting one as the names row leaves every column
            // unnamed, so no door resolves. The client skips them the same way.
            if (!string.IsNullOrWhiteSpace(line))
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

    public int GetInt32(int id) => GetInt32(this[id]);

    public int GetInt32(object obj) => Convert.ToInt32(obj);
}
