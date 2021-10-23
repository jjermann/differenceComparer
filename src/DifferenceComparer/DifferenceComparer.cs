using System;
using System.Collections.Generic;
using System.Linq;

namespace DifferenceComparer
{
    public class DifferenceComparer<T>
        where T: class
    {
        public IEqualityComparer<T> EntryIdEqualityComparer { get; }
        public IEqualityComparer<T> EntryEqualityComparer { get; }
        public IEqualityComparer<DifferenceEntry<T>> DifferenceEqualityComparer { get; }

        /// <param name="entryIdEqualityComparer">
        /// EqualityComparer for BusinessId.
        /// Must correspond to equivalence relation.
        /// Must be a subclass of the equality equivalence relation.
        /// Business condition: Must uniquely identify entries in a given data set.
        /// </param>
        /// <param name="equalityComparer">
        /// EqualityComparer for Entries.
        /// If null then the default EqualityComparer for T is used.
        /// Must correspond to equivalence relation.
        /// Business condition: Must distinguish data set entries that have "changed".
        /// </param>
        public DifferenceComparer(
            IEqualityComparer<T> entryIdEqualityComparer,
            IEqualityComparer<T>? equalityComparer = null)
        {
            EntryIdEqualityComparer = entryIdEqualityComparer;
            EntryEqualityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            DifferenceEqualityComparer = DifferenceEntry<T>.GetEqualityComparer(EntryEqualityComparer);
        }

        // Remark: It is possible to change this to an algorithm that first only works on Ids and handles whole Entries only as enumerables.
        // The challenge is to keep the method/signature simple.
        // Remark: Switched arguments give the inverse difference.
        public List<DifferenceEntry<T>> GetDifference(
            IList<T> list1,
            IList<T> list2)
        {
            var idDictionary1 = list1
                .ToDictionary(e => EntryIdEqualityComparer.GetHashCode(e), e => e);
            var idDictionary2 = list2
                .ToDictionary(e => EntryIdEqualityComparer.GetHashCode(e), e => e);
            var addIdHashSet = idDictionary2.Keys
                .Except(idDictionary1.Keys)
                .ToHashSet();
            var deleteIdHashSet = idDictionary1.Keys
                .Except(idDictionary2.Keys)
                .ToHashSet();
            var updateCandidateIdHashSet = idDictionary1.Keys
                .Intersect(idDictionary2.Keys)
                .ToHashSet();

            var differenceEntryList = new List<DifferenceEntry<T>>();
            var addList = addIdHashSet
                .Select(id => new DifferenceEntry<T>(
                    null,
                    idDictionary2[id],
                    EntryIdEqualityComparer))
                .ToList();
            differenceEntryList.AddRange(addList);
            var deleteList = deleteIdHashSet
                .Select(id => new DifferenceEntry<T>(
                    idDictionary1[id],
                    null,
                    EntryIdEqualityComparer))
                .ToList();
            differenceEntryList.AddRange(deleteList);
            var updateList = updateCandidateIdHashSet
                .Where(id => !EntryEqualityComparer.Equals(idDictionary1[id], idDictionary2[id]))
                .Select(id => new DifferenceEntry<T>(
                    idDictionary1[id],
                    idDictionary2[id],
                    EntryIdEqualityComparer))
                .ToList();
            differenceEntryList.AddRange(updateList);

            return differenceEntryList;
        }

        private void ValidateDifferenceList(IList<DifferenceEntry<T>> differenceList)
        {
            if (differenceList
                .GroupBy(d => d.Id)
                .Any(g => g.Count() > 1))
            {
                var msg = "List must be distinct (according to IdEqualityComparer)!";
                throw new ArgumentException(msg);
            }

            if (differenceList
                .Any(d => EntryIdEqualityComparer.GetHashCode(d.ExampleEntry) != d.Id))
            {
                var msg =
                    "The IdEqualityComparer of the DifferenceEntries must correspond to the one from the DifferenceComparer!";
                throw new ArgumentException(msg);
            }
        }

        // Remark: This method is symmetric.
        private void ValidateDifferenceProgression(
            IList<DifferenceEntry<T>> differenceList1,
            IList<DifferenceEntry<T>> differenceList2)
        {
            ValidateDifferenceList(differenceList1);
            ValidateDifferenceList(differenceList2);

            var differenceList1IdDictionary = differenceList1
                .ToDictionary(d => d.Id, d => d);
            var differenceList2IdDictionary = differenceList2
                .ToDictionary(d => d.Id, d => d);
            var differenceList1TypeIdDictionary = differenceList1
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceList2TypeIdDictionary = differenceList2
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceList1TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList1TypeIdDictionary[differenceType] = new HashSet<int>();
                }
                if (!differenceList2TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList2TypeIdDictionary[differenceType] = new HashSet<int>();
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

            if (deleteInBothIdHashSet.Any(id =>
                    !DifferenceEqualityComparer.Equals(differenceList1IdDictionary[id],
                        differenceList2IdDictionary[id])))
            {
                var msg = "Inconsistency: Can't have unequal Delete differences for the same Id!";
                throw new ArgumentException(msg);
            }

            if (updateInBothIdHashSet.Any(id =>
                    !DifferenceEqualityComparer.Equals(differenceList1IdDictionary[id],
                        differenceList2IdDictionary[id])))
            {
                var msg = "Inconsistency: Can't have unequal Update differences for the same Id!";
                throw new ArgumentException(msg);
            }
        }

        // Remark: This method is not symmetric.
        private void ValidateDifferenceSquash(
            IList<DifferenceEntry<T>> differenceListBefore,
            IList<DifferenceEntry<T>> differenceListAfter)
        {
            ValidateDifferenceList(differenceListBefore);
            ValidateDifferenceList(differenceListAfter);

            var differenceListBeforeIdDictionary = differenceListBefore
                .ToDictionary(d => d.Id, d => d);
            var differenceListAfterIdDictionary = differenceListAfter
                .ToDictionary(d => d.Id, d => d);
            var differenceListBeforeTypeIdDictionary = differenceListBefore
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceListAfterTypeIdDictionary = differenceListAfter
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceListBeforeTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListBeforeTypeIdDictionary[differenceType] = new HashSet<int>();
                }
                if (!differenceListAfterTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListAfterTypeIdDictionary[differenceType] = new HashSet<int>();
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

        /// <summary>
        /// Helper method to determine the update difference entry for a difference progression that corresponds to an Update.
        /// I.e. (Add, Add) or (Update, Update).
        /// Remark: Switched arguments give the inverse difference.
        /// </summary>
        /// <returns>
        /// The Update difference entry corresponding to the given difference progression unless it is a trivial Update in which case null is returned.
        /// </returns>
        /// <exception cref="ArgumentException"></exception>
        private DifferenceEntry<T>? GetUpdateDifferenceForProgression(
            DifferenceEntry<T> differenceProgressionEntry1,
            DifferenceEntry<T> differenceProgressionEntry2)
        {
            if (differenceProgressionEntry1.Id != differenceProgressionEntry2.Id)
            {
                throw new ArgumentException("The difference entries must have matching Id!");
            }

            var isAddAdd = differenceProgressionEntry1.DifferenceType == DifferenceType.Add &&
                           differenceProgressionEntry2.DifferenceType == DifferenceType.Add;
            var isUpdateUpdate = differenceProgressionEntry1.DifferenceType == DifferenceType.Update &&
                                 differenceProgressionEntry2.DifferenceType == DifferenceType.Update;

            if (!isAddAdd && !isUpdateUpdate)
            {
                throw new ArgumentException("The difference entry progression doesn't correspond to an Update!");
            }

            if (EntryEqualityComparer.Equals(differenceProgressionEntry1.EntryAfter, differenceProgressionEntry2.EntryAfter))
            {
                return null;
            }

            return new DifferenceEntry<T>(
                differenceProgressionEntry1.EntryAfter,
                differenceProgressionEntry2.EntryAfter,
                EntryIdEqualityComparer);
        }

        // Remark: This method is not really symmetric (resp. switched arguments don't make sense).
        private DifferenceEntry<T>? GetUpdateDifference(T entryBefore, T entryAfter)
        {
            if (EntryIdEqualityComparer.GetHashCode(entryBefore) != EntryIdEqualityComparer.GetHashCode(entryAfter))
            {
                throw new ArgumentException("The entries must have matching Id!");
            }

            if (EntryEqualityComparer.Equals(entryBefore, entryAfter))
            {
                return null;
            }

            return new DifferenceEntry<T>(entryBefore, entryAfter, EntryIdEqualityComparer);
        }

        // Remark: It is possible to change this to an algorithm that first only works on Ids and handles whole Entries only as enumerables.
        // The challenge is to keep the method/signature simple.
        // Remark: Switched arguments give the inverse difference.
        public List<DifferenceEntry<T>> GetDifferenceProgression(
            IList<DifferenceEntry<T>> differenceList1,
            IList<DifferenceEntry<T>> differenceList2)
        {
            // Remark: If this is unwanted/inefficient it could be skipped.
            ValidateDifferenceProgression(differenceList1, differenceList2);

            var differenceList1IdDictionary = differenceList1
                .ToDictionary(d => d.Id, d => d);
            var differenceList2IdDictionary = differenceList2
                .ToDictionary(d => d.Id, d => d);
            var differenceList1TypeIdDictionary = differenceList1
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceList2TypeIdDictionary = differenceList2
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceList1TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList1TypeIdDictionary[differenceType] = new HashSet<int>();
                }
                if (!differenceList2TypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceList2TypeIdDictionary[differenceType] = new HashSet<int>();
                }
            }

            var add1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Add];
            var delete1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Delete];
            var update1IdHashSet = differenceList1TypeIdDictionary[DifferenceType.Update];
            var add2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Add];
            var delete2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Delete];
            var update2IdHashSet = differenceList2TypeIdDictionary[DifferenceType.Update]; 

            var differenceEntryList = new List<DifferenceEntry<T>>();

            // Add
            var addList1 = add2IdHashSet.Except(add1IdHashSet)
                .Select(id => differenceList2IdDictionary[id].Clone())
                .ToList();
            var addList2 = delete1IdHashSet.Except(delete2IdHashSet).Except(update2IdHashSet)
                .Select(id => new DifferenceEntry<T>(
                    null,
                    differenceList1IdDictionary[id].EntryBefore,
                    EntryIdEqualityComparer))
                .ToList();
            var addList3 = (delete1IdHashSet.Except(delete2IdHashSet)).Intersect(update2IdHashSet)
                .Select(id => new DifferenceEntry<T>(
                    null,
                    differenceList2IdDictionary[id].EntryAfter,
                    EntryIdEqualityComparer))
                .ToList();
            differenceEntryList.AddRange(addList1);
            differenceEntryList.AddRange(addList2);
            differenceEntryList.AddRange(addList3);

            // Delete
            var delList1 = delete2IdHashSet.Except(delete1IdHashSet)
                .Select(id => differenceList2IdDictionary[id].Clone())
                .ToList();
            var delList2 = add1IdHashSet.Except(add2IdHashSet)
                .Select(id => new DifferenceEntry<T>(
                    differenceList1IdDictionary[id].EntryAfter,
                    null,
                    EntryIdEqualityComparer))
                .ToList();
            differenceEntryList.AddRange(delList1);
            differenceEntryList.AddRange(delList2);

            // Update
            var updateList1 = add2IdHashSet.Intersect(add1IdHashSet)
                .Select(id => GetUpdateDifferenceForProgression(differenceList1IdDictionary[id], differenceList2IdDictionary[id]))
                .Where(d => d != null)
                .Select(d => d!)
                .ToList(); 
            var updateList2 = update2IdHashSet.Except(update1IdHashSet.Union(delete1IdHashSet))
                .Select(id => differenceList2IdDictionary[id].Clone())
                .ToList();
            var updateList3 = update2IdHashSet.Union(update1IdHashSet).Except(delete1IdHashSet.Union(delete2IdHashSet))
                .Select(id => GetUpdateDifferenceForProgression(differenceList1IdDictionary[id], differenceList2IdDictionary[id]))
                .Where(d => d != null)
                .Select(d => d!)
                .ToList();
            differenceEntryList.AddRange(updateList1);
            differenceEntryList.AddRange(updateList2);
            differenceEntryList.AddRange(updateList3);

            return differenceEntryList;
        }

        // Remark: This method is not symmetric.
        private List<DifferenceEntry<T>> GetSquashedDifferencePair(
            IList<DifferenceEntry<T>> differenceListBefore,
            IList<DifferenceEntry<T>> differenceListAfter)
        {
            // Remark: If this is unwanted/inefficient it could be skipped.
            ValidateDifferenceSquash(differenceListBefore, differenceListAfter);

            var differenceListBeforeIdDictionary = differenceListBefore
                .ToDictionary(d => d.Id, d => d);
            var differenceListAfterIdDictionary = differenceListAfter
                .ToDictionary(d => d.Id, d => d);
            var differenceListBeforeTypeIdDictionary = differenceListBefore
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            var differenceListAfterTypeIdDictionary = differenceListAfter
                .GroupBy(d => d.DifferenceType)
                .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet());
            foreach (var differenceType in Enum.GetValues<DifferenceType>())
            {
                if (!differenceListBeforeTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListBeforeTypeIdDictionary[differenceType] = new HashSet<int>();
                }
                if (!differenceListAfterTypeIdDictionary.ContainsKey(differenceType))
                {
                    differenceListAfterTypeIdDictionary[differenceType] = new HashSet<int>();
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
                .Select(id => new DifferenceEntry<T>(
                    null,
                    differenceListAfterIdDictionary[id].EntryAfter,
                    EntryIdEqualityComparer))
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
                .Select(id => new DifferenceEntry<T>(
                    differenceListBeforeIdDictionary[id].EntryBefore,
                    null,
                    EntryIdEqualityComparer))
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

        public List<DifferenceEntry<T>> GetSquashedDifference(params IList<DifferenceEntry<T>>[] differenceListArray)
        {
            return differenceListArray
                .Aggregate(
                    new List<DifferenceEntry<T>>(),
                    GetSquashedDifferencePair)
                .ToList();
        }
    }
}

