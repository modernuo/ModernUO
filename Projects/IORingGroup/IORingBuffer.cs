// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Network;

/// <summary>
/// A double-mapped circular buffer for zero-copy I/O operations.
/// The same physical memory is mapped twice consecutively in virtual address space,
/// allowing reads/writes that wrap around to appear contiguous.
/// </summary>
/// <remarks>
/// This buffer is designed for use with RIO (Windows) and io_uring (Linux) registered buffers.
/// The double-mapping eliminates the need for wrap-around handling in hot paths.
/// </remarks>
public sealed partial class IORingBuffer : IDisposable
{
    private nint _handle;
    private nint _buffer;
    private readonly int _physicalSize;
    private readonly int _mask; // physicalSize - 1; valid as a wrap mask because size is always a power of 2
    private int _head;  // Reclaim position - advances on I/O completion (0 to physicalSize-1)
    private int _sent;  // Send position - advances when handed to the transport
    private int _tail;  // Write position (0 to physicalSize-1)
    private bool _disposed;

    /// <summary>
    /// Gets the base pointer of the buffer.
    /// The full virtual range is 2x PhysicalSize (double-mapped).
    /// </summary>
    public nint Pointer => _buffer;

    /// <summary>
    /// Gets the physical size of the buffer in bytes.
    /// </summary>
    public int PhysicalSize => _physicalSize;

    /// <summary>
    /// Gets the virtual size of the buffer (2x physical for double-mapping).
    /// </summary>
    public int VirtualSize => _physicalSize * 2;

    /// <summary>
    /// Gets or sets the registered buffer ID (RIO BufferId or io_uring buffer index).
    /// Set by the pool/ring when the buffer is registered.
    /// </summary>
    public int BufferId { get; internal set; } = -1;

    /// <summary>
    /// Indicates whether this buffer belongs to a pool (vs fallback allocation).
    /// </summary>
    internal bool IsPooled { get; }

    /// <summary>
    /// Index within the pool's buffer array (-1 for fallback buffers).
    /// </summary>
    internal int PoolIndex { get; }

