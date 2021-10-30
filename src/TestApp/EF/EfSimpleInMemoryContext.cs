using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TestApp.TestData;

namespace TestApp.EF
{
    public class EfSimpleInMemoryContext : EfInMemoryContext<SimpleTestEntry, string>
    {
        public EfSimpleInMemoryContext(string? dbName = null)
            : base(x => x.Id, dbName)
        { }

        public static EfSimpleInMemoryContext InitializeFromCollection(
            ICollection<SimpleTestEntry> entryCollection,
            string? dbName = null)
        {
            var dbMock = new EfSimpleInMemoryContext(dbName);
            dbMock.Add(entryCollection.ToArray());

            return dbMock;
        }

        public static EfSimpleInMemoryContext InitializeFromJson(
            string json,
            JsonSerializerOptions? options = null,
            string? dbName = null)
        {
            var dbMock = new EfSimpleInMemoryContext(dbName);
            dbMock.AddFromJson(json, options);

            return dbMock;
        }
    }
}
