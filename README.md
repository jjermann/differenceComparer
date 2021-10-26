# DifferenceComparer
A tool to generate and work with data state differences (with the same schema).

# Motivation
The main idea and application behind `DifferenceComparer` is to be able to work with differences instead of whole data states.
This has several benefits and is particularly useful for regression tests:
- Differences take up a lot less space compared to two full data sets.
- If multiple result sets are compared the space gain is even bigger.
- Differences (especially in regression tests) are often more meaningful.

# Reference Example / Challenges
Differences also provide more challenges. To illustrate this let's assume we have a situation with 3 data states S0, S1, S2.
where S0 could correspond to an initial data set and S1/S2 to some changes to the initial data set.

- Example/Challenge A: Parallel changes
  This could be a batch1 run (S0 -> S1) and a batch2 run (S0 -> S2).
  Batch2 could e.g. be batch1 with some minor code changes for which we want to do regression tests.
  In this situation one would usually have the initial state S0 (to be able to run the batch) and Diff(S0, S1) as a reference difference.
  When running the regression tests for batch2 we would consequently get Diff(S0, S2).
  Of interest is usually the differences between the end states S1 and S2 i.e. Diff(S1, S2).
  This poses the following challenge (difference progression):
  How do we calculate Diff(S1, S2) from Diff(S0, S1) and Diff(S0, S2)?

- Example/Challenge B: Consecutive changes
  This could be a sequence of changes (S0 -> S1 -> S2).
  Let's again assume that we only have S0, Diff(S0, S1), Diff(S1, S2) and we are interested in the overall difference Diff(S0, S2).
  This poses the following challenge (squashed progressions):
  How do we calculate Diff(S1, S2) from Diff(S0, S1) and Diff(S0, S2)?

`DifferenceComparer` provides algorithmic answers for both cases (assuming the schema doesn't change):

## Difference progression
Also see Example A above...
![Difference progression diagram](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/jjermann/differenceComparer/main/doc/differenceProgression.puml)

## Squashed progressions
Also see Example B above...
![Difference squash diagram](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/jjermann/differenceComparer/main/doc/differenceSquash.puml)

# Basic syntax and examples
See [TestApp](https://github.com/jjermann/differenceComparer/blob/main/src/TestApp/Program.cs) for an example application (it uses a small DbMock).
## Classes
- ```DifferenceEntry<T>```
- ```EquatableDifferenceEntry<T>: DifferenceEntry<T>, IEquatable<EquatableDifferenceEntry<T>>```
- ```EntryRef(int Id, int Index = 0)```

## Constructor
```
public DifferenceComparer(
    IEqualityComparer<T> entryIdEqualityComparer,
    IEqualityComparer<T>? equalityComparer = null)
```

## (De)serialize
```
public string SerializeDifference(
    ICollection<DifferenceEntry<T>> differenceEntryCollection,
    JsonSerializerOptions? options = null)
```
```
public ICollection<DifferenceEntry<T>> DeserializeDifference(
    string json,
    JsonSerializerOptions? options = null)
```

## Difference between data sets
```
public List<DifferenceEntry<T>> GetDifference(
    in ICollection<T> col1,
    in ICollection<T> col2)
```
```
public List<DifferenceEntry<T>> GetDifference(
    in ICollection<EquatableDifferenceEntry<EntryRef>> entryRefDifferenceCollection,
    in IEnumerable<T> data1,
    in IEnumerable<T> data2)
```

## Difference progression
```
public List<DifferenceEntry<T>> GetDifferenceProgression(
    in ICollection<EquatableDifferenceEntry<EntryRef>> entryRefDifferenceCollection,
    in IEnumerable<DifferenceEntry<T>> differenceData1,
    in IEnumerable<DifferenceEntry<T>> differenceData2)
```

```
public List<DifferenceEntry<T>> GetDifferenceProgression(
    in ICollection<DifferenceEntry<T>> differenceList1,
    in ICollection<DifferenceEntry<T>> differenceList2)
```

## Support methods
```
public EquatableDifferenceEntry<T> ToEquatableDifferenceEntry(DifferenceEntry<T> differenceEntry)
```
```
public List<EquatableDifferenceEntry<EntryRef>> GetEntryRefDifference(
    in ICollection<int> col1,
    in ICollection<int> col2)
```
```
public List<EquatableDifferenceEntry<EntryRef>> GetEntryRefDifferenceProgression(
    in ICollection<EquatableDifferenceEntry<EntryRef>> entryRefDifferenceList1,
    in ICollection<EquatableDifferenceEntry<EntryRef>> entryRefDifferenceList2)
```

## Squashed differences
```
public List<DifferenceEntry<T>> GetSquashedDifference(params ICollection<DifferenceEntry<T>>[] differenceCollectionArray)
```

# Limitations
- `DifferenceComparer` assumes that the schema remains unchanged.
