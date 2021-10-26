using System.Collections.Generic;

namespace DifferenceComparer.Model
{
    public record EntryRef(int Id, int Index = 0)
    {
        public static readonly IEqualityComparer<EntryRef> IdEqualityComparer = new EntryRefIdEqualityComparer();
    }
}