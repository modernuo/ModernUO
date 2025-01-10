using System;
using System.Runtime.CompilerServices;

namespace Server.Engines.Help;

public static class HelpEvents
{
    public static event Action<PageEntry> PageEnqueued;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePageEnqueued(PageEntry e) => PageEnqueued?.Invoke(e);

    public static event Action<PageEntry> PageRemoved;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePageRemoved(PageEntry e) => PageRemoved?.Invoke(e);

    public static event Action<Mobile, Mobile, PageEntry> PageHandlerChanged;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePageHandlerChanged(Mobile old, Mobile value, PageEntry e) => PageHandlerChanged?.Invoke(old, value, e);

    public static event Action<PageEntry> PageWaiting;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokePageWaiting(PageEntry e) => PageWaiting?.Invoke(e);

    public static event Action<Mobile> StuckMenu;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeStuckMenu(Mobile m) => StuckMenu?.Invoke(m);
}
