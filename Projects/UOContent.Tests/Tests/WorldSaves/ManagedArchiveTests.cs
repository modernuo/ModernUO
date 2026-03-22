using System;
using System.IO;
using Server.Compression;
using Xunit;

namespace Server.Tests;

[Collection("Sequential UOContent Tests")]
public class ManagedArchiveTests : IDisposable
{
    private readonly string _testDir;

    public ManagedArchiveTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"modernuo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    private string CreateTestDirectory(string name, params (string fileName, string content)[] files)
    {
        var dir = Path.Combine(_testDir, name);
        Directory.CreateDirectory(dir);

        foreach (var (fileName, content) in files)
        {
            var filePath = Path.Combine(dir, fileName);
            var fileDir = Path.GetDirectoryName(filePath);
            if (fileDir != null)
            {
                Directory.CreateDirectory(fileDir);
            }

            File.WriteAllText(filePath, content);
        }

        return dir;
    }

    [Fact]
    public void CreateTarZstd_SingleDirectory_RoundTrips()
    {
        // Arrange
        var sourceDir = CreateTestDirectory("source",
            ("file1.txt", "Hello World"),
            ("file2.bin", "Binary content here"),
            ("subdir/nested.txt", "Nested file")
        );

        var archivePath = Path.Combine(_testDir, "test.tar.zst");

        // Act
        var entryCount = ManagedArchive.CreateTarZstd(
            [sourceDir], archivePath, _testDir, compressionLevel: 1
        );

        // Assert
        Assert.True(entryCount > 0);
        Assert.True(File.Exists(archivePath));
        Assert.True(new FileInfo(archivePath).Length > 0);
    }

    [Fact]
    public void CreateTarZstd_MultipleDirectories_IncludesAll()
    {
        // Arrange
        var dir1 = CreateTestDirectory("backup1",
            ("items.bin", "items data"),
            ("items.idx", "items index")
        );
        var dir2 = CreateTestDirectory("backup2",
            ("mobiles.bin", "mobiles data"),
            ("mobiles.idx", "mobiles index")
        );

        var archivePath = Path.Combine(_testDir, "multi.tar.zst");

        // Act
        var entryCount = ManagedArchive.CreateTarZstd(
            [dir1, dir2], archivePath, _testDir, compressionLevel: 1
        );

        // Assert
        Assert.Equal(4, entryCount);
    }

    [Fact]
    public void ExtractTarZstd_RecreatesFiles()
    {
        // Arrange
        var sourceDir = CreateTestDirectory("source",
            ("data.txt", "Test data content"),
            ("config/settings.json", "{\"key\":\"value\"}")
        );

        var archivePath = Path.Combine(_testDir, "extract-test.tar.zst");
        ManagedArchive.CreateTarZstd([sourceDir], archivePath, _testDir, compressionLevel: 1);

        var extractDir = Path.Combine(_testDir, "extracted");

        // Act
        var success = ManagedArchive.ExtractTarZstd(archivePath, extractDir);

        // Assert
        Assert.True(success);
        Assert.True(Directory.Exists(extractDir));

        // Verify extracted content matches
        var extractedDataPath = Path.Combine(extractDir, "source", "data.txt");
        Assert.True(File.Exists(extractedDataPath));
        Assert.Equal("Test data content", File.ReadAllText(extractedDataPath));

        var extractedConfigPath = Path.Combine(extractDir, "source", "config", "settings.json");
        Assert.True(File.Exists(extractedConfigPath));
        Assert.Equal("{\"key\":\"value\"}", File.ReadAllText(extractedConfigPath));
    }

    [Fact]
    public void CountEntries_MatchesCreateCount()
    {
        // Arrange
        var sourceDir = CreateTestDirectory("source",
            ("a.txt", "a"),
            ("b.txt", "b"),
            ("c/d.txt", "d"),
            ("c/e.txt", "e")
        );

        var archivePath = Path.Combine(_testDir, "count-test.tar.zst");
        var createCount = ManagedArchive.CreateTarZstd(
            [sourceDir], archivePath, _testDir, compressionLevel: 1
        );

        // Act
        var verifyCount = ManagedArchive.CountEntries(archivePath);

        // Assert
        Assert.Equal(createCount, verifyCount);
    }

    [Fact]
    public void CreateTarZstd_EmptyDirectory_ReturnsZero()
    {
        // Arrange
        var sourceDir = Path.Combine(_testDir, "empty");
        Directory.CreateDirectory(sourceDir);

        var archivePath = Path.Combine(_testDir, "empty.tar.zst");

        // Act
        var entryCount = ManagedArchive.CreateTarZstd(
            [sourceDir], archivePath, _testDir, compressionLevel: 1
        );

        // Assert
        Assert.Equal(0, entryCount);
    }

    [Fact]
    public void CreateTarZstd_NonexistentSource_SkipsGracefully()
    {
        // Arrange
        var archivePath = Path.Combine(_testDir, "nonexistent.tar.zst");

        // Act
        var entryCount = ManagedArchive.CreateTarZstd(
            [Path.Combine(_testDir, "does-not-exist")], archivePath, _testDir, compressionLevel: 1
        );

        // Assert
        Assert.Equal(0, entryCount);
    }

    [Fact]
    public void CreateTarZstd_TempFileCleanedUp()
    {
        // Arrange
        var sourceDir = CreateTestDirectory("source", ("file.txt", "content"));
        var archivePath = Path.Combine(_testDir, "cleanup-test.tar.zst");

        // Act
        ManagedArchive.CreateTarZstd([sourceDir], archivePath, _testDir, compressionLevel: 1);

        // Assert - no .tmp file should remain
        Assert.False(File.Exists(archivePath + ".tmp"));
        Assert.True(File.Exists(archivePath));
    }

    [Fact]
    public void ExtractTarZstd_InvalidFile_ReturnsFalse()
    {
        // Arrange
        var badFile = Path.Combine(_testDir, "bad.tar.zst");
        File.WriteAllText(badFile, "this is not a valid archive");
        var extractDir = Path.Combine(_testDir, "extract-bad");

        // Act
        var result = ManagedArchive.ExtractTarZstd(badFile, extractDir);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CountEntries_InvalidFile_ReturnsNegative()
    {
        // Arrange
        var badFile = Path.Combine(_testDir, "bad.tar.zst");
        File.WriteAllText(badFile, "not a valid archive");

        // Act
        var count = ManagedArchive.CountEntries(badFile);

        // Assert
        Assert.Equal(-1, count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void CreateTarZstd_DifferentCompressionLevels_AllWork(int level)
    {
        // Arrange
        var sourceDir = CreateTestDirectory($"level-{level}", ("data.txt", new string('X', 10000)));
        var archivePath = Path.Combine(_testDir, $"level-{level}.tar.zst");

        // Act
        var entryCount = ManagedArchive.CreateTarZstd(
            [sourceDir], archivePath, _testDir, compressionLevel: level
        );

        // Assert
        Assert.Equal(1, entryCount);
        Assert.True(File.Exists(archivePath));

        // Verify we can extract it back
        var extractDir = Path.Combine(_testDir, $"extract-level-{level}");
        Assert.True(ManagedArchive.ExtractTarZstd(archivePath, extractDir));
    }
}
