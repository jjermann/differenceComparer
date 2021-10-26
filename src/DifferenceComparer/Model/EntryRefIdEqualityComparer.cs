using System.Collections.Generic;

namespace DifferenceComparer.Model
{
    internal class EntryRefIdEqualityComparer : IEqualityComparer<EntryRef>
    {
        public bool Equals(EntryRef? x, EntryRef? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Id == y.Id;
        }

        public int GetHashCode(EntryRef obj)
        {
            return obj.Id;
        }
    }
}