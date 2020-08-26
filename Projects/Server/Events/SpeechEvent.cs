using System;
using Server.Network;

namespace Server
{
    public class SpeechEventArgs : EventArgs
    {
        public SpeechEventArgs(Mobile mobile, string speech, MessageType type, int hue, int[] keywords)
        {
            Mobile = mobile;
            Speech = speech;
            Type = type;
            Hue = hue;
            Keywords = keywords;
        }

        public Mobile Mobile { get; }

        public string Speech { get; set; }

        public MessageType Type { get; }

        public int Hue { get; }

        public int[] Keywords { get; }

        public bool Handled { get; set; }

        public bool Blocked { get; set; }

        public bool HasKeyword(int keyword)
        {
            for (var i = 0; i < Keywords.Length; ++i)
                if (Keywords[i] == keyword)
                    return true;

            return false;
        }
    }

    public static partial class EventSink
    {
        public static event Action<SpeechEventArgs> Speech;
        public static void InvokeSpeech(SpeechEventArgs e) => Speech?.Invoke(e);
    }
}
