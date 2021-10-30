# DifferenceComparer
A tool to generate and work with data state differences (without schema changes).


# Motivation
The main idea and application behind `DifferenceComparer` is to be able to work with differences instead of whole data states.
This has several benefits and is particularly useful for regression tests:
- Differences between data take up a lot less space compared to two full data sets.
- If multiple results are compared the space gain in using differences is even bigger.
- Differences (especially in regression tests) are often more meaningful.


# Reference Example / Challenges
Differences also provide more challenges. To illustrate this let's assume we have a situation with 3 data states `S0`, `S1`, `S2`.
where `S0` could correspond to an initial data state and `S1`/`S2` to some changes to the initial data state.

- Example/Challenge A: Parallel changes
  This could be a `Batch1` run (`S0` -> `S1`) and a `Batch2` run (`S0` -> `S2`).
  Batch2 could e.g. be Batch1 with some minor code changes for which we run regression tests.
  In this situation one would usually have the initial state `S0` (to be able to run the batch) and `Diff(S0, S1)` as a reference difference.
  When running the regression tests for Batch2 we would consequently get `Diff(S0, S2)`.
  Of interest is usually the differences between the end states `S1` and `S2` i.e. `Diff(S1, S2)`.
  This poses the following challenge (difference progression):
  How do we calculate `Diff(S1, S2)` from `Diff(S0, S1)` and `Diff(S0, S2)`?

- Example/Challenge B: Consecutive changes
  This could be a sequence of changes (`S0` -> `S1` -> `S2`).
  Let's again assume that we only have `S0`, `Diff(S0, S1)`, `Diff(S1, S2)` and we are interested in the overall difference `Diff(S0, S2)`.
  This poses the following challenge (squashed differences):
  How do we calculate `Diff(S1, S2)` from `Diff(S0, S1)` and `Diff(S0, S2)`?

A main challenge is obviously also how to efficiently retrieve the difference `Diff(S0, S1)` between two data states `S0` and `S1`.

`DifferenceComparer` provides algorithmic answers to these challenges (assuming the schema remains unchanged):

## Difference progression
Also see Example A above...
![Difference progression diagram](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/jjermann/differenceComparer/develop/doc/differenceProgression.puml)

## Squashed differences
Also see Example B above...
![Difference squash diagram](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.githubusercontent.com/jjermann/differenceComparer/develop/doc/differenceSquash.puml)


