using MailKit.Net.Smtp;
using MimeKit;
using Server.Accounting;
using Server.Engines.Help;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Server.Misc
{
  public static class Email
  {
    public static readonly MailboxAddress FROM_ADDRESS = new MailboxAddress(Configuration.Instance.emailSettings.FromName, Configuration.Instance.emailSettings.FromAddress);
    public static readonly MailboxAddress CRASH_ADDRESS = new MailboxAddress(Configuration.Instance.emailSettings.crashName, Configuration.Instance.emailSettings.crashAddress);
    public static readonly MailboxAddress SPEECH_LOG_PAGE_ADDRESS = new MailboxAddress(Configuration.Instance.emailSettings.speechLogPageName, Configuration.Instance.emailSettings.speechLogPageAddress);
    public static readonly string EMAIL_SERVER = Configuration.Instance.emailSettings.emailServer;
    public static readonly int EMAIL_PORT = Configuration.Instance.emailSettings.emailPort;
    public static readonly string EMAIL_SERVER_USERNAME = Configuration.Instance.emailSettings.emailUsername;
    public static readonly string EMAIL_SERVER_PASSWORD = Configuration.Instance.emailSettings.emailPassword;
    public static readonly int RETRY_SEND_EMAIL_COUNT = 5;
    public static readonly int SEND_DELAY_SECONDS = 2;

    /// <summary>
    /// Sends Queue-Page request using Email
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="pageType"></param>
    public static void SendQueueEmail(PageEntry entry, string pageType)
    {
      Mobile sender = entry.Sender;
      DateTime time = DateTime.UtcNow;

      var message = new MimeMessage();
      message.From.Add(FROM_ADDRESS);
      message.To.Add(SPEECH_LOG_PAGE_ADDRESS);
      message.Subject = "ModernUO Speech Log Page Forwarding";

      using (StringWriter writer = new StringWriter())
      {
        writer.WriteLine(@$"
          ModernUO Speech Log Page - {pageType}

          From: '{sender.RawName}', Account: '{((sender.Account is Account accSend) ? accSend.Username : " ??? ")}'

          Location: {sender.Location} [{sender.Map}]
          Sent on: {time.Year}/{time.Month:00}/{time.Day:00} {time.Hour}:{time.Minute:00}:{time.Second:00}

          Message:
          '{entry.Message}'

          Speech Log
          ==========
        ");

        foreach (SpeechLogEntry logEntry in entry.SpeechLog)
        {
          Mobile from = logEntry.From;
          string fromName = from.RawName;
          string fromAccount = from.Account is Account accFrom ? accFrom.Username : "???";
          DateTime created = logEntry.Created;
          string speech = logEntry.Speech;
          writer.WriteLine(@$"{created.Hour}:{created.Minute:00}:{created.Second:00} - {fromName} ({fromAccount}): '{speech}'");
        }

        message.Body = new BodyBuilder
        {
          TextBody = writer.ToString(),
          HtmlBody = null
        }.ToMessageBody();

      }
      SendAsync(message);
    }

    /// <summary>
    /// Sends crash email
    /// </summary>
    /// <param name="filePath"></param>
    public static void SendCrashEmail(string filePath)
    {
      var message = new MimeMessage();
      message.From.Add(FROM_ADDRESS);
      message.To.Add(CRASH_ADDRESS);
      message.Subject = "Automated ModernUO Crash Report";
      var builder = new BodyBuilder
      {
        TextBody = "Automated ModernUO Crash Report. See attachment for details.",
        HtmlBody = null
      };
      builder.Attachments.Add(filePath);
      message.Body = builder.ToMessageBody();
    }

    /// <summary>
    /// Sends emails async
    /// </summary>
    /// <param name="message"></param>
    private static async void SendAsync(MimeMessage message)
    {
      DateTime now = DateTime.UtcNow;
      string messageID = $"<{now:yyyyMMdd}.{now:HHmmssff}@{EMAIL_SERVER}>";
      message.Headers.Add("Message-ID", messageID);
      message.From.Add(FROM_ADDRESS);

      int delay = SEND_DELAY_SECONDS;

      for (int i = 0; i < RETRY_SEND_EMAIL_COUNT; i++)
        try
        {
          using SmtpClient client = new SmtpClient();
          await client.ConnectAsync(EMAIL_SERVER, EMAIL_PORT, true);
          await client.AuthenticateAsync(EMAIL_SERVER_USERNAME, EMAIL_SERVER_PASSWORD);
          await client.SendAsync(message);
          await client.DisconnectAsync(true);
          return;
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.Message);
          Console.WriteLine(ex.StackTrace);
          delay *= delay;

          await Task.Delay(delay * 1000);
        }
    }
  }
}
