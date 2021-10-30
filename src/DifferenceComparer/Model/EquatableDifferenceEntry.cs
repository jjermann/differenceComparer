using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DifferenceComparer.Model
{
    public class EquatableDifferenceEntry<T, TU> :
        DifferenceEntry<T, TU>,
        IEquatable<EquatableDifferenceEntry<T, TU>>
        where T : class
        where TU : notnull
    {
        public Func<T, TU> EntryIdSelector { get; }
        public IEqualityComparer<T> EntryEqualityComparer { get; }

        public EquatableDifferenceEntry(
            T? entryBefore,
            T? entryAfter,
            Func<T, TU> entryIdSelector,
            IEqualityComparer<T>? entryEqualityComparer = null)
        {
            EntryBefore = entryBefore;
            EntryAfter = entryAfter;
            Id = entryIdSelector(ExampleEntry);
            EntryIdSelector = entryIdSelector;
            EntryEqualityComparer = entryEqualityComparer ?? EqualityComparer<T>.Default;

            if (EntryBefore == null
                && EntryAfter == null)
            {
                var msg = "At least one entry must not be null!";
                throw new ArgumentException(msg);
            }

            if (EntryBefore != null
                && EntryAfter != null
                && !EntryIdSelector(EntryBefore).Equals(EntryIdSelector(EntryAfter)))
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

            return Equals((EquatableDifferenceEntry<T, TU>)other);
        }

        public bool Equals(EquatableDifferenceEntry<T, TU>? other)
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
            if (!Id.Equals(other.Id))
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

        public EquatableDifferenceEntry<T, TU> Clone()
        {
            return new EquatableDifferenceEntry<T, TU>(
                EntryBefore,
                EntryAfter,
                EntryIdSelector,
                EntryEqualityComparer);
        }

        public EquatableDifferenceEntry<T, TU> GetInverse()
        {
            return new EquatableDifferenceEntry<T, TU>(
                EntryAfter,
                EntryBefore,
                EntryIdSelector,
                EntryEqualityComparer);
        }

        public static EquatableDifferenceEntry<T, TU> FromDifferenceEntry(
            DifferenceEntry<T, TU> differenceEntry,
            Func<T, TU> entryIdSelector,
            IEqualityComparer<T>? entryEqualityComparer = null)
        {
            return new(
                differenceEntry.EntryBefore,
                differenceEntry.EntryAfter,
                entryIdSelector,
                entryEqualityComparer);
        }

        public static EquatableDifferenceEntry<T, TU> JsonDeserialize(
            string json,
            Func<T, TU> entryIdSelector,
            IEqualityComparer<T>? entryEqualityComparer = null,
            JsonSerializerOptions? options = null)
        {
            var differenceEntry = JsonSerializer.Deserialize<DifferenceEntry<T, TU>>(json, options);
            if (differenceEntry == null)
            {
                throw new InvalidOperationException("Unable to deserialize the given json!");
            }

            return FromDifferenceEntry(
                differenceEntry,
                entryIdSelector,
                entryEqualityComparer);
        }
    }
}