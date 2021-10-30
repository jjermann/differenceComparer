using DifferenceComparer.Model;

namespace DifferenceComparer
{
    public static class EntryRefExtensionMethods
    {
        public static EntryRef<TU> ToEntryRef<TU>(this TU id, int index = 0)
        {
            return new EntryRef<TU>(id, index);
        }

        public static EntryRefDifference<TU> ToTrivialEntryRefDifference<T, TU>(
            this DifferenceEntry<T, TU> differenceEntry)
            where T: class
            where TU : notnull
        {
            return new EntryRefDifference<TU>(
                differenceEntry.Id,
                differenceEntry.EntryBefore != null ? 0 : null,
                differenceEntry.EntryAfter != null ? 0 : null);
        }

        internal static bool IsInFirstDifference<TU>(this EntryRefDifference<TU> entryRefDifference)
            where TU: notnull
        {
            return entryRefDifference.EntryBefore?.Index == 1 
                   || entryRefDifference.EntryAfter?.Index == 1;
        }

        internal static bool IsInSecondDifference<TU>(this EntryRefDifference<TU> entryRefDifference)
            where TU : notnull
        {
            return entryRefDifference.EntryBefore?.Index == 2
                   || entryRefDifference.EntryAfter?.Index == 2;
        }

        internal static bool IsInFirstDifferenceProgression<TU>(this EntryRefDifference<TU> entryRefDifference)
            where TU : notnull
        {
            return entryRefDifference.EntryBefore?.Index == (int)EntryRefDifferenceIndex.EntryAfterFromFirst
                   || entryRefDifference.EntryBefore?.Index == (int)EntryRefDifferenceIndex.EntryBeforeFromFirst
                   || entryRefDifference.EntryAfter?.Index == (int)EntryRefDifferenceIndex.EntryAfterFromFirst
                   || entryRefDifference.EntryAfter?.Index == (int)EntryRefDifferenceIndex.EntryBeforeFromFirst;
        }

        internal static bool IsInSecondDifferenceProgression<TU>(this EntryRefDifference<TU> entryRefDifference)
            where TU : notnull
        {
            return entryRefDifference.EntryBefore?.Index == (int)EntryRefDifferenceIndex.EntryAfterFromSecond
                   || entryRefDifference.EntryBefore?.Index == (int)EntryRefDifferenceIndex.EntryBeforeFromSecond
                   || entryRefDifference.EntryAfter?.Index == (int)EntryRefDifferenceIndex.EntryAfterFromSecond
                   || entryRefDifference.EntryAfter?.Index == (int)EntryRefDifferenceIndex.EntryBeforeFromSecond;
        }
    }
}
