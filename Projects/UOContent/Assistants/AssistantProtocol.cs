namespace Server.Network;

public static class AssistantProtocol
{
    private static PacketHandler[] _handlers;

    [CallPriority(10)]
    public static void Configure()
    {
        _handlers = ProtocolExtensions<AssistantsProtocolInfo>.Register(new AssistantsProtocolInfo());
    }

    public static unsafe void Register(int cmd, bool ingame, delegate*<NetState, CircularBufferReader, int, void> onReceive) =>
        _handlers[cmd] = new PacketHandler(cmd, 0, ingame, onReceive);

    private struct AssistantsProtocolInfo : IProtocolExtensionsInfo
    {
        public int PacketId => 0xF0;
    }
}
