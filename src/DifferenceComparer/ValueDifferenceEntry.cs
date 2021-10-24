using System;
using System.Collections.Generic;

namespace DifferenceComparer
{
    public class ValueDifferenceEntry<T> : IEquatable<ValueDifferenceEntry<T>>
        where T : struct
    {
        private IEqualityComparer<T> EntryIdEqualityComparer { get; }
        private IEqualityComparer<T> EntryEqualityComparer { get; }

        public T? EntryBefore { get; }
        public T? EntryAfter { get; }

        public ValueDifferenceEntry(
            T? entryBefore,
            T? entryAfter,
            IEqualityComparer<T>? entryIdEqualityComparer,
            IEqualityComparer<T>? entryEqualityComparer)
        {
            EntryBefore = entryBefore;
            EntryAfter = entryAfter;
            EntryIdEqualityComparer = entryIdEqualityComparer ?? EqualityComparer<T>.Default;
            EntryEqualityComparer = entryEqualityComparer ?? EqualityComparer<T>.Default;

            if (!EntryBefore.HasValue
                && !EntryAfter.HasValue)
            {
                var msg = "At least one entry must not be null!";
                throw new ArgumentException(msg);
            }

            if (EntryBefore.HasValue
                && EntryAfter.HasValue
                && !EntryIdEqualityComparer.Equals(EntryBefore.Value, EntryAfter.Value))
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

            return Equals((ValueDifferenceEntry<T>)other);
        }

        public bool Equals(ValueDifferenceEntry<T>? other)
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

            var areEntryBeforeEqual = !EntryBefore.HasValue && !other.EntryBefore.HasValue
                                     || EntryBefore.HasValue && other.EntryBefore.HasValue && EntryEqualityComparer.Equals(EntryBefore.Value, other.EntryBefore.Value);
            var areEntryAfterEqual = !EntryAfter.HasValue && !other.EntryAfter.HasValue
                                      || EntryAfter.HasValue && other.EntryAfter.HasValue && EntryEqualityComparer.Equals(EntryAfter.Value, other.EntryAfter.Value);

            return areEntryBeforeEqual && areEntryAfterEqual;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                DifferenceType,
                Id,
                EntryBefore.HasValue
                    ? EntryEqualityComparer.GetHashCode(EntryBefore.Value)
                    : 0,
                EntryAfter.HasValue
                    ? EntryEqualityComparer.GetHashCode(EntryAfter.Value)
                    : 0);
        }

        public DifferenceType DifferenceType
        {
            get
            {
                if (EntryBefore == null)
                {
                    return DifferenceType.Add;
                }

                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (EntryAfter == null)
                {
                    return DifferenceType.Delete;
                }

                return DifferenceType.Update;
            }
        }

        public T ExampleEntry => EntryBefore ?? EntryAfter!.Value;

        public int Id => EntryIdEqualityComparer.GetHashCode(ExampleEntry);

        public ValueDifferenceEntry<T> Clone()
        {
            return new ValueDifferenceEntry<T>(EntryBefore, EntryAfter, EntryIdEqualityComparer, EntryEqualityComparer);
        }

        public string GetStringRepresentation(
            Func<T, string>? entryToStringFunc = null,
            Func<T, T, string>? entryDifferenceToStringFunc = null)
        {
            entryToStringFunc ??= entry => $"{typeof(T).Name}({EntryIdEqualityComparer.GetHashCode(entry)})";
            entryDifferenceToStringFunc ??= (entryBefore, entryAfter) => $"{entryToStringFunc(entryBefore)} -> {entryToStringFunc(entryAfter)}";

            var result = DifferenceType switch
            {
                DifferenceType.Add => $"Add:    {entryToStringFunc(ExampleEntry)}",
                DifferenceType.Delete => $"Delete: {entryToStringFunc(ExampleEntry)}",
                DifferenceType.Update => $"Update: {entryDifferenceToStringFunc(EntryBefore!.Value, EntryAfter!.Value)}",
                _ => throw new NotImplementedException()
            };

            return result;
        }
    }
}