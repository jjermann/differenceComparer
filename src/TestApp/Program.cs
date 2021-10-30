using System.Diagnostics;
using System.Linq;
using DifferenceComparer;
using DifferenceComparer.Model;
using TestApp.EF;
using TestApp.Mocks;
using TestApp.TestData;

namespace TestApp
{
    public static class DbProgram
    {
        static void Main()
        {
            // Initialize data
            var data0 = TestDataGenerator.GetSimpleTestEntryList0().ToArray();
            var data1 = TestDataGenerator.GetSimpleTestEntryList1().ToArray();
            var data2 = TestDataGenerator.GetSimpleTestEntryList2().ToArray();

            // SimpleDbMock tests
            var dbMock0 = SimpleDbMock.InitializeFromCollection(data0);
            var dbMock1 = SimpleDbMock.InitializeFromCollection(data1);
            var dbMock2 = SimpleDbMock.InitializeFromCollection(data2);
            RunTests(dbMock0, dbMock1, dbMock2);

            // EfSimpleInMemoryContext tests
            using var efDb0 = EfSimpleInMemoryContext.InitializeFromCollection(data0);
            using var efDb1 = EfSimpleInMemoryContext.InitializeFromCollection(data1);
            using var efDb2 = EfSimpleInMemoryContext.InitializeFromCollection(data2);
            RunTests(efDb0, efDb1, efDb2);

            // EfDbComparer tests
            RunEfDbComparerTests(efDb0, efDb1, efDb2);
        }

        // ReSharper disable UnusedVariable
        private static void RunTests(
            IDbMock<SimpleTestEntry, string> dbMock0,
            IDbMock<SimpleTestEntry, string> dbMock1,
            IDbMock<SimpleTestEntry, string> dbMock2)
        {
            // Create DifferenceComparer
            var differenceComparer = new DifferenceComparer<SimpleTestEntry, string>(
                x => x.Id);
            
            // Normal / easy way to generate differences
            var difference = differenceComparer.GetDifference(
                dbMock0.GetAll(),
                dbMock2.GetAll());

            // Memory efficient way to generate differences:
            // 1. Fetch all ids from data set 1 and 2.
            // 2. Generate EntryRefDifference based on the ids.
            // 3. Generate Difference using the EntryRefDifference and data sets as enumerables.
            var entryRefDifference = differenceComparer.GetEntryRefDifference(
                dbMock0.GetAllIds(),
                dbMock2.GetAllIds());
            var efficientDifference = differenceComparer.GetDifference(
                entryRefDifference,
                dbMock0.GetAllAsOrderedEnumerable(),
                dbMock2.GetAllAsOrderedEnumerable());

            // (De)serialize Difference
            var serialized = differenceComparer.SerializeDifference(efficientDifference);
            var deserialized = differenceComparer.DeserializeDifference(serialized);

            // (De)serialize DbMock
            var differenceDbMock = SimpleDifferenceDbMock.InitializeFromCollection(efficientDifference.ToArray());
            var serializedDb = differenceDbMock.SerializeToJson();
            var deserializedDb = SimpleDifferenceDbMock.InitializeFromJson(serializedDb);

            // Prepare differences between 3 states (and corresponding DbMocks) to illustrate further concepts
            var difference01 = differenceComparer.GetDifference(dbMock0.GetAll(), dbMock1.GetAll());
            var difference12 = differenceComparer.GetDifference(dbMock1.GetAll(), dbMock2.GetAll());
            var difference02 = differenceComparer.GetDifference(dbMock0.GetAll(), dbMock2.GetAll());
            var difference01DbMock = SimpleDifferenceDbMock.InitializeFromCollection(difference01.ToArray());
            var difference12DbMock = SimpleDifferenceDbMock.InitializeFromCollection(difference12.ToArray());
            var difference02DbMock = SimpleDifferenceDbMock.InitializeFromCollection(difference02.ToArray());

            // Difference progression (differenceProgression is the same as difference02)
            // Note: Alternative one could just use difference01/difference02 as input.
            var differenceProgression = differenceComparer.GetDifferenceProgression(
                difference01DbMock.GetAll(),
                difference02DbMock.GetAll());

            // Memory efficient way to generate a difference progression:
            // 1. Fetch all ids / EntryRefs from difference data set 1 and 2 (done inefficiently here).
            // 2. Generate EntryRefDifferenceProgression based on the DifferenceEntryRefs (basically the ids).
            // 3. Generate Difference using the EntryRefDifferenceProgression and difference data sets as enumerables.
            var entryRefDifferenceProgression = differenceComparer.GetEntryRefDifferenceProgression(
                    difference01DbMock.GetAllEntryRefDifferences(),
                    difference02DbMock.GetAllEntryRefDifferences());
            var efficientDifferenceProgression = differenceComparer.GetDifferenceProgression(
                entryRefDifferenceProgression,
                difference01DbMock.GetAllAsOrderedEnumerable(),
                difference02DbMock.GetAllAsOrderedEnumerable());

            // Squashed differences (squashedDifference is the same as difference02)
            var squashedDifference = differenceComparer.GetSquashedDifference(
                difference01,
                difference12);

            // Note: Usually we don't compare arbitrary differences but if needed
            // one could do it by looping over all Ids:
            var differenceEqualityComparer = new DifferenceEntryEqualityComparer<SimpleTestEntry>();
            var squashedIdDictionary = squashedDifference
                .ToDictionary(
                    d => d.ExampleEntry.Id,
                    d => d);
            var difference02IdDictionary = difference02
                .ToDictionary(
                    d => d.ExampleEntry.Id,
                    d => d);
            var areIdsEqual = !squashedIdDictionary.Keys.Except(difference02IdDictionary.Keys).Any()
                              && !difference02IdDictionary.Keys.Except(squashedIdDictionary.Keys).Any();
            Debug.Assert(areIdsEqual);
            var areEntriesEqual = squashedIdDictionary.Keys
                .All(id => differenceEqualityComparer.Equals(
                    squashedIdDictionary[id],
                    difference02IdDictionary[id]));
            Debug.Assert(areEntriesEqual);

            // Or (more fun): We could use a difference DifferenceComparer...
            var differenceDifferenceComparer = new DifferenceComparer<DifferenceEntry<SimpleTestEntry>, string>(
                x => x.ExampleEntry.Id,
                differenceEqualityComparer);
            var differenceDifferenceForSquashed = differenceDifferenceComparer.GetDifference(
                squashedDifference,
                difference02);
            var areDifferencesEqualForSquashed = !differenceDifferenceForSquashed.Any();
            Debug.Assert(areDifferencesEqualForSquashed);

            var differenceDifferenceForProgression = differenceDifferenceComparer.GetDifference(
                differenceProgression,
                difference12);
            var areDifferencesEqualForProgression = !differenceDifferenceForProgression.Any();
            Debug.Assert(areDifferencesEqualForProgression);
        }

        private static void RunEfDbComparerTests(
            EfSimpleInMemoryContext efDb0,
            EfSimpleInMemoryContext efDb1,
            EfSimpleInMemoryContext efDb2)
        {
            var efDbComparer = new EfDbComparer<SimpleTestEntry, string>(x => x.Id);
            var efDifference01 = efDbComparer.GetDbDifference(efDb0.EntrySet, efDb1.EntrySet);
            var efDifference02 = efDbComparer.GetDbDifference(efDb0.EntrySet, efDb2.EntrySet);
            var efDifference12 = efDbComparer.GetDbDifference(efDb1.EntrySet, efDb2.EntrySet);
        }
    }
}
