using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Engines.Chat
{
    public static class Discord
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly ILogger _logger = LogFactory.GetLogger(typeof(Discord));
        private static string _webhookUrl;
        
        public static void Configure()
        {
            _webhookUrl = ServerConfiguration.GetOrUpdateSetting("chatdiscord.webhookUrl", "DISCORD_CHANNEL_WEBHOOK");
            _logger.Information("Discord integration configured. Enabled: {Enabled}", IsEnabled);
        }
        
        public static bool IsEnabled => !string.IsNullOrEmpty(_webhookUrl);
        
        public static async Task SendChannelMessageAsync(string channelName, string username, string message)
        {
            var webhookUrl = _webhookUrl;
            if (string.IsNullOrEmpty(webhookUrl))
            {
                return;
            }
            
            try
            {
                var payload = new
                {
                    username = $"UO Chat",
                    content = $"**[{channelName}]** {username}: {message}",
                };
                
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PostAsync(_webhookUrl, content);
                
                _logger.Debug("Discord message sent for channel {ChannelName} user {Username}", channelName, username);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Discord webhook failed for channel {ChannelName} user {Username}", channelName, username);
            }
        }
    }
}
