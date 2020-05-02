using System.IO;
using System.Text.Json.Serialization;
using MimeKit;
using Server.Json;

namespace Server.Configurations
{
  public static class EmailConfiguration
  {
    public static readonly bool EmailEnabled;
    public static readonly MailboxAddress FromAddress;
    public static readonly MailboxAddress CrashAddress;
    public static readonly MailboxAddress SpeechLogPageAddress;
    public static readonly string EmailServer;
    public static readonly int EmailPort;
    public static readonly string EmailServerUsername;
    public static readonly string EmailServerPassword;
    public static readonly int EmailSendRetryCount = 5; // seconds
    public static readonly int EmailSendRetryDelay = 2; // seconds

    static EmailConfiguration()
    {
      string filePath = Path.Join(Core.BaseDirectory, "Configuration/email-settings.json");
      Settings settings = JsonConfig.Deserialize<Settings>(filePath) ?? new Settings();

      if (settings.emailServer == null || settings.fromAddress == null)
      {
        JsonConfig.Serialize(filePath, settings);
        return;
      }

      EmailEnabled = true;
      FromAddress = new MailboxAddress(settings.fromName, settings.fromAddress);
      CrashAddress = new MailboxAddress(settings.crashName, settings.crashAddress);
      SpeechLogPageAddress = new MailboxAddress(settings.speechLogPageName, settings.speechLogPageAddress);
      EmailServer = settings.emailServer;
      EmailPort = settings.emailPort;
      EmailServerUsername = settings.emailUsername;
      EmailServerPassword = settings.emailPassword;
    }

    internal class Settings
    {
      [JsonPropertyName("fromAddress")]
      internal string fromAddress { get; set; }

      [JsonPropertyName("fromName")]
      internal string fromName { get; set; }

      [JsonPropertyName("crashAddress")]
      internal string crashAddress { get; set; }

      [JsonPropertyName("crashName")]
      internal string crashName { get; set; }

      [JsonPropertyName("speechLogPageAddress")]
      internal string speechLogPageAddress { get; set; }

      [JsonPropertyName("speechLogPageName")]
      internal string speechLogPageName { get; set; }

      [JsonPropertyName("emailServer")]
      internal string emailServer { get; set; }

      [JsonPropertyName("emailPort")]
      internal int emailPort { get; set; }

      [JsonPropertyName("emailUsername")]
      internal string emailUsername { get; set; }

      [JsonPropertyName("emailPassword")]
      internal string emailPassword { get; set; }
    }
  }
}
