using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        [JsonConverter(typeof(JsonStringEnumConverter))]
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

        [JsonIgnore]
        [NotNull]
        public T ExampleEntry => EntryBefore ?? EntryAfter!;

        [JsonIgnore]
        public int Id => EntryIdEqualityComparer.GetHashCode(ExampleEntry);

        public DifferenceEntry<T> Clone()
        {
            return new DifferenceEntry<T>(EntryBefore, EntryAfter, EntryIdEqualityComparer, EntryEqualityComparer);
        }

        public DifferenceEntry<EntryRef> GetTrivialEntryRefDifference()
        {
            return new DifferenceEntry<EntryRef>(
                EntryBefore != null
                    ? new EntryRef(Id)
                    : null,
                EntryAfter != null
                    ? new EntryRef(Id)
                    : null,
                EntryRef.IdEqualityComparer);
        }

        public DifferenceEntry<T> GetInverse()
        {
            return new DifferenceEntry<T>(EntryAfter, EntryBefore, EntryIdEqualityComparer, EntryEqualityComparer);
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}