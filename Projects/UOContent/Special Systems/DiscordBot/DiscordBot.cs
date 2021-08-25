using SimpleTcp;
using System;

namespace Server.DiscordBot
{
    public class DiscordBot
    {
        public static SimpleTcpClient Client { get; set; }
        public static void Initialize()
        {
            // instantiate
            Client = new SimpleTcpClient("127.0.0.1:11000");

            // set events
            Client.Events.Connected += Connected;
            Client.Events.Disconnected += Disconnected;
            Client.Events.DataReceived += DataReceived;

            // let's go!
            Client.Connect();

            
            EventSink.CharacterCreated += EventSink_CharacterCreated;
            EventSink.WorldSaveDone += EventSink_WorldSaveDone;
            

        }

        private static async void EventSink_WorldSaveDone(double obj)
        {
            string message = "{\"Id\":3,\"Content\":{\"Duration\":" + obj.ToString() + "}}";
            await Client.SendAsync(message);
        }

        private static async void EventSink_CharacterCreated(CharacterCreatedEventArgs obj)
        {
            string message = "{\"Id\":2,\"Content\":{\"Name\":\"" + obj.Name + "\"}}";
            await Client.SendAsync(message);
        }

        private static async void EventSink_GameLogin(GameLoginEventArgs obj)
        {
            //obj.Username
            string message = "{\"Id\":1,\"Content\":{\"Username\":\""+ obj.Username +"\"}}";
            await Client.SendAsync(message);
            
        }

        static void Connected(object sender, EventArgs e)
        {
            //Console.WriteLine("*** Server connected");
        }

        static void Disconnected(object sender, EventArgs e)
        {
            //Console.WriteLine("*** Server disconnected");
        }

        static void DataReceived(object sender, DataReceivedEventArgs e)
        {
            //Console.WriteLine("[" + e.IpPort + "] " + Encoding.UTF8.GetString(e.Data));
        }
    }
}
