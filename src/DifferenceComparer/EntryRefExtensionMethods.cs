using System;
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
            this DifferenceEntry<T> differenceEntry,
            Func<T, TU> entryIdSelector)
            where T: class
            where TU : notnull
        {
            return new EntryRefDifference<TU>(
                entryIdSelector(differenceEntry.ExampleEntry),
                differenceEntry.EntryBefore != null ? 0 : null,
                differenceEntry.EntryAfter != null ? 0 : null);
        }

        public static EntryRefDifference<TU> ToTrivialEntryRefDifference<T, TU>(
            this EquatableDifferenceEntry<T, TU> differenceEntry)
            where T : class
            where TU : notnull
        {
            return differenceEntry.ToTrivialEntryRefDifference(
                x => differenceEntry.EntryIdSelector(x));
        }
    }
}
