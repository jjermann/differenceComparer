using System.Text.Json;
using TestApp.TestData;

namespace TestApp.DbMock
{
    public class SimpleDbMock : DbMock<SimpleTestEntry>
    {
        public SimpleDbMock()
            : base(new SimpleTestEntryIdEqualityComparer())
        { }

        public static SimpleDbMock InitializeFromCollection(params SimpleTestEntry[] entryArray)
        {
            var dbMock = new SimpleDbMock();
            dbMock.Add(entryArray);

            return dbMock;
        }

        public static SimpleDbMock InitializeFromJson(
            string json,
            JsonSerializerOptions? options = null)
        {
            var dbMock = new SimpleDbMock();
            dbMock.AddFromJson(json, options);

            return dbMock;
        }
    }
}
