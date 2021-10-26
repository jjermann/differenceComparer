using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DifferenceComparer.Model
{
    public class EquatableDifferenceEntry<T>: DifferenceEntry<T>, IEquatable<EquatableDifferenceEntry<T>>
        where T : class
    {
        private IEqualityComparer<T> EntryIdEqualityComparer { get; }
        private IEqualityComparer<T> EntryEqualityComparer { get; }

        public EquatableDifferenceEntry(
            T? entryBefore,
            T? entryAfter,
            IEqualityComparer<T> entryIdEqualityComparer,
            IEqualityComparer<T>? entryEqualityComparer = null)
            : base(entryBefore, entryAfter)
        {
            EntryIdEqualityComparer = entryIdEqualityComparer;
            EntryEqualityComparer = entryEqualityComparer ?? EqualityComparer<T>.Default;

            if (EntryBefore != null
                && EntryAfter != null
                && !EntryIdEqualityComparer.Equals(EntryBefore, EntryAfter))
            {
                var msg = "The given entries can't have different ids!";
                throw new ArgumentException(msg);
            }
        }

        public override bool Equals(object? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            return Equals((EquatableDifferenceEntry<T>)other);
        }

        public bool Equals(EquatableDifferenceEntry<T>? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            if (DifferenceType != other.DifferenceType)
            {
                return false;
            }

            // Remark: The Id comparison "should" be redundant!
            if (Id != other.Id)
            {
                return false;
            }

            if (!EntryEqualityComparer.Equals(EntryBefore, other.EntryBefore))
            {
                return false;
            }

            if (!EntryEqualityComparer.Equals(EntryAfter, other.EntryAfter))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                DifferenceType,
                Id,
                EntryBefore != null
                    ? EntryEqualityComparer.GetHashCode(EntryBefore)
                    : 0,
                EntryAfter != null
                    ? EntryEqualityComparer.GetHashCode(EntryAfter)
                    : 0);
        }

        public int Id => EntryIdEqualityComparer.GetHashCode(ExampleEntry);
        
        public EquatableDifferenceEntry<T> Clone()
        {
            return new EquatableDifferenceEntry<T>(EntryBefore, EntryAfter, EntryIdEqualityComparer, EntryEqualityComparer);
        }

        public EquatableDifferenceEntry<EntryRef> GetTrivialEntryRefDifference()
        {
            return GetTrivialEntryRefDifference(EntryIdEqualityComparer);
        }

        public EquatableDifferenceEntry<T> GetInverse()
        {
            return new EquatableDifferenceEntry<T>(EntryAfter, EntryBefore, EntryIdEqualityComparer, EntryEqualityComparer);
        }

        public static EquatableDifferenceEntry<T> FromDifferenceEntry(
            DifferenceEntry<T> differenceEntry,
            IEqualityComparer<T> entryIdEqualityComparer,
            IEqualityComparer<T>? entryEqualityComparer = null)
        {
            return new(
                differenceEntry.EntryBefore,
                differenceEntry.EntryAfter,
                entryIdEqualityComparer,
                entryEqualityComparer);
        }

        public static EquatableDifferenceEntry<T> JsonDeserialize(
            string json,
            IEqualityComparer<T> entryIdEqualityComparer,
            IEqualityComparer<T>? entryEqualityComparer = null,
            JsonSerializerOptions? options = null)
        {
            var differenceEntry = JsonSerializer.Deserialize<DifferenceEntry<T>>(json, options);
            if (differenceEntry == null)
            {
                throw new InvalidOperationException("Unable to deserialize the given json!");
            }

            return FromDifferenceEntry(
                differenceEntry,
                entryIdEqualityComparer,
                entryEqualityComparer);
        }
    }
}