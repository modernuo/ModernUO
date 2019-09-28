/***************************************************************************
 *                             FileOperations.cs
 *                            -------------------
 *   begin                : May 1, 2002
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

using System.IO;

namespace Server.Persistence
{
  public static class FileOperations
  {
    public const int KB = 1024;
    public const int MB = 1024 * KB;

    public static int BufferSize{ get; set; } = 1 * MB;

    public static int Concurrency{ get; set; } = 1;

    public static bool AreSynchronous => Concurrency < 1;

    public static FileStream OpenSequentialStream(string path, FileMode mode, FileAccess access, FileShare share)
    {
      FileOptions options = FileOptions.SequentialScan;

      if (Concurrency > 0)
        options |= FileOptions.Asynchronous;

      return new FileStream( path, mode, access, share, BufferSize, options );
    }
  }
}
