using System.Collections.Generic;
using System.Linq;
using DifferenceComparer;
using FluentAssertions;
using NUnit.Framework;
using TestApp.TestData;

namespace DifferenceComparerTests
{
    public class DifferenceComparerTests
    {
        private static readonly List<SimpleTestEntry>[] SimpleStateList = TestDataGenerator.GetSimpleTestEntryListArray();
        private static object[] _simpleStateTripletData =
        {
            new []
            {
                SimpleStateList[0],
                SimpleStateList[1],
                SimpleStateList[2]
            }
        };

        [TestCaseSource(nameof(_simpleStateTripletData))]
        public void Diff01ToDiff02ShouldCorrespondToDiff12Test(
            List<SimpleTestEntry> state0,
            List<SimpleTestEntry> state1,
            List<SimpleTestEntry> state2)
        {
            var differenceComparer = new DifferenceComparer<SimpleTestEntry>(new SimpleTestEntryIdEqualityComparer());
            var differenceEqualityComparer = differenceComparer.DifferenceEqualityComparer;
            var diff1 = differenceComparer.GetDifference(state0, state1);
            var diff2 = differenceComparer.GetDifference(state0, state2);
            var diff3 = differenceComparer.GetDifference(state1, state2);
            var diffOfDiff = differenceComparer.GetDifferenceProgression(diff1, diff2);

            var diffOfDiffHashCodeList = diffOfDiff
                .Select(d => differenceEqualityComparer.GetHashCode(d))
                .ToList();
            var diff3HashCodeList = diff3
                .Select(d => differenceEqualityComparer.GetHashCode(d))
                .ToList();
            diffOfDiffHashCodeList.Should().BeEquivalentTo(diff3HashCodeList);
        }

        public void GetSquashDifferenceTest(params int[] indexArray)
        {
            var stateArray = indexArray
                .Select(i => SimpleStateList[i])
                .ToArray();
            var differenceComparer = new DifferenceComparer<SimpleTestEntry>(new SimpleTestEntryIdEqualityComparer());
            var differenceEqualityComparer = differenceComparer.DifferenceEqualityComparer;
            var diffArray = Enumerable.Range(0, stateArray.Length - 1)
                .Select(i => differenceComparer.GetDifference(stateArray[i], stateArray[i + 1]))
                .Cast<IList<DifferenceEntry<SimpleTestEntry>>>()
                .ToArray();
            var squashedDiff = differenceComparer.GetSquashedDifference(diffArray);
            var diffStartToEnd = differenceComparer.GetDifference(stateArray.First(), stateArray.Last());

            var squashedDiffHashCodeList = squashedDiff
                .Select(d => differenceEqualityComparer.GetHashCode(d))
                .ToList();
            var diffStartToEndHashCodeList = diffStartToEnd
                .Select(d => differenceEqualityComparer.GetHashCode(d))
                .ToList();
            squashedDiffHashCodeList.Should().BeEquivalentTo(diffStartToEndHashCodeList);
        }
    }
}