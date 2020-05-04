namespace Server.Network
{
  public sealed class ObjectHelpResponse : Packet
  {
    public ObjectHelpResponse(IEntity e, string text) : base(0xB7)
    {
      EnsureCapacity(9 + text.Length * 2);

      Stream.Write(e.Serial);
      Stream.WriteBigUniNull(text);
    }
  }
}
