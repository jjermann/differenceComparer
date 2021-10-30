using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DifferenceComparer.Model;
using TestApp.TestData;

namespace TestApp.Mocks
{
    public class SimpleDifferenceDbMock : DbMock<DifferenceEntry<SimpleTestEntry>, string>
    {
        public SimpleDifferenceDbMock()
            : base(x => x.ExampleEntry.Id)
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

        public ICollection<EntryRefDifference<string>> GetAllEntryRefDifferences()
        {
            return EntryDictionary.Values
                .Select(d => d.GetTrivialEntryRefDifference(x => x.Id))
                .ToList();
        }
    }
}
