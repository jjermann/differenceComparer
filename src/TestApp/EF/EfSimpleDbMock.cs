using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TestApp.TestData;

namespace TestApp.EF
{
    public class EfSimpleDbMock : EfDbMock<SimpleTestEntry, string>
    {
        public EfSimpleDbMock(string? dbName = null)
            : base(x => x.Id, dbName)
        { }

        public static EfSimpleDbMock InitializeFromCollection(
            ICollection<SimpleTestEntry> entryCollection,
            string? dbName = null)
        {
            var dbMock = new EfSimpleDbMock(dbName);
            dbMock.Add(entryCollection.ToArray());

            return dbMock;
        }

        public static EfSimpleDbMock InitializeFromJson(
            string json,
            JsonSerializerOptions? options = null,
            string? dbName = null)
        {
            var dbMock = new EfSimpleDbMock(dbName);
            dbMock.AddFromJson(json, options);

            return dbMock;
        }
    }
}
