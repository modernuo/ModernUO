using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SerializableMigration
{
    public class SerializableFieldFlagComparer : IComparer<(IMethodSymbol, int)>
    {
        public int Compare((IMethodSymbol, int) x, (IMethodSymbol, int) y)
        {
            var (methodSymbolX, orderX) = x;
            var (methodSymbolY, orderY) = y;

            if (ReferenceEquals(methodSymbolX, methodSymbolY))
            {
                return 0;
            }

            if (ReferenceEquals(null, methodSymbolY))
            {
                return 1;
            }

            if (ReferenceEquals(null, methodSymbolX))
            {
                return -1;
            }

            return orderX.CompareTo(orderY);
        }
    }
}
