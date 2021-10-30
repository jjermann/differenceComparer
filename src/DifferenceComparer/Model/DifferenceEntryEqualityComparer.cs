using System;
using System.Collections.Generic;

namespace DifferenceComparer.Model
{
    public class DifferenceEntryEqualityComparer<T, TU>: IEqualityComparer<DifferenceEntry<T, TU>>
        where T : class
        where TU: notnull
    {
        private readonly IEqualityComparer<T> _entryEqualityComparer;

        public DifferenceEntryEqualityComparer(
            IEqualityComparer<T>? entryEqualityComparer = null)
        {
            _entryEqualityComparer = entryEqualityComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(DifferenceEntry<T, TU>? x, DifferenceEntry<T, TU>? y)
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

            if (!x.Id.Equals(y.Id))
            {
                return false;
            }

            if (x.DifferenceType != y.DifferenceType)
            {
                return false;
            }

            if (!_entryEqualityComparer.Equals(x.EntryBefore, y.EntryBefore))
            {
                return false;
            }

            if (!_entryEqualityComparer.Equals(x.EntryAfter, y.EntryAfter))
            {
                return false;
            }

            return true;
        }

        public int GetHashCode(DifferenceEntry<T, TU> obj)
        {
            return HashCode.Combine(
                obj.Id,
                obj.DifferenceType,
                obj.EntryBefore != null
                    ? _entryEqualityComparer.GetHashCode(obj.EntryBefore)
                    : 0,
                obj.EntryAfter != null
                    ? _entryEqualityComparer.GetHashCode(obj.EntryAfter)
                    : 0);
        }
    }
}