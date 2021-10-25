using DifferenceComparer;
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

        // ReSharper disable UnusedVariable
        private static void Test()
        {
            var dbMockBefore = SimpleDbMock.InitializeFromCollection(
                TestDataGenerator.GetSimpleTestEntryList0().ToArray());
            var dbMockAfter = SimpleDbMock.InitializeFromCollection(
                TestDataGenerator.GetSimpleTestEntryList1().ToArray());

            var differenceComparer = new DifferenceComparer<SimpleTestEntry>(
                new SimpleTestEntryIdEqualityComparer());
            
            var normalDifferenceList = differenceComparer.GetDifference(
                dbMockBefore.GetAll(),
                dbMockAfter.GetAll());

            var entryRefDifferenceList = differenceComparer.GetEntryRefDifference(
                dbMockBefore.GetAllIds(),
                dbMockAfter.GetAllIds());
            var performantDifferenceList = differenceComparer.GetDifference(
                entryRefDifferenceList,
                dbMockBefore.GetAllEnumerable(),
                dbMockAfter.GetAllEnumerable());

            var serialized = differenceComparer.SerializeDifference(performantDifferenceList);
            var deserialized = differenceComparer.DeserializeDifference(serialized);
        }
    }
}
