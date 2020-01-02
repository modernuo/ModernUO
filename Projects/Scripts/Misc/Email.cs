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
    public static readonly MailboxAddress FromAddress = null; //new MailboxAddress("Joey Tribbiani", "joey@friends.com");
    public static readonly MailboxAddress CrashAddresses = null; //new MailboxAddress("Joey Tribbiani", "joey@friends.com");
    public static readonly MailboxAddress SpeechLogPageAddresses = null; //new MailboxAddress("Joey Tribbiani", "joey@friends.com");
    public static readonly string EmailServer = null; //"smtp.friends.com";
    public static readonly int EmailPort = 0; //25;
    public static readonly string EmailServerUsername = null; //joe
    public static readonly string EmailServerPassword = null; //password
    public static readonly int m_retryCount = 5;

    //private static Regex m_pattern = new Regex(@"^[a-z0-9.+_-]+@([a-z0-9-]+\.)+[a-z]+$",
    //  RegexOptions.Compiled | RegexOptions.IgnoreCase);



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
      message.To.Add(SpeechLogPageAddresses);
      message.Subject = "ModernUO Speech Log Page Forwarding";
      //=========================================================[BODY]
      //=========================================================[BODY]
      using (StringWriter writer = new StringWriter())
      {
        writer.WriteLine("RunUO Speech Log Page - {0}", pageType);
        writer.WriteLine();

        writer.WriteLine("From: '{0}', Account: '{1}'", sender.RawName,
          sender.Account is Account ? sender.Account.Username : "???");
        writer.WriteLine("Location: {0} [{1}]", sender.Location, sender.Map);
        writer.WriteLine("Sent on: {0}/{1:00}/{2:00} {3}:{4:00}:{5:00}", time.Year, time.Month, time.Day, time.Hour,
          time.Minute, time.Second);
        writer.WriteLine();

        writer.WriteLine("Message:");
        writer.WriteLine("'{0}'", entry.Message);
        writer.WriteLine();

        writer.WriteLine("Speech Log");
        writer.WriteLine("==========");

        foreach (SpeechLogEntry logEntry in entry.SpeechLog)
        {
          Mobile from = logEntry.From;
          string fromName = from.RawName;
          string fromAccount = from.Account is Account ? from.Account.Username : "???";
          DateTime created = logEntry.Created;
          string speech = logEntry.Speech;

          writer.WriteLine("{0}:{1:00}:{2:00} - {3} ({4}): '{5}'", created.Hour, created.Minute, created.Second,
            fromName, fromAccount, speech);
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
      message.Subject = "Automated RunUO Crash Report";
      //=========================================================[BODY]
      //=========================================================[BODY]
      var builder = new BodyBuilder
      {
        TextBody = "Automated RunUO Crash Report. See attachment for details.",
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

            // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync(EmailServer, EmailPort, false).ConfigureAwait(false);
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
            Console.WriteLine("Failure sending e-mail '{0}' to '{1}'.", message.Subject, message.To);
            throw;
          }
          await Task.Delay(count * 1000);
        }
      }

    }
  }
}
