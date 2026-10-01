using System;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Server.Accounting;
using Server.Configurations;
using Server.Engines.Help;
using Server.Logging;

namespace Server.Misc
{
    public static class Email
    {
        private static readonly ILogger _logger = LogFactory.GetLogger(typeof(Email));

        // How long SendCrashEmail holds up the exit after a crash: at the default settings, room for the first
        // attempts (3 and 6 s apart; a fourth starts at 21 s when they fail fast), and no longer when the SMTP
        // server is dead.
        private static readonly TimeSpan CrashEmailTimeout = TimeSpan.FromSeconds(30);

        // A retry wait stops doubling at an hour: no multi-day waits, and nothing left to overflow.
        private const int MaxRetryDelaySeconds = 3600;

        /// <summary>
        ///     Sends Queue-Page request using Email
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="pageType"></param>
        public static void SendQueueEmail(PageEntry entry, string pageType)
        {
            if (!EmailConfiguration.EmailEnabled)
            {
                return;
            }

            var sender = entry.Sender;
            var time = Core.Now;

            var message = new MimeMessage();
            message.From.Add(EmailConfiguration.FromAddress);
            message.To.Add(EmailConfiguration.SpeechLogPageAddress);
            message.Subject = "ModernUO Speech Log Page Forwarding";

            using (var writer = new StringWriter())
            {
                writer.WriteLine(
                    @$"
          ModernUO Speech Log Page - {pageType}

          From: '{sender.RawName}', Account: '{(sender.Account is Account accSend ? accSend.Username : " ??? ")}'

          Location: {sender.Location} [{sender.Map}]
          Sent on: {time.Year}/{time.Month:00}/{time.Day:00} {time.Hour}:{time.Minute:00}:{time.Second:00}

          Message:
          '{entry.Message}'

          Speech Log
          ==========
        "
                );

                foreach (var logEntry in entry.SpeechLog)
                {
                    var from = logEntry.From;
                    var fromName = from.RawName;
                    var fromAccount = from.Account is Account accFrom ? accFrom.Username : "???";
                    var created = logEntry.Created;
                    var speech = logEntry.Speech;
                    writer.WriteLine(
                        $"{created.Hour}:{created.Minute:00}:{created.Second:00} - {fromName} ({fromAccount}): '{speech}'"
                    );
                }

                message.Body = new BodyBuilder
                {
                    TextBody = writer.ToString(),
                    HtmlBody = null
                }.ToMessageBody();
            }

            _ = SendAsync(message);
        }

        /// <summary>
        ///     Sends crash email, blocking for up to the crash timeout (30 s) while it is sent: the core exits
        ///     as soon as the crash handlers return, and a send left running dies with it.
        /// </summary>
        /// <param name="filePath"></param>
        public static void SendCrashEmail(string filePath)
        {
            if (!EmailConfiguration.EmailEnabled)
            {
                return;
            }

            var message = new MimeMessage();
            message.From.Add(EmailConfiguration.FromAddress);
            message.To.Add(EmailConfiguration.CrashAddress);
            message.Subject = "Automated ModernUO Crash Report";
            var builder = new BodyBuilder
            {
                TextBody = "Automated ModernUO Crash Report. See attachment for details.",
                HtmlBody = null
            };
            builder.Attachments.Add(filePath);
            message.Body = builder.ToMessageBody();

            try
            {
                if (!SendAsync(message).Wait(CrashEmailTimeout))
                {
                    _logger.Warning("Crash email not sent after {Timeout}, giving up", CrashEmailTimeout);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Sending crash email failed");
            }
        }

        /// <summary>
        ///     Sends emails async
        /// </summary>
        /// <param name="message"></param>
        private static async Task SendAsync(MimeMessage message)
        {
            if (!EmailConfiguration.EmailEnabled)
            {
                return;
            }

            var attempts = EmailConfiguration.EmailSendRetryCount;
            var retryDelays = RetryDelays(attempts, EmailConfiguration.EmailSendRetryDelay);
            Exception lastException = null;

            // SendCrashEmail can block the game loop on this task: every await needs ConfigureAwait(false).
            for (var i = 0; i < attempts; i++)
            {
                try
                {
                    using var client = new SmtpClient();
                    await client.ConnectAsync(EmailConfiguration.EmailServer, EmailConfiguration.EmailPort, true).ConfigureAwait(false);
                    await client.AuthenticateAsync(
                        EmailConfiguration.EmailServerUsername,
                        EmailConfiguration.EmailServerPassword
                    ).ConfigureAwait(false);
                    await client.SendAsync(message).ConfigureAwait(false);
                    await client.DisconnectAsync(true).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (i == 0)
                    {
                        _logger.Warning(ex, "Sending email {Subject} failed", message.Subject);
                    }

                    if (i < retryDelays.Length)
                    {
                        await Task.Delay(retryDelays[i]).ConfigureAwait(false);
                    }
                }
            }

            _logger.Error(lastException, "Email {Subject} not sent after {Attempts} attempts", message.Subject, attempts);
        }

        /// <summary>
        ///     The waits between send attempts: the configured delay, doubling each time, and none after the last.
        /// </summary>
        /// <param name="attempts"></param>
        /// <param name="firstDelaySeconds"></param>
        internal static TimeSpan[] RetryDelays(int attempts, int firstDelaySeconds)
        {
            var delays = new TimeSpan[Math.Max(attempts, 1) - 1];
            var seconds = Math.Clamp(firstDelaySeconds, 0, MaxRetryDelaySeconds);

            for (var i = 0; i < delays.Length; i++)
            {
                delays[i] = TimeSpan.FromSeconds(seconds);
                seconds = Math.Min(seconds * 2, MaxRetryDelaySeconds);
            }

            return delays;
        }
    }
}
