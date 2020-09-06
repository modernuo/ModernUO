// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public static class MemoryPoolThrowHelper
    {
        public enum ExceptionArgument
        {
            size,
            offset,
            length,
            MemoryPoolBlock,
            MemoryPool
        }

        public static void ThrowArgumentOutOfRangeException(int sourceLength, int offset)
        {
            throw GetArgumentOutOfRangeException(sourceLength, offset);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(int sourceLength, int offset) =>
            (uint)offset > (uint)sourceLength
                ? new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.offset))
                : new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.length));

        public static void ThrowArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize)
        {
            throw GetArgumentOutOfRangeException_BufferRequestTooLarge(maxSize);
        }

        public static void ThrowObjectDisposedException(ExceptionArgument argument)
        {
            throw GetObjectDisposedException(argument);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize) =>
            new ArgumentOutOfRangeException(
                GetArgumentName(ExceptionArgument.size),
                $"Cannot allocate more than {maxSize} bytes in a single buffer"
            );

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectDisposedException GetObjectDisposedException(ExceptionArgument argument) =>
            new ObjectDisposedException(GetArgumentName(argument));

        private static string GetArgumentName(ExceptionArgument argument) => argument.ToString();
    }
}
