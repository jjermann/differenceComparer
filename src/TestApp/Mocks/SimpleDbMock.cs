using System.Text.Json;
using TestApp.TestData;

namespace TestApp.Mocks
{
    public class SimpleDbMock : DbMock<SimpleTestEntry, string>
    {
        public SimpleDbMock(): base(x => x.Id)
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
