using System;
using System.Text;

namespace Server.Tests
{
    public static class EncodingHelpers
    {
        public static (Encoding, Type) GetEncoding(string value) =>
            value.ToUpper() switch
            {
                "UTF8"      => (Utility.UTF8, typeof(byte)),
                "UNICODELE" => (Utility.UnicodeLE, typeof(char)),
                "UNICODE"   => (Utility.Unicode, typeof(char)),
                _           => (Encoding.ASCII, typeof(byte))
            };
    }
}
