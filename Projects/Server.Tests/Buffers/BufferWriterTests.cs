using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace Server.Tests.Buffers
{
  public class BufferWriterTests
  {
    [Fact]
    public void TestWriteAsciiFixed()
    {
      Span<byte> data = new byte[100];
      BufferWriter writer = new BufferWriter(data.Slice(50, 50), data.Slice(0, 50));

      // All data in first span
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteAsciiFixed("this is a test", 14);

      // Data crosses spans
      data.Fill(0);
      writer.Seek(44, SeekOrigin.Begin);
      writer.WriteAsciiFixed("this is a test", 14);

      // All data in second span
      data.Fill(0);
      writer.Seek(60, SeekOrigin.Begin);
      writer.WriteAsciiFixed("this is a test", 14);

      // Truncate string
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteAsciiFixed("this is a test", 4);

      // Pad string
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteAsciiFixed("this is a test", 30);
    }

    [Fact]
    public void TestWriteAsciiNull()
    {
      Span<byte> data = new byte[100];
      BufferWriter writer = new BufferWriter(data.Slice(50, 50), data.Slice(0, 50));

      // All data in first span
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteAsciiNull("this is a test");

      // Data crosses spans
      data.Fill(0);
      writer.Seek(44, SeekOrigin.Begin);
      writer.WriteAsciiNull("this is a test");

      // All data in second span
      data.Fill(0);
      writer.Seek(60, SeekOrigin.Begin);
      writer.WriteAsciiNull("this is a test");
    }

    [Fact]
    public void TestWriteLittleUniNull()
    {
      Span<byte> data = new byte[100];
      BufferWriter writer = new BufferWriter(data.Slice(50, 50), data.Slice(0, 50));

      // All data in first span
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteLittleUniNull("this is a test");

      // Data crosses spans without splitting any characters
      data.Fill(0);
      writer.Seek(44, SeekOrigin.Begin);
      writer.WriteLittleUniNull("this is a test");

      // Data crosses spans and splits a character
      data.Fill(0);
      writer.Seek(45, SeekOrigin.Begin);
      writer.WriteLittleUniNull("this is a test");

      // All data in second span
      data.Fill(0);
      writer.Seek(60, SeekOrigin.Begin);
      writer.WriteLittleUniNull("this is a test");
    }

    [Fact]
    public void TestWriteLittleUniFixed()
    {
      Span<byte> data = new byte[100];
      BufferWriter writer = new BufferWriter(data.Slice(50, 50), data.Slice(0, 50));

      // All data in first span
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteLittleUniFixed("this is a test", 14);

      // Data crosses spans without splitting any characters
      data.Fill(0);
      writer.Seek(44, SeekOrigin.Begin);
      writer.WriteLittleUniFixed("this is a test", 14);

      // Data crosses spans and splits a character
      data.Fill(0);
      writer.Seek(45, SeekOrigin.Begin);
      writer.WriteLittleUniFixed("this is a test", 14);

      // All data in second span
      data.Fill(0);
      writer.Seek(60, SeekOrigin.Begin);
      writer.WriteLittleUniFixed("this is a test", 14);

      // Truncate string
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteLittleUniFixed("this is a test", 4);

      // Pad string
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteLittleUniFixed("this is a test", 30);
    }

    [Fact]
    public void TestWriteBigUniNull()
    {
      Span<byte> data = new byte[100];
      BufferWriter writer = new BufferWriter(data.Slice(50, 50), data.Slice(0, 50));

      // All data in first span
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteBigUniNull("this is a test");

      // Data crosses spans without splitting any characters
      data.Fill(0);
      writer.Seek(44, SeekOrigin.Begin);
      writer.WriteBigUniNull("this is a test");

      // Data crosses spans and splits a character
      data.Fill(0);
      writer.Seek(45, SeekOrigin.Begin);
      writer.WriteBigUniNull("this is a test");

      // All data in second span
      data.Fill(0);
      writer.Seek(60, SeekOrigin.Begin);
      writer.WriteBigUniNull("this is a test");
    }

    [Fact]
    public void TestWriteBigUniFixed()
    {
      Span<byte> data = new byte[100];
      BufferWriter writer = new BufferWriter(data.Slice(50, 50), data.Slice(0, 50));

      // All data in first span
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteBigUniFixed("this is a test", 14);

      // Data crosses spans without splitting any characters
      data.Fill(0);
      writer.Seek(44, SeekOrigin.Begin);
      writer.WriteBigUniFixed("this is a test", 14);

      // Data crosses spans and splits a character
      data.Fill(0);
      writer.Seek(45, SeekOrigin.Begin);
      writer.WriteBigUniFixed("this is a test", 14);

      // All data in second span
      data.Fill(0);
      writer.Seek(60, SeekOrigin.Begin);
      writer.WriteBigUniFixed("this is a test", 14);

      // Truncate string
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteBigUniFixed("this is a test", 4);

      // Pad string
      data.Fill(0);
      writer.Seek(0, SeekOrigin.Begin);
      writer.WriteBigUniFixed("this is a test", 30);
    }
  }
}
