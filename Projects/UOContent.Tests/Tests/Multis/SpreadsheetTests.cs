using System;
using System.IO;
using Server.Multis;
using Xunit;

namespace UOContent.Tests;

public class SpreadsheetTests : IDisposable
{
    // misc.txt's shape: 13 columns, the last being the Comment the client never reads.
    private const string Types = "int\tint\tint\tint\tint\tint\tint\tint\tint\tint\tint\tint\tstring";

    private const string Names =
        "Category\tStyle\tTID\tPiece1\tPiece2\tPiece3\tPiece4\tPiece5\tPiece6\tPiece7\tPiece8\tFeatureMask\tComment";

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"muo-sheet-{Guid.NewGuid():N}.txt");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private Spreadsheet Write(params string[] rows)
    {
        var lines = new string[rows.Length + 2];
        lines[0] = Types;
        lines[1] = Names;
        rows.CopyTo(lines, 2);

        File.WriteAllLines(_path, lines);
        return new Spreadsheet(_path);
    }

    [Fact]
    public void RowMissingTrailingCommentIsStillRead()
    {
        // An empty Comment written without a trailing tab leaves the row one field short.
        var ss = Write("0\t0\t1060056\t44\t0\t41\t40\t42\t0\t43\t29\t8");

        var record = Assert.Single(ss.Records);

        Assert.Equal(44, record.GetInt32(ss.GetColumnID("Piece1")));
        Assert.Equal(29, record.GetInt32(ss.GetColumnID("Piece8")));
        Assert.Equal(8, record.GetInt32(ss.GetColumnID("FeatureMask")));
    }

    [Fact]
    public void ShortRowDoesNotDropLaterRows()
    {
        var ss = Write(
            "0\t0\t1060056\t44\t0\t41\t40\t42\t0\t43\t29\t0",
            "0\t1\t1060057\t45\t0\t0\t0\t0\t0\t0\t0\t0\tFieldstone Arches"
        );

        Assert.Equal(2, ss.Records.Length);
        Assert.Equal(44, ss.Records[0].GetInt32(ss.GetColumnID("Piece1")));
        Assert.Equal(45, ss.Records[1].GetInt32(ss.GetColumnID("Piece1")));
    }

    [Fact]
    public void TabOnlySeparatorLinesAreNotMistakenForHeaderRows()
    {
        // The retail client's doors.txt separates its header rows this way.
        var separator = new string('\t', 12);

        File.WriteAllLines(
            _path,
            [
                Types,
                separator,
                Names,
                separator,
                "0\t0\t1060056\t44\t0\t41\t40\t42\t0\t43\t29\t0\tFieldstone Archways"
            ]
        );

        var ss = new Spreadsheet(_path);

        Assert.Equal(3, ss.GetColumnID("Piece1"));
        Assert.Equal(11, ss.GetColumnID("FeatureMask"));

        var record = Assert.Single(ss.Records);
        Assert.Equal(44, record.GetInt32(ss.GetColumnID("Piece1")));
    }

    [Fact]
    public void MissingHeaderRowsThrowsInsteadOfNullReference()
    {
        File.WriteAllLines(_path, [Types]);

        Assert.Throws<InvalidDataException>(() => new Spreadsheet(_path));
    }

    [Fact]
    public void HeaderWithFewerNamesThanTypesIsTolerated()
    {
        File.WriteAllLines(
            _path,
            [
                Types,
                "Category\tStyle\tTID\tPiece1",
                "0\t0\t1060056\t44\t0\t41\t40\t42\t0\t43\t29\t0\tFieldstone Archways"
            ]
        );

        var ss = new Spreadsheet(_path);

        Assert.Equal(44, ss.Records[0].GetInt32(ss.GetColumnID("Piece1")));
        Assert.Equal(-1, ss.GetColumnID("FeatureMask"));
    }
}