# Basic syntax and examples
See [TestApp](https://github.com/jjermann/differenceComparer/blob/develop/src/TestApp/Program.cs) for an example application (it uses a small DbMock).
## Classes
- ```DifferenceEntry<T, TU>```
  This is the simple core `data` behind the difference (between two states) of individual entries.
  A `DifferenceEntry` is given by and `Id` of type `TU` and nullable `EntryBefore` and `EntryAfter` of type `T`.
  `EntryBefore = null` (and `EntryAfter != null`) corresponds to `Add`.
  `EntryAfter = null` (and `EntryBefore != null`) corresponds to `Delete`.
  `EntryBefore != null and EntryAfter != null` corresponds to `Update`.
  They can be (de)serialized (provided `T` and `TU` can be) but the class can't compare entries yet.

- ```EquatableDifferenceEntry<T, TU>```
  This extends a `DifferenceEntry<T, TU>` with an id selector (which maps an entry to it's `Id` of type `TU`) and an entry equality comparer.

- ```EntryRef(TU Id, int Index = 0)```
  This represents a `reference` to a data entry based on its `Id`.
  The optional `Index` parameter can be used to further distinguish entries from different data states (indexed by `Index`).
  If `T` is large compared to `TU` then entry references can be used for more efficient difference calculations.
  Essentially by first working with `EntryRef` data and then fetching the corresponding entries.

-- ```EntryRefDifference<TU>```
  This represents a `reference` to a `DifferenceEntry` based on it's `Id` and (nullable) index indicators for `EntryBefore` and `EntryAfter`.
  If `T` is large compared to `TU` then `EntryRefDifference` can be used for more efficient difference calculations.
  Essentially by first working with `EntryRefDifference` data and then fetching the corresponding entry differences.

-- ```DifferenceComparer<T, TU>```
  The main class.
  Used to compare and work with differences of data states of type `T` with `Id`s of type `TU`.
  The class needs an `Id` selector and an equality comparer for the entries.

### Generic types `T` and `TU`
The generic entry type `T` must be a class and (for difference comparisons) requires an `EqualityComparer`.
The generic `Id` type `TU` must be not null and is assumed to have `nice` "equatable" behavior.
The `Id` is determined from the entry type `T` by using an `Id` selector (`Func<T, TU>`).

For example:
`T` could be an entity type with an additional `EqualityComparer` or it could be a record / equatable class type.
`TU` could be a usual `Id` type like `int`, `Guid`, `string` with a simple property selector as `Id` selector.

## DifferenceComparer methods
### (De)serialize
Differences (`DifferenceEntry<T, TU>`) can be (deserialized):
```
public string SerializeDifference(
    ICollection<DifferenceEntry<T, TU>> differenceEntryCollection,
    JsonSerializerOptions? options = null)
```
```
public ICollection<DifferenceEntry<T, TU>> DeserializeDifference(
    string json,
    JsonSerializerOptions? options = null)
```

### Difference between data sets
The difference between two data states can be calculated with:
```
public List<DifferenceEntry<T, TU>> GetDifference(
    in ICollection<T> col1,
    in ICollection<T> col2)
```

A more (memory) efficient version is available which first (as a prelimenary step)
requires one to calculate the difference between `Id`'s (of type `TU`) using e.g.
```
public List<EntryRefDifference<TU>> GetEntryRefDifference(
    in ICollection<TU> col1,
    in ICollection<TU> col2)
```

Using the `EntryRef` difference and an enumeration for the data entries
in both data sets the difference can be calculated with:
```
public List<DifferenceEntry<T, TU>> GetDifference(
    in ICollection<EntryRefDifference<TU>> entryRefDifferenceCollection,
    in IEnumerable<T> data1,
    in IEnumerable<T> data2)
```
This allows (in principle) to iterate through the data entries instead of loading both data set as a whole into memory.

### Difference progression
The difference progression can be calculated with:
```
public List<DifferenceEntry<T, TU>> GetDifferenceProgression(
    in ICollection<DifferenceEntry<T, TU>> differenceList1,
    in ICollection<DifferenceEntry<T, TU>> differenceList2)
```

A more (memory) efficient version is available which first (as a prelimenary step)
requires one to calculate the difference progression between `EntryRef<TU>` differences using e.g.
```
public List<EntryRefDifference<TU>> GetEntryRefDifferenceProgression(
    in ICollection<EntryRefDifference<TU>> entryRefDifferenceList1,
    in ICollection<EntryRefDifference<TU>> entryRefDifferenceList2)
```

Using the `EntryRef` difference progression and an enumeration for the difference entries
in both data sets the difference progression can be calculated with:
```
public List<DifferenceEntry<T, TU>> GetDifferenceProgression(
    in ICollection<EntryRefDifference<TU>> entryRefDifferenceCollection,
    in IEnumerable<DifferenceEntry<T, TU>> differenceData1,
    in IEnumerable<DifferenceEntry<T, TU>> differenceData2)
```

### Squashed differences
Squashed differences can be calculated with:
```
public List<DifferenceEntry<T, TU>> GetSquashedDifference(params ICollection<DifferenceEntry<T, TU>>[] differenceCollectionArray)
```

Currently no memory efficient version is available...

### Support methods
To convert a `DifferenceEntry<T, TU>` to an `EquatableDifferenceEntry<T, TU>` the following support method can be used:
```
public EquatableDifferenceEntry<T, TU> ToEquatableDifferenceEntry(DifferenceEntry<T, TU> differenceEntry)
```