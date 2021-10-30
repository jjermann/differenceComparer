using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DifferenceComparer.Model
{
    public class DifferenceEntry<T, TU>
        where T : class
        where TU: notnull
    {
        public TU Id { get; init; } = default!;
        public T? EntryBefore { get; init; }
        public T? EntryAfter { get; init; }

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