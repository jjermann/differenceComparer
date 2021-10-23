using TestApp.DbMock;
using TestApp.TestData;

namespace TestApp
{
    public static class DbProgram
    {
        static void Main()
        {
            Test();
        }

        private static void Test()
        {
            var dbMockComparer = new DbMockComparer<SimpleTestEntry>(new SimpleTestEntryIdEqualityComparer());

            var dbMockBefore = new DbMock<SimpleTestEntry>(new SimpleTestEntryIdEqualityComparer());
            dbMockBefore.Add(TestDataGenerator.GetSimpleTestEntryList0().ToArray());

            var dbMockAfter = new DbMock<SimpleTestEntry>(new SimpleTestEntryIdEqualityComparer());
            dbMockAfter.Add(TestDataGenerator.GetSimpleTestEntryList1().ToArray());

            // ReSharper disable once UnusedVariable
            var differenceList = dbMockComparer.GetDifference(dbMockBefore, dbMockAfter);
        }
    }
}
