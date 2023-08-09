/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EmailConfiguration.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MimeKit;
using Server.Json;
using Server.Logging;

namespace Server.Configurations
{
    public static class EmailConfiguration
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(EmailConfiguration));

        private const string m_RelPath = "Configuration/email-settings.json";

        public static MailboxAddress CrashAddress { get; private set; }
        public static bool EmailEnabled { get; private set; }
        public static int EmailPort { get; private set; }
        public static int EmailSendRetryCount { get; private set; } // seconds
        public static int EmailSendRetryDelay { get; private set; } // seconds
        public static string EmailServer { get; private set; }
        public static string EmailServerUsername { get; private set; }
        public static string EmailServerPassword { get; private set; }
        public static MailboxAddress FromAddress { get; private set; }
        public static MailboxAddress SpeechLogPageAddress { get; private set; }

        public static void Configure()
        {
            var path = Path.Join(Core.BaseDirectory, m_RelPath);

            Settings settings;

            if (File.Exists(path))
            {
                settings = JsonConfig.Deserialize<Settings>(path);

                if (settings == null)
                {
                    logger.Error("Failed reading email configuration from {Path}", m_RelPath);
                    throw new JsonException($"Failed to deserialize {path}.");
                }

                logger.Information("Email configuration read from {Path}", m_RelPath);
            }
            else
            {
                settings = new Settings();
                JsonConfig.Serialize(path, settings);
                logger.Information("Email configuration saved to {}.", m_RelPath);
            }

            EmailEnabled = settings.enabled;
            FromAddress = new MailboxAddress(settings.fromName, settings.fromAddress);
            CrashAddress = new MailboxAddress(settings.crashName, settings.crashAddress);
            SpeechLogPageAddress = new MailboxAddress(settings.speechLogPageName, settings.speechLogPageAddress);
            EmailServer = settings.emailServer;
            EmailPort = settings.emailPort;
            EmailServerUsername = settings.emailUsername;
            EmailServerPassword = settings.emailPassword;
            EmailSendRetryCount = settings.emailSendRetryCount;
            EmailSendRetryDelay = settings.emailSendRetryDelay;
        }

        public class Settings
        {
            [JsonPropertyName("enabled")]
            public bool enabled { get; set; } = false;

            [JsonPropertyName("fromAddress")]
            public string fromAddress { get; set; } = "support@modernuo.com";

            [JsonPropertyName("fromName")]
            public string fromName { get; set; } = "ModernUO Team";

            [JsonPropertyName("crashAddress")]
            public string crashAddress { get; set; } = "crashes@modernuo.com";

            [JsonPropertyName("crashName")]
            public string crashName { get; set; } = "Crash Log";

            [JsonPropertyName("speechLogPageAddress")]
            public string speechLogPageAddress { get; set; } = "support@modernuo.com";

            [JsonPropertyName("speechLogPageName")]
            public string speechLogPageName { get; set; } = "GM Support Conversation";

            [JsonPropertyName("emailServer")]
            public string emailServer { get; set; } = "smtp.gmail.com";

            [JsonPropertyName("emailPort")]
            public int emailPort { get; set; } = 465;

            [JsonPropertyName("emailUsername")]
            public string emailUsername { get; set; } = "support@modernuo.com";

            [JsonPropertyName("emailPassword")]
            public string emailPassword { get; set; } = "Some Password 123";

            [JsonPropertyName("emailSendRetryCount")]
            public int emailSendRetryCount { get; set; } = 5;

            [JsonPropertyName("emailSendRetryDelay")]
            public int emailSendRetryDelay { get; set; } = 3;
        }
    }
}
