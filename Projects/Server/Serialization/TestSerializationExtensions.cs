namespace Server;

public static class TestSerializationExtensions
{
    public static void Write(this IGenericWriter writer, Poison p)
    {
        if (p == null)
        {
            writer.Write((byte)0);
        }
        else
        {
            writer.Write((byte)1);
            writer.Write((byte)p.Level);
        }
    }

    public static Poison ReadPoison(this IGenericReader reader) =>
        reader.ReadByte() switch
        {
            1 => Poison.GetPoison(reader.ReadByte()),
            _ => null
        };
}
