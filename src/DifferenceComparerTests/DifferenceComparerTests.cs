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
            var diff1 = differenceComparer.GetDifference(state0, state1);
            var diff2 = differenceComparer.GetDifference(state0, state2);
            var diff3 = differenceComparer.GetDifference(state1, state2);
            var diffOfDiff = differenceComparer.GetDifferenceProgression(diff1, diff2);

            var diffOfDiffHashCodeList = diffOfDiff
                .Select(d => d.GetHashCode())
                .ToList();
            var diff3HashCodeList = diff3
                .Select(d => d.GetHashCode())
                .ToList();
            diffOfDiffHashCodeList.Should().BeEquivalentTo(diff3HashCodeList);
        }

        [TestCase(0, 1, 2, 3)]
        [TestCase(0, 1, 3, 2)]
        [TestCase(0, 2, 1, 3)]
        [TestCase(0, 2, 3, 1)]
        [TestCase(0, 3, 1, 2)]
        [TestCase(0, 3, 2, 1)]
        [TestCase(1, 2, 3, 0)]
        [TestCase(1, 2, 0, 3)]
        [TestCase(1, 3, 2, 0)]
        [TestCase(1, 3, 0, 2)]
        [TestCase(1, 0, 2, 3)]
        [TestCase(1, 0, 3, 2)]
        [TestCase(2, 3, 0, 1)]
        [TestCase(2, 3, 1, 0)]
        [TestCase(2, 0, 3, 1)]
        [TestCase(2, 0, 1, 3)]
        [TestCase(2, 1, 3, 0)]
        [TestCase(2, 1, 0, 3)]
        [TestCase(3, 0, 1, 2)]
        [TestCase(3, 0, 2, 1)]
        [TestCase(3, 1, 0, 2)]
        [TestCase(3, 1, 2, 0)]
        [TestCase(3, 2, 0, 1)]
        [TestCase(3, 2, 1, 0)]
        public void GetSquashDifferenceTest(params int[] indexArray)
        {
            var stateArray = indexArray
                .Select(i => SimpleStateList[i])
                .ToArray();
            var differenceComparer = new DifferenceComparer<SimpleTestEntry>(new SimpleTestEntryIdEqualityComparer());
            var diffArray = Enumerable.Range(0, stateArray.Length - 1)
                .Select(i => differenceComparer.GetDifference(stateArray[i], stateArray[i + 1]))
                .ToArray();
            // ReSharper disable once CoVariantArrayConversion
            var squashedDiff = differenceComparer.GetSquashedDifference(diffArray);
            var diffStartToEnd = differenceComparer.GetDifference(stateArray.First(), stateArray.Last());

            var squashedDiffHashCodeList = squashedDiff
                .Select(d => d.GetHashCode())
                .ToList();
            var diffStartToEndHashCodeList = diffStartToEnd
                .Select(d => d.GetHashCode())
                .ToList();
            squashedDiffHashCodeList.Should().BeEquivalentTo(diffStartToEndHashCodeList);
        }
    }
}