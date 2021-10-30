using System.Diagnostics;
using System.IO;
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
            var data = TestDataGenerator.GetSimpleTestEntryDictionary();
            var data0 = data["db0.json"].ToArray();
            var data1 = data["db1.json"].ToArray();
            var data2 = data["db2.json"].ToArray();

            // Alternative way:
            //var data0 = TestDataGenerator.GetSimpleTestEntryList0().ToArray();
            //var data1 = TestDataGenerator.GetSimpleTestEntryList1().ToArray();
            //var data2 = TestDataGenerator.GetSimpleTestEntryList2().ToArray();

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

            // Tests with large databases
            RunEfLargeDataTests();

            // Generate TestData
            GenerateTestData();
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
            var differenceEqualityComparer = new DifferenceEntryEqualityComparer<SimpleTestEntry, string>();
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
            var differenceDifferenceComparer = new DifferenceComparer<DifferenceEntry<SimpleTestEntry, string>, string>(
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
            var efDifferenceDb01 = EfSimpleDifferenceInMemoryContext.InitializeFromCollection(efDifference01);
            var efDifferenceDb02 = EfSimpleDifferenceInMemoryContext.InitializeFromCollection(efDifference02);
            var efDifferenceDb12 = EfSimpleDifferenceInMemoryContext.InitializeFromCollection(efDifference12);
            var efDifferenceProgression = efDbComparer.GetDbDifferenceProgression(
                efDifferenceDb01.EntrySet,
                efDifferenceDb02.EntrySet);
            var efSquashedProgression = efDbComparer.GetDbSquashedDifference(
                efDifferenceDb01.EntrySet,
                efDifferenceDb12.EntrySet);

            var differenceDifferenceComparer = new DifferenceComparer<DifferenceEntry<SimpleTestEntry, string>, string>(
                x => x.ExampleEntry.Id);
            var differenceDifferenceForSquashed = differenceDifferenceComparer.GetDifference(
                efSquashedProgression,
                efDifference02);
            var areDifferencesEqualForSquashed = !differenceDifferenceForSquashed.Any();
            Debug.Assert(areDifferencesEqualForSquashed);

            var differenceDifferenceForProgression = differenceDifferenceComparer.GetDifference(
                efDifferenceProgression,
                efDifference12);
            var areDifferencesEqualForProgression = !differenceDifferenceForProgression.Any();
            Debug.Assert(areDifferencesEqualForProgression);
        }

        private static void RunEfLargeDataTests()
        {
            var largeStringA = new string('A', 1000);
            var largeStringB = new string('B', 1000);
            var largeStringC = new string('C', 1000);
            var testEntryArray1 = Enumerable.Range(0, 100000)
                .Select(i => new SimpleTestEntry(
                    i.ToString(),
                    $"{largeStringA}{i}",
                    $"{largeStringB}{i}",
                    $"{largeStringC}{i}"))
                .ToArray();
            var testEntryArray2 = Enumerable.Range(0, 100000)
                .Select(i => new SimpleTestEntry(
                    i.ToString(),
                    $"{largeStringA}{i}",
                    $"{largeStringB}{i}",
                    $"{largeStringC}{i}"))
                .ToArray();
            for (var i = 0; i < 200; i++)
            {
                testEntryArray2[i * i] = testEntryArray2[i * i] with { ColumnA = $"NewA{i * i}" };
                testEntryArray2[i * 500] = testEntryArray2[i * 500] with { ColumnB = $"NewB{i * 500}" };
                testEntryArray2[i * 500 + 1] = testEntryArray2[i * 500 + 1] with { Id = $"NewId{i * 500 + 1}" };
            }
            testEntryArray2[42] = testEntryArray2[42] with { ColumnC = "42" };
            var testEntryArray0 = new []
            {
                testEntryArray1[0] with { ColumnA = "Change1" },
                testEntryArray1[30000] with { ColumnB = "Change2" },
                testEntryArray1[42] with { ColumnC = "Change3" },
                testEntryArray1[3] with { Id = "ChangeId" }
            };

            using var efDb0 = new EfSimpleInMemoryContext();
            using var efDb1 = new EfSimpleInMemoryContext();
            using var efDb2 = new EfSimpleInMemoryContext();
            efDb0.Add(testEntryArray0);
            efDb1.Add(testEntryArray1);
            efDb2.Add(testEntryArray2);

            var efDbComparer = new EfDbComparer<SimpleTestEntry, string>(x => x.Id);
            var efDiff12 = efDbComparer.GetDbDifference(efDb1.EntrySet, efDb2.EntrySet);
            var efDiff01 = efDbComparer.GetDbDifference(efDb0.EntrySet, efDb1.EntrySet);
            var efDiff02 = efDbComparer.GetDbDifference(efDb0.EntrySet, efDb2.EntrySet);
            //var diffProgression = efDbComparer.GetDifferenceProgression(diff01, diff02);
            var differenceComparer = new DifferenceComparer<SimpleTestEntry, string>(x => x.Id);
            var diff12 = differenceComparer.GetDifference(efDb1.GetAll(), efDb2.GetAll());
            var diff01 = differenceComparer.GetDifference(efDb0.GetAll(), efDb1.GetAll());
            var diff02 = differenceComparer.GetDifference(efDb0.GetAll(), efDb2.GetAll());
        }

        // Remark: This regenerates the existing .json files in TestData.
        private static void GenerateTestData()
        {
            var testAppDirectory = new DirectoryInfo(".").Parent!.Parent!.Parent!.FullName;
            var testDataDirectory = Path.Combine(testAppDirectory, "TestData");
            var differenceComparer = new DifferenceComparer<SimpleTestEntry, string>(x => x.Id);
            var dbMock0 = SimpleDbMock.InitializeFromCollection(TestDataGenerator.GetSimpleTestEntryList0().ToArray());
            var dbMock1 = SimpleDbMock.InitializeFromCollection(TestDataGenerator.GetSimpleTestEntryList1().ToArray());
            var dbMock2 = SimpleDbMock.InitializeFromCollection(TestDataGenerator.GetSimpleTestEntryList2().ToArray());
            var dbMock3 = SimpleDbMock.InitializeFromCollection(TestDataGenerator.GetSimpleTestEntryList3().ToArray());
            var diffDb01 = differenceComparer.GetDifference(dbMock0.GetAll(), dbMock1.GetAll());
            var diffDb02 = differenceComparer.GetDifference(dbMock0.GetAll(), dbMock2.GetAll());
            var diffDb03 = differenceComparer.GetDifference(dbMock0.GetAll(), dbMock3.GetAll());
            var diffDb12 = differenceComparer.GetDifference(dbMock1.GetAll(), dbMock2.GetAll());
            var diffDb23 = differenceComparer.GetDifference(dbMock2.GetAll(), dbMock3.GetAll());

            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntry", "db0.json"),
                dbMock0.SerializeToJson());
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntry", "db1.json"),
                dbMock1.SerializeToJson());
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntry", "db2.json"),
                dbMock2.SerializeToJson());
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntry", "db3.json"),
                dbMock3.SerializeToJson());
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntryDifference", "diffDb01.json"),
                differenceComparer.SerializeDifference(diffDb01));
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntryDifference", "diffDb02.json"),
                differenceComparer.SerializeDifference(diffDb02));
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntryDifference", "diffDb03.json"),
                differenceComparer.SerializeDifference(diffDb03));
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntryDifference", "diffDb12.json"),
                differenceComparer.SerializeDifference(diffDb12));
            File.WriteAllText(
                Path.Combine(testDataDirectory, "SimpleTestEntryDifference", "diffDb23.json"),
                differenceComparer.SerializeDifference(diffDb23));
        }
    }
}
