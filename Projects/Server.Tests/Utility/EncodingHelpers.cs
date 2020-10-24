using System;
using System.Text;

namespace Server.Tests
{
    public static class EncodingHelpers
    {
        public static Encoding GetEncoding(string bodyname) =>
            bodyname switch
            {
                "utf-8"    => Utility.UTF8,
                "utf-16"   => Utility.UnicodeLE,
                "utf-16BE" => Utility.Unicode,
                _          => Encoding.ASCII
            };
    }
}
