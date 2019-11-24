// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server;

namespace Libuv.Internal
{
  public class LibuvFunctions
  {
    public LibuvFunctions()
    {
      _uv_loop_init = NativeMethods.uv_loop_init;
      _uv_loop_close = NativeMethods.uv_loop_close;
      _uv_run = NativeMethods.uv_run;
      _uv_stop = NativeMethods.uv_stop;
      _uv_ref = NativeMethods.uv_ref;
      _uv_unref = NativeMethods.uv_unref;
      _uv_fileno = NativeMethods.uv_fileno;
      _uv_close = NativeMethods.uv_close;
      _uv_async_init = NativeMethods.uv_async_init;
      _uv_async_send = NativeMethods.uv_async_send;
      _uv_unsafe_async_send = NativeMethods.uv_unsafe_async_send;
      _uv_tcp_init = NativeMethods.uv_tcp_init;
      _uv_tcp_bind = NativeMethods.uv_tcp_bind;
      _uv_tcp_open = NativeMethods.uv_tcp_open;
      _uv_tcp_nodelay = NativeMethods.uv_tcp_nodelay;
      _uv_pipe_init = NativeMethods.uv_pipe_init;
      _uv_pipe_bind = NativeMethods.uv_pipe_bind;
      _uv_pipe_open = NativeMethods.uv_pipe_open;
      _uv_listen = NativeMethods.uv_listen;
      _uv_accept = NativeMethods.uv_accept;
      _uv_pipe_connect = NativeMethods.uv_pipe_connect;
      _uv_pipe_pending_count = NativeMethods.uv_pipe_pending_count;
      _uv_read_start = NativeMethods.uv_read_start;
      _uv_read_stop = NativeMethods.uv_read_stop;
      _uv_try_write = NativeMethods.uv_try_write;
      unsafe
      {
        _uv_write = NativeMethods.uv_write;
        _uv_write2 = NativeMethods.uv_write2;
      }
      _uv_err_name = NativeMethods.uv_err_name;
      _uv_strerror = NativeMethods.uv_strerror;
      _uv_loop_size = NativeMethods.uv_loop_size;
      _uv_handle_size = NativeMethods.uv_handle_size;
      _uv_req_size = NativeMethods.uv_req_size;
      _uv_ip4_addr = NativeMethods.uv_ip4_addr;
      _uv_ip6_addr = NativeMethods.uv_ip6_addr;
      _uv_tcp_getpeername = NativeMethods.uv_tcp_getpeername;
      _uv_tcp_getsockname = NativeMethods.uv_tcp_getsockname;
      _uv_walk = NativeMethods.uv_walk;
      _uv_timer_init = NativeMethods.uv_timer_init;
      _uv_timer_start = NativeMethods.uv_timer_start;
      _uv_timer_stop = NativeMethods.uv_timer_stop;
      _uv_now = NativeMethods.uv_now;
    }

    // Second ctor that doesn't set any fields only to be used by MockLibuv
    public LibuvFunctions(bool onlyForTesting)
    {
    }

    public void ThrowIfErrored(int statusCode)
    {
      // Note: method is explicitly small so the success case is easily inlined
      if (statusCode < 0) ThrowError(statusCode);
    }

    private void ThrowError(int statusCode)
    {
      // Note: only has one throw block so it will marked as "Does not return" by the jit
      // and not inlined into previous function, while also marking as a function
      // that does not need cpu register prep to call (see: https://github.com/dotnet/coreclr/pull/6103)
      throw GetError(statusCode);
    }

    public void Check(int statusCode, out UvException error)
    {
      // Note: method is explicitly small so the success case is easily inlined
      error = statusCode < 0 ? GetError(statusCode) : null;
    }

    // Note: method marked as NoInlining so it doesn't bloat either of the two preceding functions
    // Check and ThrowError and alter their jit heuristics.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private UvException GetError(int statusCode) =>
      new UvException($"Error {statusCode} {err_name(statusCode)} {strerror(statusCode)}", statusCode);

