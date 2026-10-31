/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: UOClient.cs                                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;
using Server.Logging;

namespace Server;

public static class UOClient
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(UOClient));

    private static bool _automaticallyDetected;

    public static CUOSettings CuoSettings { get; private set; }
    public static ClientVersion ServerClientVersion { get; private set; }
    public static ClientVersion MinRequired { get; set; }
    public static ClientVersion MaxRequired { get; set; }

    public static void Load()
    {
        ServerClientVersion = ServerConfiguration.GetSetting("clientData.clientVersion", (ClientVersion)null);

        if (ServerClientVersion == null)
        {
            ServerClientVersion = DetectCUOClient() ?? DetectClassicClient();
            _automaticallyDetected = true;
        }
    }

    public static void Configure()
    {
        if (ServerClientVersion == null)
        {
            logger.Warning("Could not detect client version. This may cause data files to load improperly.");
            return;
        }

        if (_automaticallyDetected)
        {
            logger.Information(
                CuoSettings?.ClientVersion == ServerClientVersion
                    ? "Automatically detected client version {ServerClientVersion} from CUO settings."
                    : "Automatically detected client version {ServerClientVersion}",
                ServerClientVersion
            );
            return;
        }

        logger.Information("Manually configured to use client version {ServerClientVersion}", ServerClientVersion);
    }

    private static ClientVersion DetectCUOClient()
    {
        var path = Core.FindDataFile("settings.json", false);
        if (File.Exists(path))
        {
            var settings = JsonConfig.Deserialize<CUOSettings>(path);
            var file = new FileInfo(path);

            if (settings.UltimaOnlineDirectory != null)
            {
                settings.UltimaOnlineDirectory = PathUtility.GetFullPath(settings.UltimaOnlineDirectory, file.DirectoryName);
                if (Directory.Exists(settings.UltimaOnlineDirectory))
                {
                    CuoSettings = settings;
                }
            }

            return settings.ClientVersion;
        }

        return null;
    }

    private static ClientVersion DetectClassicClient()
    {
        var path = Core.FindDataFile("client.exe", false);

        return File.Exists(path) ? DetectClassicClient(File.ReadAllBytes(path)) : null;
    }

    internal static ClientVersion DetectClassicClient(ReadOnlySpan<byte> exe) =>
        DetectFromVersionInfo(exe) ?? DetectFromVersionFormat(exe);

    private static ClientVersion DetectFromVersionInfo(ReadOnlySpan<byte> exe)
    {
        // VS_VERSION_INFO (unicode)
        var versionIndex = exe.IndexOf("V\0S\0_\0V\0E\0R\0S\0I\0O\0N\0_\0I\0N\0F\0O\0"u8);
        if (versionIndex < 0 || versionIndex + 50 > exe.Length)
        {
            return null;
        }

        var offset = versionIndex + 42; // 30 + 12

        var minorPart = BinaryPrimitives.ReadUInt16LittleEndian(exe[offset..]);
        var majorPart = BinaryPrimitives.ReadUInt16LittleEndian(exe[(offset + 2)..]);
        var privatePart = BinaryPrimitives.ReadUInt16LittleEndian(exe[(offset + 4)..]);
        var buildPart = BinaryPrimitives.ReadUInt16LittleEndian(exe[(offset + 6)..]);

        return new ClientVersion(majorPart, minorPart, buildPart, privatePart);
    }

    // Older clients (4.0.11c and later 4.x/5.0.x builds) have no version resource. They build the version string at
    // startup with sprintf("%d.%d.%d%s", major, minor, revision, letter), so the parts are the operands of the pushes
    // just before the format string's push:
    //   68 <letter VA>   6A <revision>   6A <minor>   6A <major>   68 <format VA>
    private static ClientVersion DetectFromVersionFormat(ReadOnlySpan<byte> exe)
    {
        var formatOffset = exe.IndexOf("%d.%d.%d%s\0"u8);
        if (formatOffset < 0 || !TryOffsetToVirtualAddress(exe, formatOffset, out var formatVa))
        {
            return null;
        }

        Span<byte> pushFormat = stackalloc byte[5];
        pushFormat[0] = 0x68;
        BinaryPrimitives.WriteUInt32LittleEndian(pushFormat[1..], formatVa);

        var pushOffset = exe.IndexOf(pushFormat);
        if (pushOffset < 11 || exe[(pushOffset + 1)..].IndexOf(pushFormat) >= 0)
        {
            return null; // Missing or ambiguous
        }

        var pushes = exe.Slice(pushOffset - 11, 11);
        if (pushes[0] != 0x68 || pushes[5] != 0x6A || pushes[7] != 0x6A || pushes[9] != 0x6A)
        {
            return null;
        }

        var letterVa = BinaryPrimitives.ReadUInt32LittleEndian(pushes[1..]);
        if (!TryVirtualAddressToOffset(exe, letterVa, out var letterOffset))
        {
            return null;
        }

        var letter = exe[letterOffset..];
        var letterLength = letter.IndexOf((byte)0);
        if (letterLength is < 0 or > 1 || letterLength == 1 && !char.IsAsciiLetterLower((char)letter[0]))
        {
            return null;
        }

        var suffix = letterLength == 1 ? ((char)letter[0]).ToString() : "";
        return new ClientVersion($"{pushes[10]}.{pushes[8]}.{pushes[6]}{suffix}");
    }

    private static bool TryOffsetToVirtualAddress(ReadOnlySpan<byte> exe, int offset, out uint virtualAddress)
    {
        foreach (var (sectionVa, rawPointer, rawSize) in GetSections(exe, out var imageBase))
        {
            if (offset >= rawPointer && offset < rawPointer + rawSize)
            {
                virtualAddress = imageBase + sectionVa + (uint)(offset - rawPointer);
                return true;
            }
        }

        virtualAddress = 0;
        return false;
    }

    private static bool TryVirtualAddressToOffset(ReadOnlySpan<byte> exe, uint virtualAddress, out int offset)
    {
        foreach (var (sectionVa, rawPointer, rawSize) in GetSections(exe, out var imageBase))
        {
            var start = imageBase + sectionVa;
            if (virtualAddress >= start && virtualAddress < start + rawSize && rawPointer + (virtualAddress - start) < exe.Length)
            {
                offset = (int)(rawPointer + (virtualAddress - start));
                return true;
            }
        }

        offset = 0;
        return false;
    }

    // PE32 section headers that fit in the file; empty if not a PE32 image.
    private static (uint VirtualAddress, uint RawPointer, uint RawSize)[] GetSections(
        ReadOnlySpan<byte> exe, out uint imageBase
    )
    {
        imageBase = 0;
        if (exe.Length < 0x40)
        {
            return [];
        }

        var pe = BinaryPrimitives.ReadInt32LittleEndian(exe[0x3C..]);
        if (pe < 0 || pe + 24 + 32 > exe.Length || BinaryPrimitives.ReadUInt32LittleEndian(exe[pe..]) != 0x00004550 ||
            BinaryPrimitives.ReadUInt16LittleEndian(exe[(pe + 24)..]) != 0x10B) // "PE\0\0", PE32 optional header
        {
            return [];
        }

        var count = BinaryPrimitives.ReadUInt16LittleEndian(exe[(pe + 6)..]);
        var optionalSize = BinaryPrimitives.ReadUInt16LittleEndian(exe[(pe + 20)..]);
        var table = pe + 24 + optionalSize;
        if (table >= exe.Length)
        {
            return [];
        }

        imageBase = BinaryPrimitives.ReadUInt32LittleEndian(exe[(pe + 24 + 28)..]);

        var sections = new (uint, uint, uint)[Math.Min(count, (exe.Length - table) / 40)];
        for (var i = 0; i < sections.Length; i++)
        {
            var section = exe.Slice(table + i * 40, 40);
            sections[i] = (
                BinaryPrimitives.ReadUInt32LittleEndian(section[12..]),
                BinaryPrimitives.ReadUInt32LittleEndian(section[20..]),
                BinaryPrimitives.ReadUInt32LittleEndian(section[16..])
            );
        }

        return sections;
    }

    public record CUOSettings
    {
        [JsonPropertyName("clientversion")]
        public ClientVersion ClientVersion { get; set; }

        [JsonPropertyName("ultimaonlinedirectory")]
        public string UltimaOnlineDirectory { get; set; }
    }
}
