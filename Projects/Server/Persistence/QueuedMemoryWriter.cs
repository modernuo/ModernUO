/***************************************************************************
 *                            QueuedMemoryWriter.cs
 *                            -------------------
 *   begin                : December 16, 2010
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System.Collections.Generic;
using System.IO;

namespace Server
{
  public sealed class QueuedMemoryWriter : BinaryFileWriter
  {
    private readonly MemoryStream m_MemoryStream;
    private readonly List<IndexInfo> m_OrderedIndexInfo = new List<IndexInfo>();

    public QueuedMemoryWriter()
      : base(new MemoryStream(1024 * 1024), true) =>
      m_MemoryStream = UnderlyingStream as MemoryStream;

    protected override int BufferSize => 512;

    public void QueueForIndex(ISerializable serializable, int size)
    {
      IndexInfo info;

      info.size = size;

      info.typeCode = serializable.TypeRef; // For guilds, this will automagically be zero.
      info.serial = serializable.Serial;

      m_OrderedIndexInfo.Add(info);
    }

    public void CommitTo(SequentialFileWriterStream dataFile, SequentialFileWriterStream indexFile)
    {
      Flush();

      var memLength = (int)m_MemoryStream.Position;

      if (memLength > 0)
      {
        var memBuffer = m_MemoryStream.GetBuffer();

        var actualPosition = dataFile.Position;

        dataFile.Write(memBuffer, 0, memLength); // The buffer contains the data from many items.

        // Console.WriteLine("Writing {0} bytes starting at {1}, with {2} things", memLength, actualPosition, _orderedIndexInfo.Count);

        var indexBuffer = new byte[20];

        // int indexWritten = _orderedIndexInfo.Count * indexBuffer.Length;
        // int totalWritten = memLength + indexWritten

        for (var i = 0; i < m_OrderedIndexInfo.Count; i++)
        {
          var info = m_OrderedIndexInfo[i];

          indexBuffer[0] = (byte)info.typeCode;
          indexBuffer[1] = (byte)(info.typeCode >> 8);
          indexBuffer[2] = (byte)(info.typeCode >> 16);
          indexBuffer[3] = (byte)(info.typeCode >> 24);

          indexBuffer[4] = (byte)info.serial;
          indexBuffer[5] = (byte)(info.serial >> 8);
          indexBuffer[6] = (byte)(info.serial >> 16);
          indexBuffer[7] = (byte)(info.serial >> 24);

          indexBuffer[8] = (byte)actualPosition;
          indexBuffer[9] = (byte)(actualPosition >> 8);
          indexBuffer[10] = (byte)(actualPosition >> 16);
          indexBuffer[11] = (byte)(actualPosition >> 24);
          indexBuffer[12] = (byte)(actualPosition >> 32);
          indexBuffer[13] = (byte)(actualPosition >> 40);
          indexBuffer[14] = (byte)(actualPosition >> 48);
          indexBuffer[15] = (byte)(actualPosition >> 56);

          indexBuffer[16] = (byte)info.size;
          indexBuffer[17] = (byte)(info.size >> 8);
          indexBuffer[18] = (byte)(info.size >> 16);
          indexBuffer[19] = (byte)(info.size >> 24);

          indexFile.Write(indexBuffer, 0, indexBuffer.Length);

          actualPosition += info.size;
        }
      }

      Close(); // We're done with this writer.
    }

    private struct IndexInfo
    {
      public int size;
      public int typeCode;
      public uint serial;
    }
  }
}
