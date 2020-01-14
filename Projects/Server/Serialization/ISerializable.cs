namespace Server
{
  public interface ISerializable
  {
    BufferWriter SaveBuffer { get; }
    int TypeReference { get; }
    uint SerialIdentity { get; }
    void Serialize();
    void Serialize(IGenericWriter writer);
  }
}
