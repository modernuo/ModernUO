using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Server.Logging;
using System.Collections.Generic;

namespace Server.Engines.Help
{
    public static class PageDiscord
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly ILogger _logger = LogFactory.GetLogger(typeof(PageDiscord));
        private static string _webhookUrl;
        
        public static void Configure()
        {
            _webhookUrl = ServerConfiguration.GetOrUpdateSetting("pagediscord.webhookUrl", "DISCORD_PAGE_WEBHOOK");
            _logger.Information("Page Discord integration configured. Enabled: {Enabled}", IsEnabled);
        }
        
        public static bool IsEnabled => !string.IsNullOrEmpty(_webhookUrl);
        
        public static async Task SendPageNotificationAsync(PageEntry entry)
        {
            if (!IsEnabled)
            {
                return;
            }
            
            try
            {
                var pageTypeName = PageQueue.GetPageTypeName(entry.Type);
                var locationInfo = $"{entry.PageLocation} ({entry.PageMap?.Name ?? "Unknown"})";
                var timeSent = entry.Sent.ToString("yyyy-MM-dd HH:mm:ss UTC");
                
                var fieldsList = new List<object>
                {
                    new { name = "Player", value = entry.Sender?.Name ?? "Unknown", inline = true },
                    new { name = "Account", value = entry.Sender?.Account?.Username ?? "Unknown", inline = true },
                    new { name = "Page Type", value = pageTypeName, inline = true },
                    new { name = "Location", value = locationInfo, inline = true },
                    new { name = "Time Sent", value = timeSent, inline = true },
                    new { name = "Queue Position", value = $"#{PageQueue.List.Count}", inline = true },
                    new { name = "Message", value = TruncateMessage(entry.Message), inline = false }
                };

                if (entry.SpeechLog != null && entry.SpeechLog.Count > 0)
                {
                    fieldsList.Add(new 
                    { 
                        name = "Speech Log Available", 
                        value = $"Contains {entry.SpeechLog.Count} speech entries", 
                        inline = false
                    });
                }

                var embed = new
                {
                    title = $"New {pageTypeName} Page",
                    color = GetPageTypeColor(entry.Type),
                    fields = fieldsList.ToArray(),
                    footer = new { text = "UO Page System" },
                    timestamp = entry.Sent.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                var payload = new
                {
                    username = "UO Pages",
                    embeds = new[] { embed }
                };
                
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PostAsync(_webhookUrl, content);
                
                _logger.Debug("Discord page notification sent for {Player} - {PageType}", 
                    entry.Sender?.Name, pageTypeName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Discord page webhook failed for {Player} - {PageType}", 
                    entry.Sender?.Name, entry.Type);
            }
        }
        
        public static async Task SendPageHandlerUpdateAsync(PageEntry entry, Mobile oldHandler, Mobile newHandler)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (oldHandler != null && newHandler == null && PageQueue.IndexOf(entry) == -1)
            {
                return;
            }
            
            try
            {
                var pageTypeName = PageQueue.GetPageTypeName(entry.Type);
                string actionText;
                int color;
                
                if (oldHandler == null && newHandler != null)
                {
                    actionText = $"🛠️ **{newHandler.Name}** is now handling the page";
                    color = 0x00FF00; // green
                }
                else if (oldHandler != null && newHandler == null)
                {
                    actionText = $"❌ **{oldHandler.Name}** stopped handling the page";
                    color = 0xFF0000; // red
                }
                else
                {
                    return;
                }
                
                var embed = new
                {
                    title = $"{pageTypeName} Page Update",
                    description = actionText,
                    color,
                    fields = new[]
                    {
                        new { name = "Player", value = entry.Sender?.Name ?? "Unknown", inline = true },
                        new { name = "Page Type", value = pageTypeName, inline = true },
                        new { name = "Queue Position", value = $"#{PageQueue.IndexOf(entry) + 1}", inline = true }
                    },
                    footer = new { text = "UO Page System" },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                var payload = new
                {
                    username = "UO Pages",
                    embeds = new[] { embed }
                };
                
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PostAsync(_webhookUrl, content);
                
                _logger.Debug("Discord page handler update sent for {Player} - {PageType}", 
                    entry.Sender?.Name, pageTypeName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Discord page handler update webhook failed for {Player} - {PageType}", 
                    entry.Sender?.Name, entry.Type);
            }
        }
        
        public static async Task SendPageCompletedAsync(PageEntry entry)
        {
            if (!IsEnabled)
            {
                return;
            }
            
            try
            {
                var pageTypeName = PageQueue.GetPageTypeName(entry.Type);
                var handlerName = entry.Handler?.Name ?? "Staff";
                var elapsed = Core.Now - entry.Sent;
                var elapsedText = elapsed.TotalMinutes < 1 
                    ? "< 1 minute" 
                    : $"{elapsed.TotalMinutes:F0} minutes";
                
                var embed = new
                {
                    title = $"✅ {pageTypeName} Page Completed",
                    color = 0x00AA00, // dark green
                    fields = new[]
                    {
                        new { name = "Player", value = entry.Sender?.Name ?? "Unknown", inline = true },
                        new { name = "Handled By", value = handlerName, inline = true },
                        new { name = "Response Time", value = elapsedText, inline = true }
                    },
                    footer = new { text = "UO Page System" },
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                var payload = new
                {
                    username = "UO Pages",
                    embeds = new[] { embed }
                };
                
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PostAsync(_webhookUrl, content);
                
                _logger.Debug("Discord page completed notification sent for {Player} - {PageType}", 
                    entry.Sender?.Name, pageTypeName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Discord page completed webhook failed for {Player} - {PageType}", 
                    entry.Sender?.Name, entry.Type);
            }
        }
        
        private static int GetPageTypeColor(PageType type)
        {
            return type switch
            {
                PageType.Bug => 0xFF0000,                   // red
                PageType.Stuck => 0xFFFF00,                 // yellow  
                PageType.Account => 0x0000FF,               // blue
                PageType.Question => 0x00FFFF,              // cyan
                PageType.Suggestion => 0x00FF00,            // green
                PageType.VerbalHarassment => 0xFF00FF,      // magenta
                PageType.PhysicalHarassment => 0x800080,    // purple
                _ => 0x888888                               // gray for other
            };
        }
        
        private static string TruncateMessage(string message)
        {
            const int maxLength = 1000; // apparently the discord field limit is 1024
            
            if (string.IsNullOrEmpty(message))
            {
                return "*(no message)*";
            }
            
            return message.Length <= maxLength 
                ? message 
                : message.Substring(0, maxLength - 3) + "...";
        }
    }
}