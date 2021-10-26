using System.Collections.Generic;

namespace DifferenceComparer.Model
{
    public class DifferenceEntryIdEqualityComparer<T>: IEqualityComparer<DifferenceEntry<T>>
        where T : class
    {
        private readonly IEqualityComparer<T> _entryIdEqualityComparer;

        public DifferenceEntryIdEqualityComparer(IEqualityComparer<T> entryIdEqualityComparer)
        {
            _entryIdEqualityComparer = entryIdEqualityComparer;
        }

        public bool Equals(DifferenceEntry<T>? x, DifferenceEntry<T>? y)
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

            return _entryIdEqualityComparer.Equals(x.ExampleEntry, y.ExampleEntry);
        }

        public int GetHashCode(DifferenceEntry<T> obj)
        {
            return _entryIdEqualityComparer.GetHashCode(obj.ExampleEntry);
        }
    }
}