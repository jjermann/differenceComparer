using DifferenceComparer;
using FluentAssertions;
using NUnit.Framework;
using TestApp.TestData;

namespace DifferenceComparerTests
{
    public class RandomDbComparerTests
    {
        [TestCase(30)]
        public void RandomDiff01ToDiff02ShouldCorrespondToDiff12Test(int n)
        {
            var dbComparer = new DifferenceComparer<SimpleTestEntry>(new SimpleTestEntryIdEqualityComparer());
            var dbState0 = TestDataGenerator.GetRandomSimpleTestEntryList(n, 2*n);
            var dbState1 = TestDataGenerator.GetRandomSimpleTestEntryList(n, 2*n);
            var dbState2 = TestDataGenerator.GetRandomSimpleTestEntryList(n, 2*n);
            var diff1 = dbComparer.GetDifference(dbState0, dbState1);
            var diff2 = dbComparer.GetDifference(dbState0, dbState2);
            var diff3 = dbComparer.GetDifference(dbState1, dbState2);
            var diffOfDiff = dbComparer.GetDifferenceProgression(diff1, diff2);

            diffOfDiff.Should().BeEquivalentTo(diff3);
        }
    }
}