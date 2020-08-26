using System;

namespace Server.Misc
{
  public class HexStringConverter
  {
    public static readonly uint[] m_Lookup32Chars = CreateLookup32Chars();

    private static uint[] CreateLookup32Chars()
    {
      var result = new uint[256];
      for (int i = 0; i < 256; i++)
      {
        string s = i.ToString("X2");
        if (BitConverter.IsLittleEndian)
          result[i] = s[0] + ((uint)s[1] << 16);
        else
          result[i] = s[1] + ((uint)s[0] << 16);
      }

      return result;
    }

    public static unsafe string GetString(ReadOnlySpan<byte> bytes)
    {
      var result = new string((char)0, bytes.Length * 2);
      fixed (char* resultP = result)
      {
        uint* resultP2 = (uint*)resultP;
        for (int i = 0; i < bytes.Length; i++)
          resultP2[i] = m_Lookup32Chars[bytes[i]];
      }
      return result;
    }

    public static unsafe void GetBytes(string str, Span<byte> bytes)
    {
      fixed (char* strP = str)
      {
        int i = 0;
        int j = 0;
        while (i < str.Length)
        {
          int chr1 = strP[i++];
          int chr2 = strP[i++];
          if (BitConverter.IsLittleEndian)
            bytes[j++] = (byte)((chr1 - (chr1 >= 65 ? 55 : 48)) << 4 | (chr2 - (chr2 >= 65 ? 55 : 48)));
          else
            bytes[j++] = (byte)((chr1 - (chr1 >= 65 ? 55 : 48)) | ((chr2 - (chr2 >= 65 ? 55 : 48)) << 4));
        }
      }
    }
  }
}
