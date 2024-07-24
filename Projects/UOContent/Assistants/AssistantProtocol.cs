using System.Buffers;
using System.Runtime.CompilerServices;

namespace Server.Network;

public static class AssistantProtocol
{
    private static PacketHandler[] _handlers;

    [CallPriority(10)]
    public static void Configure()
    {
        _handlers = ProtocolExtensions<AssistantsProtocolInfo>.Register(new AssistantsProtocolInfo());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Register(int cmd, bool ingame, delegate*<NetState, SpanReader, void> onReceive) =>
        Register(cmd, ingame, false, onReceive);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Register(int cmd, bool ingame, bool outgame, delegate*<NetState, SpanReader, void> onReceive) =>
        _handlers[cmd] = new PacketHandler(cmd, onReceive, inGameOnly: ingame, outGameOnly: outgame);

    private struct AssistantsProtocolInfo : IProtocolExtensionsInfo
    {
        public int PacketId => 0xF0;
    }
}
