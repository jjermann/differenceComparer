using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DifferenceComparer
{
    public class DifferenceEntry<T> : IEquatable<DifferenceEntry<T>>
        where T : class
    {
        private IEqualityComparer<T> EntryIdEqualityComparer { get; }
        private IEqualityComparer<T> EntryEqualityComparer { get; }

        public T? EntryBefore { get; }
        public T? EntryAfter { get; }

        public DifferenceEntry(
            T? entryBefore,
            T? entryAfter,
            IEqualityComparer<T> entryIdEqualityComparer,
            IEqualityComparer<T>? entryEqualityComparer = null)
        {
            EntryBefore = entryBefore;
            EntryAfter = entryAfter;
            EntryIdEqualityComparer = entryIdEqualityComparer;
            EntryEqualityComparer = entryEqualityComparer ?? EqualityComparer<T>.Default;

            if (EntryBefore == null
                && EntryAfter == null)
            {
                var msg = "At least one entry must not be null!";
                throw new ArgumentException(msg);
            }

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

            return Equals((DifferenceEntry<T>)other);
        }

        public bool Equals(DifferenceEntry<T>? other)
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

        [NotNull]
        public T ExampleEntry => EntryBefore ?? EntryAfter!;

        public int Id => EntryIdEqualityComparer.GetHashCode(ExampleEntry);

        public DifferenceEntry<T> Clone()
        {
            return new DifferenceEntry<T>(EntryBefore, EntryAfter, EntryIdEqualityComparer, EntryEqualityComparer);
        }

        public string GetStringRepresentation(
            Func<T, string>? entryToStringFunc = null,
            Func<T, T, string>? entryDifferenceToStringFunc = null)
        {
            entryToStringFunc ??= entry => $"{typeof(T).Name}({EntryIdEqualityComparer.GetHashCode(entry)})";
            entryDifferenceToStringFunc ??= (entryBefore, entryAfter) => $"{entryToStringFunc(entryBefore)} -> {entryToStringFunc(entryAfter)}";

            var result = DifferenceType switch
            {
                DifferenceType.Add => $"Add:    {entryToStringFunc(EntryAfter!)}",
                DifferenceType.Delete => $"Delete: {entryToStringFunc(EntryBefore!)}",
                DifferenceType.Update => $"Update: {entryDifferenceToStringFunc(EntryBefore!, EntryAfter!)}",
                _ => throw new NotImplementedException()
            };

            return result;
        }
    }
}