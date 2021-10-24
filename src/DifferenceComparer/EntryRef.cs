using System.Collections.Generic;

namespace DifferenceComparer
{
    public record EntryRef(int Id, int Index = 0)
    {
        public static IEqualityComparer<EntryRef> IdEqualityComparer = new EntryRefIdEqualityComparer();
    }
}