using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DifferenceComparer.Model;
using TestApp.TestData;

namespace TestApp.Mocks
{
    public class SimpleDifferenceDbMock : DbMock<DifferenceEntry<SimpleTestEntry>>
    {
        public static readonly IEqualityComparer<SimpleTestEntry> BaseEntryIdEqualityComparer =
            new SimpleTestEntryIdEqualityComparer();

        public SimpleDifferenceDbMock()
            : base(new DifferenceEntryIdEqualityComparer<SimpleTestEntry>(
                BaseEntryIdEqualityComparer))
        { }

        public static SimpleDifferenceDbMock InitializeFromCollection(params DifferenceEntry<SimpleTestEntry>[] entryArray)
        {
            var dbMock = new SimpleDifferenceDbMock();
            dbMock.Add(entryArray);

            return dbMock;
        }

        public static SimpleDifferenceDbMock InitializeFromJson(
            string json,
            JsonSerializerOptions? options = null)
        {
            var dbMock = new SimpleDifferenceDbMock();
            dbMock.AddFromJson(json, options);

            return dbMock;
        }

        public ICollection<EquatableDifferenceEntry<EntryRef>> GetAllDifferenceEntryRefs()
        {
            return EntryDictionary.Values
                .Select(d => d.GetTrivialEntryRefDifference(
                    BaseEntryIdEqualityComparer))
                .ToList();
        }
    }
}
