/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

        if (File.Exists(path))
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = GC.AllocateUninitializedArray<byte>((int)fs.Length, true);
            fs.Read(buffer);
            // VS_VERSION_INFO (unicode)
            Span<byte> vsVersionInfo = stackalloc byte[]
            {
                0x56, 0x00, 0x53, 0x00, 0x5F, 0x00, 0x56, 0x00,
                0x45, 0x00, 0x52, 0x00, 0x53, 0x00, 0x49, 0x00,
                0x4F, 0x00, 0x4E, 0x00, 0x5F, 0x00, 0x49, 0x00,
                0x4E, 0x00, 0x46, 0x00, 0x4F, 0x00
            };

            var versionIndex = buffer.AsSpan().IndexOf(vsVersionInfo);
            if (versionIndex > -1)
            {
                var offset = versionIndex + 42; // 30 + 12

                var minorPart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset));
                var majorPart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 2));
                var privatePart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 4));
                var buildPart = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 6));

                return new ClientVersion(majorPart, minorPart, buildPart, privatePart);
            }
        }

        return null;
    }

    public record CUOSettings
    {
        [JsonPropertyName("clientversion")]
        public ClientVersion ClientVersion { get; set; }

        [JsonPropertyName("ultimaonlinedirectory")]
        public string UltimaOnlineDirectory { get; set; }
    }
}
