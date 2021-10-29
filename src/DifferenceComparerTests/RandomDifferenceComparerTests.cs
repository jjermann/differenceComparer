using DifferenceComparer;
using FluentAssertions;
using NUnit.Framework;
using TestApp.TestData;

namespace DifferenceComparerTests
{
    public class RandomDifferenceComparerTests
    {
        [TestCase(30)]
        public void RandomDiff01ToDiff02ShouldCorrespondToDiff12Test(int n)
        {
            var differenceComparer = new DifferenceComparer<SimpleTestEntry, string>(x => x.Id);
            var state0 = TestDataGenerator.GetRandomSimpleTestEntryList(n, 2*n);
            var state1 = TestDataGenerator.GetRandomSimpleTestEntryList(n, 2*n);
            var state2 = TestDataGenerator.GetRandomSimpleTestEntryList(n, 2*n);
            var diff1 = differenceComparer.GetDifference(state0, state1);
            var diff2 = differenceComparer.GetDifference(state0, state2);
            var diff3 = differenceComparer.GetDifference(state1, state2);
            var differenceProgression = differenceComparer.GetDifferenceProgression(diff1, diff2);

            differenceProgression.Should().BeEquivalentTo(diff3);
        }
    }
}