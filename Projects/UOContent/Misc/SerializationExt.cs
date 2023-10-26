using Server.Accounting;

namespace Server;

public static class SerializationExt
{
    public static IAccount ReadAccount(this IGenericReader reader)
    {
        return reader.ReadByte() switch
        {
            0 => null,
            1 => Accounts.GetAccount(reader.ReadStringRaw()),
            2 => Accounts.FindAccount(reader.ReadSerial())
        };
    }

    public static void Write(this IGenericWriter writer, IAccount acct)
    {
        if (acct == null)
        {
            writer.Write((byte)0);
        }
        else
        {
            writer.Write((byte)2);
            writer.Write(acct.Serial);
        }
    }
}
