using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Engines.Chat
{
    public static class Discord
    {
        private static readonly HttpClient _httpClient = new();
        private static string _webhookUrl;
        
        public static void Configure()
        {
            _webhookUrl = ServerConfiguration.GetOrUpdateSetting("chatdiscord.webhookUrl", "DISCORD_CHANNEL_WEBHOOK_HERE");
        }
        
        public static bool IsEnabled => !string.IsNullOrEmpty(_webhookUrl);
        
        public static async Task SendChannelMessageAsync(string channelName, string username, string message)
        {
            if (!IsEnabled)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discord webhook error: {ex.Message}");
            }
        }
    }
}