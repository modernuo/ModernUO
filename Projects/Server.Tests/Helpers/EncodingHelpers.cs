using System.Text;
using Server.Text;

namespace Server.Tests
{
    public static class EncodingHelpers
    {
        public static Encoding GetEncoding(string bodyname) =>
            bodyname switch
            {
                "utf-8"    => TextEncoding.UTF8,
                "utf-16"   => TextEncoding.UnicodeLE,
                "utf-16BE" => TextEncoding.Unicode,
                _          => Encoding.ASCII
            };
    }
}
