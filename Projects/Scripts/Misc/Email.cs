using MailKit.Net.Smtp;
using MimeKit;
using Server.Accounting;
using Server.Engines.Help;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Misc
{
  public class Email
  {
    public static readonly MailboxAddress FromAddress = new MailboxAddress(Configuration.Instance.emailSettings.FromName, Configuration.Instance.emailSettings.FromAddress);
    public static readonly MailboxAddress CrashAddresses = new MailboxAddress(Configuration.Instance.emailSettings.crashName, Configuration.Instance.emailSettings.crashAddress);
    public static readonly MailboxAddress SpeechLogPageAddress = new MailboxAddress(Configuration.Instance.emailSettings.speechLogPageName, Configuration.Instance.emailSettings.speechLogPageAddress);
    public static readonly string EmailServer = Configuration.Instance.emailSettings.emailServer;
    public static readonly int EmailPort = Configuration.Instance.emailSettings.emailPort;
    public static readonly string EmailServerUsername = Configuration.Instance.emailSettings.emailUsername;
    public static readonly string EmailServerPassword = Configuration.Instance.emailSettings.emailPassword;
    public static readonly int m_retryCount = 5;

    /// <summary>
    /// Sends Queue-Page request using Email
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="pageType"></param>
    public static void SendQueueEmail(PageEntry entry, string pageType)
    {
      Mobile sender = entry.Sender;
      DateTime time = DateTime.UtcNow;
      //=========================================================[HEADER]
      //=========================================================[HEADER]
      var message = new MimeMessage();
      message.To.Add(SpeechLogPageAddress);
      message.Subject = "ModernUO Speech Log Page Forwarding";
      //=========================================================[BODY]
      //=========================================================[BODY]
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
          string fromAccount = (from.Account is Account accFrom) ? accFrom.Username : "???";
          DateTime created = logEntry.Created;
          string speech = logEntry.Speech;
          writer.WriteLine(@$"{created.Hour}:{created.Minute:00}:{created.Second:00} - {fromName} ({fromAccount}): '{speech}'");
        }
        var builder = new BodyBuilder
        {
          TextBody = writer.ToString(),
          HtmlBody = null
        };
        message.Body = builder.ToMessageBody();

      }
      SendAsync(message);

    }

    /// <summary>
    /// Sends crash email
    /// </summary>
    /// <param name="filePath"></param>
    public static void SendCrashEmail(string filePath)
    {
      //=========================================================[HEADER]
      //=========================================================[HEADER]
      var message = new MimeMessage();
      message.To.Add(CrashAddresses);
      message.Subject = "Automated ModernUO Crash Report";
      //=========================================================[BODY]
      //=========================================================[BODY]
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
      string messageID = $"<{now.ToString("yyyyMMdd")}.{now.ToString("HHmmssff")}@{EmailServer}>";
      message.Headers.Add("Message-ID", messageID);
      message.From.Add(FromAddress);

      for (var count = 1; count <= m_retryCount; count++)
      {
        try
        {
          using (var client = new SmtpClient())
          {
            await client.ConnectAsync(EmailServer, EmailPort, true).ConfigureAwait(false);
            await client.AuthenticateAsync(EmailServerUsername, EmailServerPassword);
            await client.SendAsync(message).ConfigureAwait(false);
            await client.DisconnectAsync(true).ConfigureAwait(false);
            Console.WriteLine("Sent e-mail '{0}' to '{1}'.", message.Subject, message.To);
            return;
          }
        }
        catch (Exception exception)
        {
          Console.WriteLine(exception);
          if (m_retryCount >= 0)
          {
            Console.WriteLine(exception.StackTrace);
          }
          await Task.Delay(count * 1000);
        }
      }

    }
  }
}
