/***************************************************************************
 *                           SequentialFileWriter.cs
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

using System;
using System.IO;

namespace Server
{
    public sealed class SequentialFileWriterStream : Stream
    {
        private FileQueue fileQueue;
        private FileStream fileStream;

        private AsyncCallback writeCallback;

        public SequentialFileWriterStream(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            fileStream = FileOperations.OpenSequentialStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

            fileQueue = new FileQueue(
                Math.Max(FileOperations.Concurrency, 1),
                FileCallback
            );
        }

        public override long Position
        {
            get => fileQueue.Position;
            set => throw new InvalidOperationException();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => Position;

        private void FileCallback(FileQueue.Chunk chunk)
        {
            if (FileOperations.AreSynchronous)
            {
                fileStream.Write(chunk.Buffer, chunk.Offset, chunk.Size);

                chunk.Commit();
            }
            else
            {
                writeCallback ??= OnWrite;

                fileStream.BeginWrite(chunk.Buffer, chunk.Offset, chunk.Size, writeCallback, chunk);
            }
        }

        private void OnWrite(IAsyncResult asyncResult)
        {
            var chunk = asyncResult.AsyncState as FileQueue.Chunk;

            fileStream.EndWrite(asyncResult);

            chunk?.Commit();
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            fileQueue.Enqueue(buffer, offset, size);
        }

        public override void Flush()
        {
            fileQueue.Flush();
            fileStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (fileStream != null)
            {
                Flush();

                fileQueue.Dispose();
                fileQueue = null;

                fileStream.Close();
                fileStream = null;
            }

            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();

        public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();

        public override void SetLength(long value)
        {
            fileStream.SetLength(value);
        }
    }
}