    public Func<UvLoopHandle, int> _uv_loop_init;
    public void loop_init(UvLoopHandle handle)
    {
      ThrowIfErrored(_uv_loop_init(handle));
    }

    public Func<IntPtr, int> _uv_loop_close;
    public void loop_close(UvLoopHandle handle)
    {
      handle.Validate(true);
      ThrowIfErrored(_uv_loop_close(handle.InternalGetHandle()));
    }

    public Func<UvLoopHandle, int, int> _uv_run;
    public void run(UvLoopHandle handle, int mode)
    {
      handle.Validate();
      ThrowIfErrored(_uv_run(handle, mode));
    }

    public Action<UvLoopHandle> _uv_stop;
    public void stop(UvLoopHandle handle)
    {
      handle.Validate();
      _uv_stop(handle);
    }

    public Action<UvHandle> _uv_ref;
    public void @ref(UvHandle handle)
    {
      handle.Validate();
      _uv_ref(handle);
    }

    public Action<UvHandle> _uv_unref;
    public void unref(UvHandle handle)
    {
      handle.Validate();
      _uv_unref(handle);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int uv_fileno_func(UvHandle handle, ref IntPtr socket);
    public uv_fileno_func _uv_fileno;
    public void uv_fileno(UvHandle handle, ref IntPtr socket)
    {
      handle.Validate();
      ThrowIfErrored(_uv_fileno(handle, ref socket));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_close_cb(IntPtr handle);
    public Action<IntPtr, uv_close_cb> _uv_close;
    public void close(UvHandle handle, uv_close_cb close_cb)
    {
      handle.Validate(true);
      _uv_close(handle.InternalGetHandle(), close_cb);
    }

    public void close(IntPtr handle, uv_close_cb close_cb)
    {
      _uv_close(handle, close_cb);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_async_cb(IntPtr handle);
    public Func<UvLoopHandle, UvAsyncHandle, uv_async_cb, int> _uv_async_init;
    public void async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb)
    {
      loop.Validate();
      handle.Validate();
      ThrowIfErrored(_uv_async_init(loop, handle, cb));
    }

    public Func<UvAsyncHandle, int> _uv_async_send;
    public void async_send(UvAsyncHandle handle)
    {
      ThrowIfErrored(_uv_async_send(handle));
    }

    public Func<IntPtr, int> _uv_unsafe_async_send;
    public void unsafe_async_send(IntPtr handle)
    {
      ThrowIfErrored(_uv_unsafe_async_send(handle));
    }

    public Func<UvLoopHandle, UvTcpHandle, int> _uv_tcp_init;
    public void tcp_init(UvLoopHandle loop, UvTcpHandle handle)
    {
      loop.Validate();
      handle.Validate();
      ThrowIfErrored(_uv_tcp_init(loop, handle));
    }

    public delegate int uv_tcp_bind_func(UvTcpHandle handle, ref SockAddr addr, int flags);
    public uv_tcp_bind_func _uv_tcp_bind;
    public void tcp_bind(UvTcpHandle handle, ref SockAddr addr, int flags)
    {
      handle.Validate();
      ThrowIfErrored(_uv_tcp_bind(handle, ref addr, flags));
    }

    public Func<UvTcpHandle, IntPtr, int> _uv_tcp_open;
    public void tcp_open(UvTcpHandle handle, IntPtr hSocket)
    {
      handle.Validate();
      ThrowIfErrored(_uv_tcp_open(handle, hSocket));
    }

    public Func<UvTcpHandle, int, int> _uv_tcp_nodelay;
    public void tcp_nodelay(UvTcpHandle handle, bool enable)
    {
      handle.Validate();
      ThrowIfErrored(_uv_tcp_nodelay(handle, enable ? 1 : 0));
    }

    public Func<UvLoopHandle, UvPipeHandle, int, int> _uv_pipe_init;
    public void pipe_init(UvLoopHandle loop, UvPipeHandle handle, bool ipc)
    {
      loop.Validate();
      handle.Validate();
      ThrowIfErrored(_uv_pipe_init(loop, handle, ipc ? -1 : 0));
    }

    public Func<UvPipeHandle, string, int> _uv_pipe_bind;
    public void pipe_bind(UvPipeHandle handle, string name)
    {
      handle.Validate();
      ThrowIfErrored(_uv_pipe_bind(handle, name));
    }

    public Func<UvPipeHandle, IntPtr, int> _uv_pipe_open;
    public void pipe_open(UvPipeHandle handle, IntPtr hSocket)
    {
      handle.Validate();
      ThrowIfErrored(_uv_pipe_open(handle, hSocket));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_connection_cb(IntPtr server, int status);
    public Func<UvStreamHandle, int, uv_connection_cb, int> _uv_listen;
    public void listen(UvStreamHandle handle, int backlog, uv_connection_cb cb)
    {
      handle.Validate();
      ThrowIfErrored(_uv_listen(handle, backlog, cb));
    }

    public Func<UvStreamHandle, UvStreamHandle, int> _uv_accept;
    public void accept(UvStreamHandle server, UvStreamHandle client)
    {
      server.Validate();
      client.Validate();
      ThrowIfErrored(_uv_accept(server, client));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_connect_cb(IntPtr req, int status);
    public Action<UvConnectRequest, UvPipeHandle, string, uv_connect_cb> _uv_pipe_connect;
    public void pipe_connect(UvConnectRequest req, UvPipeHandle handle, string name, uv_connect_cb cb)
    {
      req.Validate();
      handle.Validate();
      _uv_pipe_connect(req, handle, name, cb);
    }

    public Func<UvPipeHandle, int> _uv_pipe_pending_count;
    public int pipe_pending_count(UvPipeHandle handle)
    {
      handle.Validate();
      return _uv_pipe_pending_count(handle);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_alloc_cb(IntPtr server, int suggested_size, out uv_buf_t buf);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_read_cb(IntPtr server, int nread, ref uv_buf_t buf);
    public Func<UvStreamHandle, uv_alloc_cb, uv_read_cb, int> _uv_read_start;
    public void read_start(UvStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb)
    {
      handle.Validate();
      ThrowIfErrored(_uv_read_start(handle, alloc_cb, read_cb));
    }

    public Func<UvStreamHandle, int> _uv_read_stop;
    public void read_stop(UvStreamHandle handle)
    {
      handle.Validate();
      ThrowIfErrored(_uv_read_stop(handle));
    }

    public Func<UvStreamHandle, uv_buf_t[], int, int> _uv_try_write;
    public int try_write(UvStreamHandle handle, uv_buf_t[] bufs, int nbufs)
    {
      handle.Validate();
      var count = _uv_try_write(handle, bufs, nbufs);
      ThrowIfErrored(count);
      return count;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_write_cb(IntPtr req, int status);

    public unsafe delegate int uv_write_func(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb);
    public uv_write_func _uv_write;
    public unsafe void write(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb)
    {
      req.Validate();
      handle.Validate();
      ThrowIfErrored(_uv_write(req, handle, bufs, nbufs, cb));
    }

    public unsafe delegate int uv_write2_func(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, UvStreamHandle sendHandle, uv_write_cb cb);
    public uv_write2_func _uv_write2;
    public unsafe void write2(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, UvStreamHandle sendHandle, uv_write_cb cb)
    {
      req.Validate();
      handle.Validate();
      ThrowIfErrored(_uv_write2(req, handle, bufs, nbufs, sendHandle, cb));
    }

    public Func<int, IntPtr> _uv_err_name;
    public string err_name(int err)
    {
      IntPtr ptr = _uv_err_name(err);
      return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }

    public Func<int, IntPtr> _uv_strerror;
    public string strerror(int err)
    {
      IntPtr ptr = _uv_strerror(err);
      return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }

    public Func<int> _uv_loop_size;
    public int loop_size() => _uv_loop_size();

    public Func<HandleType, int> _uv_handle_size;
    public int handle_size(HandleType handleType) => _uv_handle_size(handleType);

    public Func<RequestType, int> _uv_req_size;
    public int req_size(RequestType reqType) => _uv_req_size(reqType);

    public delegate int uv_ip4_addr_func(string ip, int port, out SockAddr addr);
    public uv_ip4_addr_func _uv_ip4_addr;
    public void ip4_addr(string ip, int port, out SockAddr addr, out UvException error)
    {
      Check(_uv_ip4_addr(ip, port, out addr), out error);
    }

    public delegate int uv_ip6_addr_func(string ip, int port, out SockAddr addr);
    public uv_ip6_addr_func _uv_ip6_addr;
    public void ip6_addr(string ip, int port, out SockAddr addr, out UvException error)
    {
      Check(_uv_ip6_addr(ip, port, out addr), out error);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_walk_cb(IntPtr handle, IntPtr arg);
    public Func<UvLoopHandle, uv_walk_cb, IntPtr, int> _uv_walk;
    public void walk(UvLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg)
    {
      loop.Validate();
      _uv_walk(loop, walk_cb, arg);
    }

    public Func<UvLoopHandle, UvTimerHandle, int> _uv_timer_init;
    public void timer_init(UvLoopHandle loop, UvTimerHandle handle)
    {
      loop.Validate();
      handle.Validate();
      ThrowIfErrored(_uv_timer_init(loop, handle));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void uv_timer_cb(IntPtr handle);
    public Func<UvTimerHandle, uv_timer_cb, long, long, int> _uv_timer_start;
    public void timer_start(UvTimerHandle handle, uv_timer_cb cb, long timeout, long repeat)
    {
      handle.Validate();
      ThrowIfErrored(_uv_timer_start(handle, cb, timeout, repeat));
    }

    public Func<UvTimerHandle, int> _uv_timer_stop;
    public void timer_stop(UvTimerHandle handle)
    {
      handle.Validate();
      ThrowIfErrored(_uv_timer_stop(handle));
    }

    public Func<UvLoopHandle, long> _uv_now;
    public long now(UvLoopHandle loop)
    {
      loop.Validate();
      return _uv_now(loop);
    }

    public delegate int uv_tcp_getsockname_func(UvTcpHandle handle, out SockAddr addr, ref int namelen);
    public uv_tcp_getsockname_func _uv_tcp_getsockname;
    public void tcp_getsockname(UvTcpHandle handle, out SockAddr addr, ref int namelen)
    {
      handle.Validate();
      ThrowIfErrored(_uv_tcp_getsockname(handle, out addr, ref namelen));
    }

    public delegate int uv_tcp_getpeername_func(UvTcpHandle handle, out SockAddr addr, ref int namelen);
    public uv_tcp_getpeername_func _uv_tcp_getpeername;
    public void tcp_getpeername(UvTcpHandle handle, out SockAddr addr, ref int namelen)
    {
      handle.Validate();
      ThrowIfErrored(_uv_tcp_getpeername(handle, out addr, ref namelen));
    }

    public uv_buf_t buf_init(IntPtr memory, int len) => new uv_buf_t(memory, len, Core.IsWindows);

    public struct uv_buf_t
    {
      // this type represents a WSABUF struct on Windows
      // https://msdn.microsoft.com/en-us/library/windows/desktop/ms741542(v=vs.85).aspx
      // and an iovec struct on *nix
      // http://man7.org/linux/man-pages/man2/readv.2.html
      // because the order of the fields in these structs is different, the field
      // names in this type don't have meaningful symbolic names. instead, they are
      // assigned in the correct order by the constructor at runtime

      private readonly IntPtr _field0;
      private readonly IntPtr _field1;

      public uv_buf_t(IntPtr memory, int len, bool windows)
      {
        if (windows)
        {
          _field0 = (IntPtr)len;
          _field1 = memory;
        }
        else
        {
          _field0 = memory;
          _field1 = (IntPtr)len;
        }
      }
    }

    public enum HandleType
    {
      Unknown = 0,
      ASYNC,
      CHECK,
      FS_EVENT,
      FS_POLL,
      HANDLE,
      IDLE,
      NAMED_PIPE,
      POLL,
      PREPARE,
      PROCESS,
      STREAM,
      TCP,
      TIMER,
      TTY,
      UDP,
      SIGNAL,
    }

    public enum RequestType
    {
      Unknown = 0,
      REQ,
      CONNECT,
      WRITE,
      SHUTDOWN,
      UDP_SEND,
      FS,
      WORK,
      GETADDRINFO,
      GETNAMEINFO,
    }

    private static class NativeMethods
    {
      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_loop_init(UvLoopHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_loop_close(IntPtr a0);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_run(UvLoopHandle handle, int mode);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern void uv_stop(UvLoopHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern void uv_ref(UvHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern void uv_unref(UvHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_fileno(UvHandle handle, ref IntPtr socket);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern void uv_close(IntPtr handle, uv_close_cb close_cb);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_async_init(UvLoopHandle loop, UvAsyncHandle handle, uv_async_cb cb);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_async_send(UvAsyncHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl, EntryPoint = "uv_async_send")]
      public static extern int uv_unsafe_async_send(IntPtr handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_tcp_init(UvLoopHandle loop, UvTcpHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_tcp_bind(UvTcpHandle handle, ref SockAddr addr, int flags);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_tcp_open(UvTcpHandle handle, IntPtr hSocket);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_tcp_nodelay(UvTcpHandle handle, int enable);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_pipe_init(UvLoopHandle loop, UvPipeHandle handle, int ipc);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_pipe_bind(UvPipeHandle loop, string name);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_pipe_open(UvPipeHandle handle, IntPtr hSocket);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_listen(UvStreamHandle handle, int backlog, uv_connection_cb cb);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_accept(UvStreamHandle server, UvStreamHandle client);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
      public static extern void uv_pipe_connect(UvConnectRequest req, UvPipeHandle handle, string name, uv_connect_cb cb);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_pipe_pending_count(UvPipeHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_read_start(UvStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_read_stop(UvStreamHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_try_write(UvStreamHandle handle, uv_buf_t[] bufs, int nbufs);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern unsafe int uv_write(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern unsafe int uv_write2(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, UvStreamHandle sendHandle, uv_write_cb cb);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern IntPtr uv_err_name(int err);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern IntPtr uv_strerror(int err);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_loop_size();

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_handle_size(HandleType handleType);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_req_size(RequestType reqType);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_ip4_addr(string ip, int port, out SockAddr addr);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_ip6_addr(string ip, int port, out SockAddr addr);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_tcp_getsockname(UvTcpHandle handle, out SockAddr name, ref int namelen);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_tcp_getpeername(UvTcpHandle handle, out SockAddr name, ref int namelen);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_walk(UvLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_timer_init(UvLoopHandle loop, UvTimerHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_timer_start(UvTimerHandle handle, uv_timer_cb cb, long timeout, long repeat);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern int uv_timer_stop(UvTimerHandle handle);

      [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
      public static extern long uv_now(UvLoopHandle loop);

      [DllImport("WS2_32.dll", CallingConvention = CallingConvention.Winapi)]
      public static extern unsafe int WSAIoctl(
        IntPtr socket,
        int dwIoControlCode,
        int* lpvInBuffer,
        uint cbInBuffer,
        int* lpvOutBuffer,
        int cbOutBuffer,
        out uint lpcbBytesReturned,
        IntPtr lpOverlapped,
        IntPtr lpCompletionRoutine
      );

      [DllImport("WS2_32.dll", CallingConvention = CallingConvention.Winapi)]
      public static extern int WSAGetLastError();
    }
  }
}