    /// <summary>
    /// Gets the number of bytes available for reading.
    /// </summary>
    public int ReadableBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var head = _head;
            var tail = _tail;
            return tail >= head ? tail - head : _physicalSize - head + tail;
        }
    }

    /// <summary>
    /// Gets the number of bytes available for writing.
    /// </summary>
    public int WritableBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _physicalSize - ReadableBytes - 1; // -1 to distinguish full from empty
    }

    /// <summary>
    /// Gets the current read offset (head position) in the buffer.
    /// Use this when preparing RIO/io_uring operations.
    /// </summary>
    public int ReadOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _head;
    }

    /// <summary>
    /// Gets the offset of the first byte not yet handed to the transport.
    /// </summary>
    /// <remarks>
    /// Sends post from here so several can be in flight; <see cref="ReadOffset"/> is the reclaim
    /// point and advances only on completion, keeping in-flight bytes safe from overwrite. Required
    /// for zero-copy I/O, where the transport may re-read this memory to retransmit.
    /// </remarks>
    public int SendOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _sent;
    }

    /// <summary>
    /// Gets the number of bytes queued but not yet handed to the transport.
    /// </summary>
    public int SendableBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var sent = _sent;
            var tail = _tail;
            return tail >= sent ? tail - sent : _physicalSize - sent + tail;
        }
    }

    /// <summary>
    /// Gets the number of bytes handed to the transport but not yet completed.
    /// </summary>
    public int InFlightBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var head = _head;
            var sent = _sent;
            return sent >= head ? sent - head : _physicalSize - head + sent;
        }
    }

    /// <summary>
    /// Gets the current write offset (tail position) in the buffer.
    /// Use this when preparing RIO/io_uring operations.
    /// </summary>
    public int WriteOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tail;
    }

    /// <summary>
    /// Creates a new double-mapped circular buffer.
    /// </summary>
    /// <param name="physicalSize">Physical size in bytes. Must be a power of 2 and a multiple of the platform allocation granularity (64 KB on Windows, the page size on Unix).</param>
    /// <returns>A new IORingBuffer instance.</returns>
    public static IORingBuffer Create(int physicalSize) => Create(physicalSize, isPooled: false, poolIndex: -1);

    /// <summary>
    /// Creates a new double-mapped circular buffer with pool tracking.
    /// </summary>
    internal static IORingBuffer Create(int physicalSize, bool isPooled, int poolIndex)
    {
        if (physicalSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(physicalSize), "Size must be positive");
        }

        // Verify power of 2
        if ((physicalSize & (physicalSize - 1)) != 0)
        {
            throw new ArgumentException("Size must be a power of 2", nameof(physicalSize));
        }

        // Windows places the second mapping at an offset of physicalSize and requires both the
        // base address and the offset to be aligned to the 64 KB allocation granularity, not just
        // the page size. Unix mappings only need page alignment. physicalSize is already validated
        // as a power of 2, so on Windows this is effectively a 64 KB minimum-size check.
        var alignment = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? 65536
            : Environment.SystemPageSize;

        if (physicalSize % alignment != 0)
        {
            throw new ArgumentException(
                $"Size must be a multiple of the allocation granularity ({alignment} bytes on this platform)",
                nameof(physicalSize)
            );
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return CreateWindows(physicalSize, isPooled, poolIndex);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return CreateLinux(physicalSize, isPooled, poolIndex);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return CreateBsd(physicalSize, isPooled, poolIndex);
        }

        throw new PlatformNotSupportedException("IORingBuffer requires Windows, Linux, macOS, or FreeBSD");
    }

    private IORingBuffer(nint handle, nint buffer, int physicalSize, bool isPooled, int poolIndex)
    {
        _handle = handle;
        _buffer = buffer;
        _physicalSize = physicalSize;
        _mask = physicalSize - 1;
        _head = 0;
        _sent = 0;
        _tail = 0;
        IsPooled = isPooled;
        PoolIndex = poolIndex;
    }

    /// <summary>
    /// Gets a contiguous span for writing.
    /// Due to double-mapping, the span is always contiguous regardless of wrap-around.
    /// Use <see cref="WriteOffset"/> when preparing RIO/io_uring operations.
    /// </summary>
    /// <returns>A span representing the writable region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<byte> GetWriteSpan() => new((byte*)_buffer + _tail, WritableBytes);

    /// <summary>
    /// Advances the write position after writing data.
    /// </summary>
    /// <param name="count">Number of bytes written.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CommitWrite(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, WritableBytes);

        _tail = (_tail + count) & _mask;
    }

    /// <summary>
    /// Gets a contiguous span for reading.
    /// Due to double-mapping, the span is always contiguous regardless of wrap-around.
    /// Use <see cref="ReadOffset"/> when preparing RIO/io_uring operations.
    /// </summary>
    /// <returns>A span representing the readable region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<byte> GetReadSpan()
    {
        return new Span<byte>((byte*)_buffer + _head, ReadableBytes);
    }

    /// <summary>
    /// Advances the read position after consuming data.
    /// </summary>
    /// <param name="count">Number of bytes consumed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CommitRead(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ReadableBytes);

        // Reclaiming past the send cursor would hand the writer space the transport is still
        // reading from. On the recv path _sent tracks _head so this is a no-op.
        if (count > InFlightBytes)
        {
            _sent = (_head + count) & _mask;
        }

        _head = (_head + count) & _mask;

        // NOTE: We intentionally do NOT reset head/tail to 0 when empty.
        // With zero-copy I/O, a recv may already be posted at the current _tail offset.
        // If we reset to 0, the kernel will write data at the old offset, but we'll
        // read from offset 0 (which contains stale data).
        // The double-mapping handles wrap-around, so no reset is needed.
    }

    /// <summary>
    /// Resets the buffer to empty state.
    /// </summary>
    public void Reset()
    {
        _head = 0;
        _sent = 0;
        _tail = 0;
    }

    /// <summary>
    /// Reclaims a partially completed send, rewinding the send cursor so the bytes the transport
    /// did not accept become sendable again.
    /// </summary>
    /// <param name="sent">Bytes the transport actually accepted.</param>
    /// <remarks>
    /// Only valid when nothing else is in flight; sends posted beyond the gap cannot be repaired in
    /// order. Routine on send() semantics (epoll, io_uring, kqueue), which is why this is a normal
    /// path rather than a fatal one.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CommitShortSend(int sent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sent, InFlightBytes);

        _head = (_head + sent) & _mask;
        _sent = _head;
    }

    /// <summary>
    /// Advances the send position after handing <paramref name="count"/> bytes to the transport.
    /// The bytes stay unreclaimed until the matching completion calls <see cref="CommitRead"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CommitSend(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, SendableBytes);

        _sent = (_sent + count) & _mask;
    }

    #region Windows Implementation

    // VirtualAlloc2 and MapViewOfFile3 (and the MEM_*_PLACEHOLDER flags) are exported from
    // kernelbase.dll starting with Windows 10 1803 / Windows Server 2019. They are absent on
    // Windows Server 2012, 2012 R2, and 2016. Resolve availability once so the hot path stays a
    // single bool check rather than an exception on first call.
    private static readonly bool UsePlaceholderApi = DetectPlaceholderApi();

    private static bool DetectPlaceholderApi()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        if (!NativeLibrary.TryLoad("kernelbase.dll", out var lib))
        {
            return false;
        }

        try
        {
            return NativeLibrary.TryGetExport(lib, "VirtualAlloc2", out _)
                && NativeLibrary.TryGetExport(lib, "MapViewOfFile3", out _);
        }
        finally
        {
            NativeLibrary.Free(lib);
        }
    }

    // Test seam: forces the legacy (pre-1803) or placeholder path regardless of what the OS
    // exports, so the Server 2012/2016 fallback can be exercised on a modern Windows runner.
    // null = auto-detect from UsePlaceholderApi (production default). No effect off Windows.
    internal static bool? ForceLegacyWindowsPath;

    private static IORingBuffer CreateWindows(int physicalSize, bool isPooled, int poolIndex)
    {
        var useLegacy = ForceLegacyWindowsPath ?? !UsePlaceholderApi;
        return useLegacy
            ? CreateWindowsLegacy(physicalSize, isPooled, poolIndex)
            : CreateWindowsPlaceholder(physicalSize, isPooled, poolIndex);
    }

    // Modern path (Windows 10 1803 / Server 2019+): the placeholder split is race-free.
    private static IORingBuffer CreateWindowsPlaceholder(int physicalSize, bool isPooled, int poolIndex)
    {
        // Reserve a region of virtual memory (2x size for double-mapping)
        var region = WindowsNative.VirtualAlloc2(
            nint.Zero,
            nint.Zero,
            (ulong)physicalSize * 2,
            WindowsNative.MEM_RESERVE | WindowsNative.MEM_RESERVE_PLACEHOLDER,
            WindowsNative.PAGE_NOACCESS,
            nint.Zero,
            0
        );

        if (region == nint.Zero)
        {
            throw new InvalidOperationException($"VirtualAlloc2 failed: {Marshal.GetLastPInvokeError()}");
        }

        // Split the placeholder - release the first half to create two separate placeholders
        var freed = WindowsNative.VirtualFree(
            region,
            (uint)physicalSize,
            WindowsNative.MEM_RELEASE | WindowsNative.MEM_PRESERVE_PLACEHOLDER
        );

        if (!freed)
        {
            WindowsNative.VirtualFree(region, 0, WindowsNative.MEM_RELEASE);
            throw new InvalidOperationException($"VirtualFree placeholder failed: {Marshal.GetLastPInvokeError()}");
        }

        // Create a file mapping object backed by the paging file
        var handle = WindowsNative.CreateFileMappingW(
            WindowsNative.InvalidHandleValue,
            nint.Zero,
            WindowsNative.PAGE_READWRITE,
            0,
            (uint)physicalSize,
            null
        );

        if (handle == nint.Zero)
        {
            WindowsNative.VirtualFree(region, 0, WindowsNative.MEM_RELEASE);
            WindowsNative.VirtualFree(region + physicalSize, 0, WindowsNative.MEM_RELEASE);
            throw new InvalidOperationException($"CreateFileMapping failed: {Marshal.GetLastPInvokeError()}");
        }

        // Map the first view
        var buffer = WindowsNative.MapViewOfFile3(
            handle,
            nint.Zero,
            region,
            0,
            (ulong)physicalSize,
            WindowsNative.MEM_REPLACE_PLACEHOLDER,
            WindowsNative.PAGE_READWRITE,
            nint.Zero,
            0
        );

        if (buffer == nint.Zero)
        {
            WindowsNative.CloseHandle(handle);
            WindowsNative.VirtualFree(region, 0, WindowsNative.MEM_RELEASE);
            WindowsNative.VirtualFree(region + physicalSize, 0, WindowsNative.MEM_RELEASE);
            throw new InvalidOperationException($"MapViewOfFile3 (first) failed: {Marshal.GetLastPInvokeError()}");
        }

        // Map the second view (same physical memory, adjacent virtual address)
        var view2 = WindowsNative.MapViewOfFile3(
            handle,
            nint.Zero,
            buffer + physicalSize,
            0,
            (ulong)physicalSize,
            WindowsNative.MEM_REPLACE_PLACEHOLDER,
            WindowsNative.PAGE_READWRITE,
            nint.Zero,
            0
        );

        if (view2 == nint.Zero)
        {
            WindowsNative.UnmapViewOfFile(buffer);
            WindowsNative.CloseHandle(handle);
            throw new InvalidOperationException($"MapViewOfFile3 (second) failed: {Marshal.GetLastPInvokeError()}");
        }

        return new IORingBuffer(handle, buffer, physicalSize, isPooled, poolIndex);
    }

    // Legacy path (Windows Server 2012 / 2012 R2 / 2016, or any pre-1803 build) where the
    // placeholder APIs do not exist. Reserve a 2x hole to locate contiguous address space,
    // release it, then map the same section into both halves. The window between the release and
    // the maps is racy: another allocation can steal the hole, so retry a bounded number of times.
    // Warming the buffer pool at startup on a single thread makes this window effectively
    // uncontended, so in practice the first attempt succeeds.
    private static IORingBuffer CreateWindowsLegacy(int physicalSize, bool isPooled, int poolIndex)
    {
        // Single pagefile-backed section shared by both views.
        var handle = WindowsNative.CreateFileMappingW(
            WindowsNative.InvalidHandleValue,
            nint.Zero,
            WindowsNative.PAGE_READWRITE,
            0,
            (uint)physicalSize,
            null
        );

        if (handle == nint.Zero)
        {
            throw new InvalidOperationException($"CreateFileMapping failed: {Marshal.GetLastPInvokeError()}");
        }

        var buffer = nint.Zero;
        const int maxAttempts = 100;

        for (var attempt = 0; attempt < maxAttempts && buffer == nint.Zero; attempt++)
        {
            // Reserve 2x to locate a contiguous, correctly aligned hole.
            var region = WindowsNative.VirtualAlloc(
                nint.Zero,
                (nuint)((long)physicalSize * 2),
                WindowsNative.MEM_RESERVE,
                WindowsNative.PAGE_NOACCESS
            );

            if (region == nint.Zero)
            {
                WindowsNative.CloseHandle(handle);
                throw new InvalidOperationException($"VirtualAlloc reserve failed: {Marshal.GetLastPInvokeError()}");
            }

            // Release the reservation so the hole is available to map into.
            WindowsNative.VirtualFree(region, 0, WindowsNative.MEM_RELEASE);

            // Map the first half at the start of the hole.
            var view1 = WindowsNative.MapViewOfFileEx(
                handle,
                WindowsNative.FILE_MAP_READ | WindowsNative.FILE_MAP_WRITE,
                0,
                0,
                (nuint)physicalSize,
                region
            );

            if (view1 == nint.Zero)
            {
                continue; // hole was taken between release and map; retry
            }

            // Map the second half immediately after (same section => same physical pages).
            var view2 = WindowsNative.MapViewOfFileEx(
                handle,
                WindowsNative.FILE_MAP_READ | WindowsNative.FILE_MAP_WRITE,
                0,
                0,
                (nuint)physicalSize,
                region + physicalSize
            );

            if (view2 == nint.Zero)
            {
                WindowsNative.UnmapViewOfFile(view1);
                continue; // second half was taken; retry the whole reservation
            }

            buffer = view1;
        }

        if (buffer == nint.Zero)
        {
            WindowsNative.CloseHandle(handle);
            throw new InvalidOperationException(
                $"Failed to reserve a contiguous double-mapped region after {maxAttempts} attempts"
            );
        }

        return new IORingBuffer(handle, buffer, physicalSize, isPooled, poolIndex);
    }

    private static partial class WindowsNative
    {
        public const nint InvalidHandleValue = -1;
        public const uint MEM_PRESERVE_PLACEHOLDER = 0x02;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_REPLACE_PLACEHOLDER = 0x4000;
        public const uint MEM_RELEASE = 0x8000;
        public const uint MEM_RESERVE_PLACEHOLDER = 0x40000;
        public const uint PAGE_NOACCESS = 0x01;
        public const uint PAGE_READWRITE = 0x04;

        // Access rights for MapViewOfFileEx (FILE_MAP_*, not PAGE_*).
        public const uint FILE_MAP_WRITE = 0x0002;
        public const uint FILE_MAP_READ = 0x0004;

        [LibraryImport("kernelbase.dll", SetLastError = true)]
        public static partial nint VirtualAlloc2(
            nint process, nint address, ulong size, uint allocationType, uint protect,
            nint extendedParameters, uint parameterCount);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool VirtualFree(nint lpAddress, uint dwSize, uint dwFreeType);

        // Legacy reserve/map primitives for the pre-placeholder fallback. Present on every supported Windows.
        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint VirtualAlloc(nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial nint MapViewOfFileEx(
            nint hFileMappingObject, uint dwDesiredAccess,
            uint dwFileOffsetHigh, uint dwFileOffsetLow, nuint dwNumberOfBytesToMap, nint lpBaseAddress);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial nint CreateFileMappingW(
            nint hFile, nint lpFileMappingAttributes, uint flProtect,
            uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string? lpName);

        [LibraryImport("kernelbase.dll", SetLastError = true)]
        public static partial nint MapViewOfFile3(
            nint hFileMappingObject, nint processHandle, nint pvBaseAddress,
            ulong ullOffset, ulong ullSize, uint allocFlags, uint dwDesiredAccess,
            nint hExtendedParameter, int parameterCount);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnmapViewOfFile(nint lpBaseAddress);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CloseHandle(nint hObject);
    }

    #endregion

    #region Linux Implementation

    private static IORingBuffer CreateLinux(int physicalSize, bool isPooled, int poolIndex)
    {
        // Create anonymous file descriptor
        var fd = LinuxNative.memfd_create("ioring_buffer", LinuxNative.MFD_CLOEXEC);
        if (fd < 0)
        {
            throw new InvalidOperationException($"memfd_create failed: {Marshal.GetLastPInvokeError()}");
        }

        // Set the file size
        if (LinuxNative.ftruncate(fd, physicalSize) < 0)
        {
            LinuxNative.close(fd);
            throw new InvalidOperationException($"ftruncate failed: {Marshal.GetLastPInvokeError()}");
        }

        // Reserve virtual address space for both mappings
        var region = LinuxNative.mmap(
            nint.Zero,
            (nuint)(physicalSize * 2),
            LinuxNative.PROT_NONE,
            LinuxNative.MAP_PRIVATE | LinuxNative.MAP_ANONYMOUS,
            -1,
            0
        );

        if (region == LinuxNative.MAP_FAILED)
        {
            LinuxNative.close(fd);
            throw new InvalidOperationException($"mmap (reserve) failed: {Marshal.GetLastPInvokeError()}");
        }

        // Map first view
        var buffer = LinuxNative.mmap(
            region,
            (nuint)physicalSize,
            LinuxNative.PROT_READ | LinuxNative.PROT_WRITE,
            LinuxNative.MAP_SHARED | LinuxNative.MAP_FIXED,
            fd,
            0
        );

        if (buffer == LinuxNative.MAP_FAILED)
        {
            LinuxNative.munmap(region, (nuint)(physicalSize * 2));
            LinuxNative.close(fd);
            throw new InvalidOperationException($"mmap (first) failed: {Marshal.GetLastPInvokeError()}");
        }

        // Map second view (same fd, same offset = same physical memory)
        var view2 = LinuxNative.mmap(
            region + physicalSize,
            (nuint)physicalSize,
            LinuxNative.PROT_READ | LinuxNative.PROT_WRITE,
            LinuxNative.MAP_SHARED | LinuxNative.MAP_FIXED,
            fd,
            0
        );

        if (view2 == LinuxNative.MAP_FAILED)
        {
            LinuxNative.munmap(region, (nuint)(physicalSize * 2));
            LinuxNative.close(fd);
            throw new InvalidOperationException($"mmap (second) failed: {Marshal.GetLastPInvokeError()}");
        }

        return new IORingBuffer(fd, buffer, physicalSize, isPooled, poolIndex);
    }

    private static partial class LinuxNative
    {
        public static readonly nint MAP_FAILED = -1;

        public const int MFD_CLOEXEC = 0x0001;
        public const int PROT_NONE = 0x0;
        public const int PROT_READ = 0x1;
        public const int PROT_WRITE = 0x2;
        public const int MAP_SHARED = 0x01;
        public const int MAP_PRIVATE = 0x02;
        public const int MAP_FIXED = 0x10;
        public const int MAP_ANONYMOUS = 0x20;

        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int memfd_create(string name, uint flags);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int ftruncate(int fd, long length);

        [LibraryImport("libc", SetLastError = true)]
        public static partial nint mmap(nint addr, nuint length, int prot, int flags, int fd, long offset);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int munmap(nint addr, nuint length);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int close(int fd);
    }

    #endregion

    #region BSD/macOS Implementation

    private static int _shmCounter;

    private static IORingBuffer CreateBsd(int physicalSize, bool isPooled, int poolIndex)
    {
        // Create a unique name for the shared memory object
        // macOS has a ~30 char limit for shm names, so keep it short
        var shmName = $"/io{Environment.ProcessId:X}_{Interlocked.Increment(ref _shmCounter):X}";

        // Create shared memory object
        var fd = BsdNative.shm_open(shmName, BsdNative.O_RDWR | BsdNative.O_CREAT | BsdNative.O_EXCL, 0600);
        if (fd < 0)
        {
            throw new InvalidOperationException($"shm_open failed: {Marshal.GetLastPInvokeError()}");
        }

        // Immediately unlink so it gets cleaned up when all fds are closed
        BsdNative.shm_unlink(shmName);

        // Set the file size
        if (BsdNative.ftruncate(fd, physicalSize) < 0)
        {
            BsdNative.close(fd);
            throw new InvalidOperationException($"ftruncate failed: {Marshal.GetLastPInvokeError()}");
        }

        // Reserve virtual address space for both mappings
        var region = BsdNative.mmap(
            nint.Zero,
            (nuint)(physicalSize * 2),
            BsdNative.PROT_NONE,
            BsdNative.MAP_PRIVATE | BsdNative.MAP_ANON,
            -1,
            0
        );

        if (region == BsdNative.MAP_FAILED)
        {
            BsdNative.close(fd);
            throw new InvalidOperationException($"mmap (reserve) failed: {Marshal.GetLastPInvokeError()}");
        }

        // Map first view
        var buffer = BsdNative.mmap(
            region,
            (nuint)physicalSize,
            BsdNative.PROT_READ | BsdNative.PROT_WRITE,
            BsdNative.MAP_SHARED | BsdNative.MAP_FIXED,
            fd,
            0
        );

        if (buffer == BsdNative.MAP_FAILED)
        {
            BsdNative.munmap(region, (nuint)(physicalSize * 2));
            BsdNative.close(fd);
            throw new InvalidOperationException($"mmap (first) failed: {Marshal.GetLastPInvokeError()}");
        }

        // Map second view (same fd, same offset = same physical memory)
        var view2 = BsdNative.mmap(
            region + physicalSize,
            (nuint)physicalSize,
            BsdNative.PROT_READ | BsdNative.PROT_WRITE,
            BsdNative.MAP_SHARED | BsdNative.MAP_FIXED,
            fd,
            0
        );

        if (view2 == BsdNative.MAP_FAILED)
        {
            BsdNative.munmap(region, (nuint)(physicalSize * 2));
            BsdNative.close(fd);
            throw new InvalidOperationException($"mmap (second) failed: {Marshal.GetLastPInvokeError()}");
        }

        return new IORingBuffer(fd, buffer, physicalSize, isPooled, poolIndex);
    }

    private static partial class BsdNative
    {
        public static readonly nint MAP_FAILED = -1;

        public const int O_RDWR = 0x0002;
        public const int O_CREAT = 0x0200;
        public const int O_EXCL = 0x0800;
        public const int PROT_NONE = 0x0;
        public const int PROT_READ = 0x1;
        public const int PROT_WRITE = 0x2;
        public const int MAP_SHARED = 0x0001;
        public const int MAP_PRIVATE = 0x0002;
        public const int MAP_FIXED = 0x0010;
        public const int MAP_ANON = 0x1000;

        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int shm_open(string name, int oflag, int mode);

        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int shm_unlink(string name);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int ftruncate(int fd, long length);

        [LibraryImport("libc", SetLastError = true)]
        public static partial nint mmap(nint addr, nuint length, int prot, int flags, int fd, long offset);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int munmap(nint addr, nuint length);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int close(int fd);
    }

    #endregion

    #region Disposal

    private void ReleaseUnmanagedResources()
    {
        if (_disposed || _buffer == nint.Zero)
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (_handle != nint.Zero)
            {
                WindowsNative.CloseHandle(_handle);
                _handle = nint.Zero;
            }

            if (_buffer != nint.Zero)
            {
                WindowsNative.UnmapViewOfFile(_buffer);
                WindowsNative.UnmapViewOfFile(_buffer + _physicalSize);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (_buffer != nint.Zero)
            {
                LinuxNative.munmap(_buffer, (nuint)(_physicalSize * 2));
            }

            if (_handle != nint.Zero)
            {
                LinuxNative.close((int)_handle);
                _handle = nint.Zero;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            if (_buffer != nint.Zero)
            {
                BsdNative.munmap(_buffer, (nuint)(_physicalSize * 2));
            }

            if (_handle != nint.Zero)
            {
                BsdNative.close((int)_handle);
                _handle = nint.Zero;
            }
        }

        _buffer = nint.Zero;
        _disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~IORingBuffer()
    {
        ReleaseUnmanagedResources();
    }

    #endregion
}
