using System.Collections.Generic;

namespace SerializableMigration;

public class SerializableMetadataComparer : IComparer<SerializableMetadata>
{
    public int Compare(SerializableMetadata x, SerializableMetadata y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (ReferenceEquals(null, y))
        {
            return 1;
        }

        if (ReferenceEquals(null, x))
        {
            return -1;
        }

        return x.Version.CompareTo(y.Version);
    }
}