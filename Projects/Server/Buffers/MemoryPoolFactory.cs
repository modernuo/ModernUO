// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Buffers
{
    public static class SlabMemoryPoolFactory
    {
        public static MemoryPool<byte> Create() => CreateSlabMemoryPool();
        public static MemoryPool<byte> CreateSlabMemoryPool() => new SlabMemoryPool();
    }
}
