using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DifferenceComparer.Model;

namespace DifferenceComparer
{
    public class DifferenceComparer<T, TU>
        where T: class
        where TU: notnull
    {
        public Func<T, TU> EntryIdSelector { get; }
        public IEqualityComparer<T> EntryEqualityComparer { get; }
        public bool SkipValidation { get; set; }

        /// <param name="entryIdSelector">
        /// Selector for "Id".
        /// The default EqualityComparer will be used.
        /// Business condition: Must uniquely identify entries in a given data collection.
        /// </param>
        /// <param name="equalityComparer">
        /// EqualityComparer for Entries.
        /// If null then the default EqualityComparer for T is used.
        /// Must correspond to an equivalence relation.
        /// Business condition: Must distinguish data collection entries that have "changed".
        /// </param>
        /// <param name="skipValidation">
        /// If true then Validations are skipped (default false).
        /// </param>
        public DifferenceComparer(
            Func<T, TU> entryIdSelector,
            IEqualityComparer<T>? equalityComparer = null,
            bool skipValidation = false)
        {
            EntryIdSelector = entryIdSelector;
            EntryEqualityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            SkipValidation = skipValidation;
        }

        public string SerializeDifference(
            ICollection<DifferenceEntry<T>> differenceEntryCollection,
            JsonSerializerOptions? options = null)
        {
            options ??= new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(differenceEntryCollection, options);
        }

        public ICollection<DifferenceEntry<T>> DeserializeDifference(
            string json,
            JsonSerializerOptions? options = null)
        {
            var entryList = JsonSerializer.Deserialize<ICollection<DifferenceEntry<T>>>(json, options);

            if (entryList == null)
            {
                throw new InvalidOperationException("Unable to deserialize collection!");
            }

            return entryList;
        }


        public EquatableDifferenceEntry<T, TU> ToEquatableDifferenceEntry(DifferenceEntry<T> differenceEntry)
        {
            return EquatableDifferenceEntry<T, TU>.FromDifferenceEntry(
                differenceEntry,
                EntryIdSelector,
                EntryEqualityComparer);
        }

        // Remark: Switched arguments give the inverse difference.
        public List<EntryRefDifference<TU>> GetEntryRefDifference(
            in ICollection<TU> col1,
            in ICollection<TU> col2)
        {
            var addIdHashSet = col2
                .Except(col1)
                .ToHashSet();
            var deleteIdHashSet = col1
                .Except(col2)
                .ToHashSet();
            var updateCandidateIdHashSet = col1
                .Intersect(col2)
                .ToHashSet();

            var differenceEntryList = new List<EntryRefDifference<TU>>();
            var addList = addIdHashSet
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    null,
                    2))
                .ToList();
            differenceEntryList.AddRange(addList);
            var deleteList = deleteIdHashSet
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    1,
                    null))
                .ToList();
            differenceEntryList.AddRange(deleteList);
            var updateList = updateCandidateIdHashSet
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    1,
                    2))
                .ToList();
            differenceEntryList.AddRange(updateList);

            return differenceEntryList;
        }

        private DifferenceEntry<T>? GetDifferenceEntryFromEntryRefAndEntryPair(
            in DifferenceEntry<EntryRef<TU>> refDifference,
            in T? entry1,
            in T? entry2)
        {
            if (refDifference.DifferenceType == DifferenceType.Add)
            {
                var entryAfter = refDifference.EntryAfter!.Index == 1
                    ? entry1!
                    : entry2!;
                return new EquatableDifferenceEntry<T, TU>(
                    null,
                    entryAfter,
                    EntryIdSelector,
                    EntryEqualityComparer);
            }

            if (refDifference.DifferenceType == DifferenceType.Delete)
            {
                var entryBefore = refDifference.EntryBefore!.Index == 1
                    ? entry1!
                    : entry2!;
                return new EquatableDifferenceEntry<T, TU>(
                    entryBefore,
                    null,
                    EntryIdSelector,
                    EntryEqualityComparer);
            }

            if (refDifference.DifferenceType == DifferenceType.Update)
            {
                var entryBefore = refDifference.EntryBefore!.Index == 1
                    ? entry1!
                    : entry2!;
                var entryAfter = refDifference.EntryAfter!.Index == 1
                    ? entry1!
                    : entry2!;
                return GetUpdateDifference(entryBefore, entryAfter);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Memory efficient version to return the difference between two collections.
        /// As a prelimenary step entryRefDifferenceCollection must be determined from GetEntryRefDifference
        /// then the required data can be fetched (even as an enumerable) from data collection 1/2.
        /// Remark: Except for delete and add operations this unfortunately includes all data from both data sets.
        /// Remark: If the data arguments in the whole procedure are switched then the inverse difference is returned.
        /// </summary>
        /// <param name="entryRefDifferenceCollection">
        /// Must come from GetEntryRefDifference from the ids of differenceData1 and differenceData2 (in that order).
        /// </param>
        /// <param name="data1">
        /// Corresponding data from the first collection (even as an enumerable).
        /// Remark: Only matching data for the ids of entryRefDifferenceCollection are needed.
        /// Except for delete and add operations this unfortunately includes all data from both data sets.
        /// </param>
        /// <param name="data2">
        /// Corresponding data from the second collection (even as an enumerable).
        /// Remark: Only matching data for the ids of entryRefDifferenceCollection are needed.
        /// Except for delete and add operations this unfortunately includes all data from both data sets.
        /// </param>
        /// <returns>
        /// The difference between collection 1 and 2.
        /// </returns>
        public List<DifferenceEntry<T>> GetDifference(
            in ICollection<EntryRefDifference<TU>> entryRefDifferenceCollection,
            in IEnumerable<T> data1,
            in IEnumerable<T> data2)
        {
            // TODO: Use an enumerable mechanism from here on...
            var idDictionary1 = data1
                .ToDictionary(EntryIdSelector, e => e);
            var idDictionary2 = data2
                .ToDictionary(EntryIdSelector, e => e);

            var differenceEntryList = entryRefDifferenceCollection
                .Select(d => GetDifferenceEntryFromEntryRefAndEntryPair(
                    d,
                    idDictionary1.ContainsKey(d.Id) ? idDictionary1[d.Id] : null,
                    idDictionary2.ContainsKey(d.Id) ? idDictionary2[d.Id] : null))
                .Where(d => d != null)
                .Select(d => d!)
                .ToList();

            return differenceEntryList;
        }

        // Remark: Switched arguments give the inverse difference.
        // Remark: For a more efficient version, see GetDifference above.
        public List<DifferenceEntry<T>> GetDifference(
            in ICollection<T> col1,
            in ICollection<T> col2)
        {
            var idList1 = col1
                .Select(EntryIdSelector)
                .ToList();
            var idList2 = col2
                .Select(EntryIdSelector)
                .ToList();
            var entryRefDifferenceCollection = GetEntryRefDifference(idList1, idList2);

            return GetDifference(entryRefDifferenceCollection, col1, col2);
        }

        private void ValidateEntryRefDifferenceList(
            in ICollection<EntryRefDifference<TU>> entryRefDifferenceCollection)
        {
            if (entryRefDifferenceCollection
                .GroupBy(d => d.Id)
                .Any(g => g.Count() > 1))
            {
                var msg = "The id list must be distinct!";
                throw new ArgumentException(msg);
            }
        }

        private void ValidateDifferenceList(
            in ICollection<DifferenceEntry<T>> differenceCollection)
        {
            if (differenceCollection
                .Select(ToEquatableDifferenceEntry)
                .GroupBy(d => d.Id)
                .Any(g => g.Count() > 1))
            {
                var msg = "The id list must be distinct!";
                throw new ArgumentException(msg);
            }
        }

        // Remark: This method is symmetric.
        private void ValidateEntryRefDifferenceProgression(
            in ICollection<EntryRefDifference<TU>> entryRefDifferenceCollection1,
            in ICollection<EntryRefDifference<TU>> entryRefDifferenceCollection2)
        {
            ValidateEntryRefDifferenceList(entryRefDifferenceCollection1);
            ValidateEntryRefDifferenceList(entryRefDifferenceCollection2);

            var differenceList1IdDictionary = entryRefDifferenceCollection1
                .ToDictionary(d => d.Id, d => d);
            var differenceList2IdDictionary = entryRefDifferenceCollection2
                .ToDictionary(d => d.Id, d => d);
            var differenceList1TypeIdDictionary = entryRefDifferenceCollection1
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceList2TypeIdDictionary = entryRefDifferenceCollection2
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceList1TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList1TypeIdDictionary[differenceType] = new HashSet<TU>();
                }
                if (!differenceList2TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList2TypeIdDictionary[differenceType] = new HashSet<TU>();
                }
            }

            var add1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Add];
            var delete1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Delete];
            var update1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Update];
            var add2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Add];
            var delete2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Delete];
            var update2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Update];
            var deleteInBothIdHashSet = delete1IdHashSet
                .Intersect(delete2IdHashSet)
                .ToHashSet();
            var updateInBothIdHashSet = update1IdHashSet
                .Intersect(update2IdHashSet)
                .ToHashSet();

            if (add1IdHashSet.Intersect(delete2IdHashSet).Any()
                || add2IdHashSet.Intersect(delete1IdHashSet).Any())
            {
                var msg = "Inconsistency: Can't Add for one difference and Delete for the other difference!";
                throw new ArgumentException(msg);
            }

            if (add1IdHashSet.Intersect(update2IdHashSet).Any()
                || add2IdHashSet.Intersect(update1IdHashSet).Any())
            {
                var msg = "Inconsistency: Can't Add for one difference and Update for the other difference!";
                throw new ArgumentException(msg);
            }

            if (deleteInBothIdHashSet.Any(id => !differenceList1IdDictionary[id].Equals(differenceList2IdDictionary[id])))
            {
                var msg = "Inconsistency: Can't have unequal Delete differences for the same Id!";
                throw new ArgumentException(msg);
            }

            if (updateInBothIdHashSet.Any(id => !differenceList1IdDictionary[id].Equals(differenceList2IdDictionary[id])))
            {
                var msg = "Inconsistency: Can't have unequal Update differences for the same Id!";
                throw new ArgumentException(msg);
            }
        }

        // Remark: This method is symmetric.
        private void ValidateDifferenceProgression(
            in ICollection<DifferenceEntry<T>> differenceCollection1,
            in ICollection<DifferenceEntry<T>> differenceCollection2)
        {
            ValidateDifferenceList(differenceCollection1);
            ValidateDifferenceList(differenceCollection2);

            var differenceList1IdDictionary = differenceCollection1
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);
            var differenceList2IdDictionary = differenceCollection2
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);
            var differenceList1TypeIdDictionary = differenceCollection1
                .Select(ToEquatableDifferenceEntry)
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceList2TypeIdDictionary = differenceCollection2
                .Select(ToEquatableDifferenceEntry)
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceList1TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList1TypeIdDictionary[differenceType] = new HashSet<TU>();
                }
                if (!differenceList2TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList2TypeIdDictionary[differenceType] = new HashSet<TU>();
                }
            }

            var add1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Add];
            var delete1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Delete];
            var update1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Update];
            var add2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Add];
            var delete2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Delete];
            var update2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Update];
            var deleteInBothIdHashSet = delete1IdHashSet
                .Intersect(delete2IdHashSet)
                .ToHashSet();
            var updateInBothIdHashSet = update1IdHashSet
                .Intersect(update2IdHashSet)
                .ToHashSet();

            if (add1IdHashSet.Intersect(delete2IdHashSet).Any()
                || add2IdHashSet.Intersect(delete1IdHashSet).Any())
            {
                var msg = "Inconsistency: Can't Add for one difference and Delete for the other difference!";
                throw new ArgumentException(msg);
            }

            if (add1IdHashSet.Intersect(update2IdHashSet).Any()
                || add2IdHashSet.Intersect(update1IdHashSet).Any())
            {
                var msg = "Inconsistency: Can't Add for one difference and Update for the other difference!";
                throw new ArgumentException(msg);
            }

            if (deleteInBothIdHashSet.Any(id => !differenceList1IdDictionary[id].Equals(differenceList2IdDictionary[id])))
            {
                var msg = "Inconsistency: Can't have unequal Delete differences for the same Id!";
                throw new ArgumentException(msg);
            }

            if (updateInBothIdHashSet.Any(id => !differenceList1IdDictionary[id].Equals(differenceList2IdDictionary[id])))
            {
                var msg = "Inconsistency: Can't have unequal Update differences for the same Id!";
                throw new ArgumentException(msg);
            }
        }

        // Remark: This method is not symmetric.
        private void ValidateDifferenceSquash(
            in ICollection<DifferenceEntry<T>> differenceCollectionBefore,
            in ICollection<DifferenceEntry<T>> differenceCollectionAfter)
        {
            ValidateDifferenceList(differenceCollectionBefore);
            ValidateDifferenceList(differenceCollectionAfter);

            var differenceListBeforeIdDictionary = differenceCollectionBefore
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);
            var differenceListAfterIdDictionary = differenceCollectionAfter
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);
            var differenceListBeforeTypeIdDictionary = differenceCollectionBefore
                .Select(ToEquatableDifferenceEntry)
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceListAfterTypeIdDictionary = differenceCollectionAfter
                .Select(ToEquatableDifferenceEntry)
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceListBeforeTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListBeforeTypeIdDictionary[differenceType] = new HashSet<TU>();
                }
                if (!differenceListAfterTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListAfterTypeIdDictionary[differenceType] = new HashSet<TU>();
                }
            }

            var addBeforeIdHashSet = differenceListBeforeTypeIdDictionary[DifferenceType.Add];
            var deleteBeforeIdHashSet = differenceListBeforeTypeIdDictionary[DifferenceType.Delete];
            var updateBeforeIdHashSet = differenceListBeforeTypeIdDictionary[DifferenceType.Update];
            var addAfterIdHashSet = differenceListAfterTypeIdDictionary[DifferenceType.Add];
            var deleteAfterIdHashSet = differenceListAfterTypeIdDictionary[DifferenceType.Delete];
            var updateAfterIdHashSet = differenceListAfterTypeIdDictionary[DifferenceType.Update];

            if (addBeforeIdHashSet.Intersect(addAfterIdHashSet).Any())
            {
                var msg = "Inconsistency: Can't have Add before and after!";
                throw new ArgumentException(msg);
            }

            if (deleteBeforeIdHashSet.Intersect(deleteAfterIdHashSet).Any())
            {
                var msg = "Inconsistency: Can't have Delete before and Delete after!";
                throw new ArgumentException(msg);
            }

            if (deleteBeforeIdHashSet.Intersect(updateAfterIdHashSet).Any())
            {
                var msg = "Inconsistency: Can't have Delete before and Update after!";
                throw new ArgumentException(msg);
            }

            if (updateBeforeIdHashSet.Intersect(addAfterIdHashSet).Any())
            {
                var msg = "Inconsistency: Can't have Update before and Add after!";
                throw new ArgumentException(msg);
            }

            if (addBeforeIdHashSet.Except(deleteAfterIdHashSet).Intersect(updateAfterIdHashSet).Any(id =>
                    !EntryEqualityComparer.Equals(differenceListBeforeIdDictionary[id].EntryAfter!, differenceListAfterIdDictionary[id].EntryBefore!)))
            {
                var msg = "Inconsistency: Add -> Update must have matching connecting entries!";
                throw new ArgumentException(msg);
            }

            if (deleteAfterIdHashSet.Except(addBeforeIdHashSet).Intersect(updateBeforeIdHashSet).Any(id =>
                    !EntryEqualityComparer.Equals(differenceListBeforeIdDictionary[id].EntryAfter!, differenceListAfterIdDictionary[id].EntryBefore!)))
            {
                var msg = "Inconsistency: Update -> Delete must have matching connecting entries!";
                throw new ArgumentException(msg);
            }

            if (updateBeforeIdHashSet.Except(deleteAfterIdHashSet).Intersect(updateAfterIdHashSet).Any(id =>
                    !EntryEqualityComparer.Equals(differenceListBeforeIdDictionary[id].EntryAfter!, differenceListAfterIdDictionary[id].EntryBefore!)))
            {
                var msg = "Inconsistency: Update -> Update must have matching connecting entries!";
                throw new ArgumentException(msg);
            }
        }

        // Remark: This method is not really symmetric (resp. switched arguments don't make sense).
        private DifferenceEntry<T>? GetUpdateDifference(in T entryBefore, in T entryAfter)
        {
            if (!EntryIdSelector(entryBefore).Equals(EntryIdSelector(entryAfter)))
            {
                throw new ArgumentException("The entries must have matching Id!");
            }

            if (EntryEqualityComparer.Equals(entryBefore, entryAfter))
            {
                return null;
            }

            return new EquatableDifferenceEntry<T, TU>(
                entryBefore,
                entryAfter,
                EntryIdSelector,
                EntryEqualityComparer);
        }

        /// <summary>
        /// Returns the EntryRef difference progression for the two given EntryRef differences.
        /// Remark: Switched arguments give the inverse difference.
        /// </summary>
        /// <param name="entryRefDifferenceList1">
        /// The EntryRef difference (the index is irrelevant).</param>
        /// <param name="entryRefDifferenceList2">
        /// The EntryRef difference (the index is irrelevant).
        /// </param>
        /// <returns>
        /// The EntryRef difference progression with a specific (technical) index.
        /// </returns>
        public List<EntryRefDifference<TU>> GetEntryRefDifferenceProgression(
            in ICollection<EntryRefDifference<TU>> entryRefDifferenceList1,
            in ICollection<EntryRefDifference<TU>> entryRefDifferenceList2)
        {
            if (!SkipValidation)
            {
                ValidateEntryRefDifferenceProgression(entryRefDifferenceList1, entryRefDifferenceList2);
            }

            var differenceList1TypeIdDictionary = entryRefDifferenceList1
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceList2TypeIdDictionary = entryRefDifferenceList2
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceList1TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList1TypeIdDictionary[differenceType] = new HashSet<TU>();
                }
                if (!differenceList2TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList2TypeIdDictionary[differenceType] = new HashSet<TU>();
                }
            }

            var add1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Add];
            var delete1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Delete];
            var update1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Update];
            var add2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Add];
            var delete2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Delete];
            var update2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Update];

            var differenceEntryList = new List<EntryRefDifference<TU>>();

            // Add
            var addList1 = add2IdHashSet.Except(add1IdHashSet)
                .Select(id => new EntryRefDifference<TU>(
                        id,
                        null,
                        (int)EntryRefDifferenceIndex.EntryAfterFromSecond))
                .ToList();
            var addList2 = delete1IdHashSet.Except(delete2IdHashSet).Except(update2IdHashSet)
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    null, 
                    (int)EntryRefDifferenceIndex.EntryBeforeFromFirst))
                .ToList();
            var addList3 = (delete1IdHashSet.Except(delete2IdHashSet)).Intersect(update2IdHashSet)
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    null,
                    (int)EntryRefDifferenceIndex.EntryAfterFromSecond))
                .ToList();
            differenceEntryList.AddRange(addList1);
            differenceEntryList.AddRange(addList2);
            differenceEntryList.AddRange(addList3);

            // Delete
            var delList1 = delete2IdHashSet.Except(delete1IdHashSet)
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    (int)EntryRefDifferenceIndex.EntryBeforeFromSecond,
                    null))
                .ToList();
            var delList2 = add1IdHashSet.Except(add2IdHashSet)
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    (int)EntryRefDifferenceIndex.EntryAfterFromFirst,
                    null))
                .ToList();
            differenceEntryList.AddRange(delList1);
            differenceEntryList.AddRange(delList2);

            // Update
            var updateList1 = add2IdHashSet.Intersect(add1IdHashSet)
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    (int)EntryRefDifferenceIndex.EntryAfterFromFirst,
                    (int)EntryRefDifferenceIndex.EntryAfterFromSecond))
                .ToList();
            var updateList2 = update2IdHashSet.Except(update1IdHashSet.Union(delete1IdHashSet))
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    (int)EntryRefDifferenceIndex.EntryBeforeFromSecond,
                    (int)EntryRefDifferenceIndex.EntryAfterFromSecond))
                .ToList();
            var updateList3 = update2IdHashSet.Union(update1IdHashSet).Except(delete1IdHashSet.Union(delete2IdHashSet))
                .Select(id => new EntryRefDifference<TU>(
                    id,
                    (int)EntryRefDifferenceIndex.EntryAfterFromFirst,
                    (int)EntryRefDifferenceIndex.EntryAfterFromSecond))
                .ToList();
            differenceEntryList.AddRange(updateList1);
            differenceEntryList.AddRange(updateList2);
            differenceEntryList.AddRange(updateList3);

            return differenceEntryList;
        }

        private DifferenceEntry<T>? GetDifferenceEntryFromEntryRefAndDifferenceEntryPair(
            in DifferenceEntry<EntryRef<TU>> refDifference,
            in DifferenceEntry<T>? entry1,
            in DifferenceEntry<T>? entry2)
        {
            T GetEntryFromIndex(
                EntryRefDifferenceIndex differenceIndex,
                in DifferenceEntry<T>? e1,
                in DifferenceEntry<T>? e2)
            {
                return differenceIndex switch
                {
                    EntryRefDifferenceIndex.EntryBeforeFromFirst => e1!.EntryBefore!,
                    EntryRefDifferenceIndex.EntryAfterFromFirst => e1!.EntryAfter!,
                    EntryRefDifferenceIndex.EntryBeforeFromSecond => e2!.EntryBefore!,
                    EntryRefDifferenceIndex.EntryAfterFromSecond => e2!.EntryAfter!,
                    _ => throw new NotImplementedException()
                };
            }

            if (refDifference.DifferenceType == DifferenceType.Add)
            {
                var entryAfter = GetEntryFromIndex(
                    (EntryRefDifferenceIndex)refDifference.EntryAfter!.Index,
                    entry1,
                    entry2);

                return new EquatableDifferenceEntry<T, TU>(
                    null,
                    entryAfter,
                    EntryIdSelector,
                    EntryEqualityComparer);
            }

            if (refDifference.DifferenceType == DifferenceType.Delete)
            {
                var entryBefore = GetEntryFromIndex(
                    (EntryRefDifferenceIndex)refDifference.EntryBefore!.Index,
                    entry1,
                    entry2);

                return new EquatableDifferenceEntry<T, TU>(
                    entryBefore,
                    null,
                    EntryIdSelector,
                    EntryEqualityComparer);
            }

            if (refDifference.DifferenceType == DifferenceType.Update)
            {
                var entryBefore = GetEntryFromIndex(
                    (EntryRefDifferenceIndex)refDifference.EntryBefore!.Index,
                    entry1,
                    entry2);
                var entryAfter = GetEntryFromIndex(
                    (EntryRefDifferenceIndex)refDifference.EntryAfter!.Index,
                    entry1,
                    entry2);

                return GetUpdateDifference(entryBefore, entryAfter);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Memory efficient version to return the difference progression between two differences.
        /// As a prelimenary step entryRefDifferenceCollection must be determined from GetEntryRefDifferenceProgression
        /// then the required difference data can be supplied (even as an enumerable).
        /// Remark: If the data arguments in the whole procedure are switched then the inverse difference is returned.
        /// </summary>
        /// <param name="entryRefDifferenceCollection">
        /// Must come from GetEntryRefDifferenceProgression from the ids of differenceData1 and differenceData2 (in that order).
        /// </param>
        /// <param name="differenceData1">
        /// Corresponding difference data from the first difference (even as an enumerable).
        /// Remark: Only matching data for the ids of entryRefDifferenceCollection are needed.
        /// </param>
        /// <param name="differenceData2">
        /// Corresponding difference data from the second difference (even as an enumerable).
        /// Remark: Only matching data for the ids of entryRefDifferenceCollection are needed.
        /// </param>
        /// <returns>
        /// The difference progression from difference 1 to difference 2.
        /// </returns>
        public List<DifferenceEntry<T>> GetDifferenceProgression(
            in ICollection<EntryRefDifference<TU>> entryRefDifferenceCollection,
            in IEnumerable<DifferenceEntry<T>> differenceData1,
            in IEnumerable<DifferenceEntry<T>> differenceData2)
        {
            // TODO: Use an enumerable mechanism from here on...
            var idDictionary1 = differenceData1
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);
            var idDictionary2 = differenceData2
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);

            var differenceEntryList = entryRefDifferenceCollection
                .Select(d => GetDifferenceEntryFromEntryRefAndDifferenceEntryPair(
                    d,
                    idDictionary1.ContainsKey(d.Id) ? idDictionary1[d.Id] : null,
                    idDictionary2.ContainsKey(d.Id) ? idDictionary2[d.Id] : null))
                .Where(d => d != null)
                .Select(d => d!)
                .ToList();

            return differenceEntryList;
        }

        // Remark: Switched arguments give the inverse difference.
        // Remark: For a more efficient version, see GetDifferenceProgression above.
        public List<DifferenceEntry<T>> GetDifferenceProgression(
            in ICollection<DifferenceEntry<T>> differenceList1,
            in ICollection<DifferenceEntry<T>> differenceList2)
        {
            if (!SkipValidation)
            {
                ValidateDifferenceProgression(differenceList1, differenceList2);
            }

            var entryRefDifferenceList1 = differenceList1
                .Select(ToEquatableDifferenceEntry)
                .Select(d => d.GetTrivialEntryRefDifference())
                .ToList();
            var entryRefDifferenceList2 = differenceList2
                .Select(ToEquatableDifferenceEntry)
                .Select(d => d.GetTrivialEntryRefDifference())
                .ToList();
            var entryRefDifferenceCollection = GetEntryRefDifferenceProgression(
                entryRefDifferenceList1,
                entryRefDifferenceList2);

            return GetDifferenceProgression(
                entryRefDifferenceCollection,
                differenceList1,
                differenceList2);
        }

        // Remark: This method is not symmetric.
        // Remark: This could also be performance improved if really needed.
        private List<DifferenceEntry<T>> GetSquashedDifferencePair(
            in ICollection<DifferenceEntry<T>> differenceCollectionBefore,
            in ICollection<DifferenceEntry<T>> differenceCollectionAfter)
        {
            if (!SkipValidation)
            {
                ValidateDifferenceSquash(differenceCollectionBefore, differenceCollectionAfter);
            }

            var differenceListBeforeIdDictionary = differenceCollectionBefore
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);
            var differenceListAfterIdDictionary = differenceCollectionAfter
                .Select(ToEquatableDifferenceEntry)
                .ToDictionary(d => d.Id, d => d);
            var differenceListBeforeTypeIdDictionary = differenceCollectionBefore
                .Select(ToEquatableDifferenceEntry)
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceListAfterTypeIdDictionary = differenceCollectionAfter
                .Select(ToEquatableDifferenceEntry)
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceListBeforeTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListBeforeTypeIdDictionary[differenceType] = new HashSet<TU>();
                }
                if (!differenceListAfterTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListAfterTypeIdDictionary[differenceType] = new HashSet<TU>();
                }
            }

            var addBeforeIdHashSet = differenceListBeforeTypeIdDictionary[DifferenceType.Add];
            var deleteBeforeIdHashSet = differenceListBeforeTypeIdDictionary[DifferenceType.Delete];
            var updateBeforeIdHashSet = differenceListBeforeTypeIdDictionary[DifferenceType.Update];
            var addAfterIdHashSet = differenceListAfterTypeIdDictionary[DifferenceType.Add];
            var deleteAfterIdHashSet = differenceListAfterTypeIdDictionary[DifferenceType.Delete];
            var updateAfterIdHashSet = differenceListAfterTypeIdDictionary[DifferenceType.Update];

            var differenceEntryList = new List<DifferenceEntry<T>>();

            // Add
            var addList11 = addBeforeIdHashSet.Except(deleteAfterIdHashSet).Except(updateAfterIdHashSet)
                .Select(id => differenceListBeforeIdDictionary[id].Clone())
                .ToList();
            var addList12 = addBeforeIdHashSet.Except(deleteAfterIdHashSet).Intersect(updateAfterIdHashSet)
                .Select(id => new EquatableDifferenceEntry<T, TU>(
                    null,
                    differenceListAfterIdDictionary[id].EntryAfter,
                    EntryIdSelector,
                    EntryEqualityComparer))
                .ToList();
            var addList2 = addAfterIdHashSet.Except(deleteBeforeIdHashSet)
                .Select(id => differenceListAfterIdDictionary[id].Clone())
                .ToList();
            differenceEntryList.AddRange(addList11);
            differenceEntryList.AddRange(addList12);
            differenceEntryList.AddRange(addList2);

            // Delete
            var delList1 = deleteBeforeIdHashSet.Except(addAfterIdHashSet)
                .Select(id => differenceListBeforeIdDictionary[id].Clone())
                .ToList();
            var delList21 = deleteAfterIdHashSet.Except(addBeforeIdHashSet).Except(updateBeforeIdHashSet)
                .Select(id => differenceListAfterIdDictionary[id].Clone())
                .ToList();
            var delList22 = deleteAfterIdHashSet.Except(addBeforeIdHashSet).Intersect(updateBeforeIdHashSet)
                .Select(id => new EquatableDifferenceEntry<T, TU>(
                    differenceListBeforeIdDictionary[id].EntryBefore,
                    null,
                    EntryIdSelector,
                    EntryEqualityComparer))
                .ToList();
            differenceEntryList.AddRange(delList1);
            differenceEntryList.AddRange(delList21);
            differenceEntryList.AddRange(delList22);

            // Update
            var updateList11 = updateBeforeIdHashSet.Except(deleteAfterIdHashSet).Except(updateAfterIdHashSet)
                .Select(id => differenceListBeforeIdDictionary[id].Clone())
                .ToList();
            var updateList12 = updateBeforeIdHashSet.Except(deleteAfterIdHashSet).Intersect(updateAfterIdHashSet)
                .Select(id => GetUpdateDifference(differenceListBeforeIdDictionary[id].EntryBefore!, differenceListAfterIdDictionary[id].EntryAfter!))
                .Where(d => d != null)
                .Select(d => d!)
                .ToList();
            var updateList2 = addAfterIdHashSet.Intersect(deleteBeforeIdHashSet)
                .Select(id => GetUpdateDifference(differenceListBeforeIdDictionary[id].EntryBefore!, differenceListAfterIdDictionary[id].EntryAfter!))
                .Where(d => d != null)
                .Select(d => d!)
                .ToList();
            var updateList3 = updateAfterIdHashSet.Except(addBeforeIdHashSet).Except(updateBeforeIdHashSet)
                .Select(id => differenceListAfterIdDictionary[id].Clone())
                .ToList();
            differenceEntryList.AddRange(updateList11);
            differenceEntryList.AddRange(updateList12);
            differenceEntryList.AddRange(updateList2);
            differenceEntryList.AddRange(updateList3);

            return differenceEntryList;
        }

        // Remark: This could also be performance improved if really needed.
        public List<DifferenceEntry<T>> GetSquashedDifference(params ICollection<DifferenceEntry<T>>[] differenceCollectionArray)
        {
            return differenceCollectionArray
                .Aggregate(
                    new List<DifferenceEntry<T>>(),
                    (current, next) => 
                        GetSquashedDifferencePair(current, next))
                .ToList();
        }
    }
}

