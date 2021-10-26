using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DifferenceComparer.Model
{
    public class DifferenceEntry<T>
        where T : class
    {
        public T? EntryBefore { get; }
        public T? EntryAfter { get; }

        public DifferenceEntry(
            T? entryBefore,
            T? entryAfter)
        {
            EntryBefore = entryBefore;
            EntryAfter = entryAfter;

            if (EntryBefore == null
                && EntryAfter == null)
            {
                var msg = "At least one entry must not be null!";
                throw new ArgumentException(msg);
            }
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

        public EquatableDifferenceEntry<EntryRef> GetTrivialEntryRefDifference(
            IEqualityComparer<T> entryIdEqualityComparer)
        {
            return new EquatableDifferenceEntry<EntryRef>(
                EntryBefore != null
                    ? new EntryRef(entryIdEqualityComparer.GetHashCode(EntryBefore))
                    : null,
                EntryAfter != null
                    ? new EntryRef(entryIdEqualityComparer.GetHashCode(EntryAfter))
                    : null,
                EntryRef.IdEqualityComparer);
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        public string JsonSerialize(JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(this, options);
        }
    }
}