using System;
using System.Collections.Generic;
using System.Linq;
using DifferenceComparer;
using DifferenceComparer.Model;
using Microsoft.EntityFrameworkCore;

namespace TestApp.EF
{
    public class EfDbComparer<T, TU>: DifferenceComparer<T, TU>
        where T: class
        where TU: notnull
    {
        public EfDbComparer(
            Func<T, TU> idSelector,
            IEqualityComparer<T>? entryEqualityComparer = null)
            : base(idSelector, entryEqualityComparer)
        { }

        public List<DifferenceEntry<T>> GetDbDifference(DbSet<T> state1, DbSet<T> state2)
        {
            var entryRefDifference = GetEntryRefDifference(
                state1
                    .OrderBy(EntryIdSelector)
                    .Select(EntryIdSelector)
                    .ToList(),
                state1
                    .OrderBy(EntryIdSelector)
                    .Select(EntryIdSelector)
                    .ToList());
            var difference = GetDifference(
                entryRefDifference,
                state1
                    .OrderBy(EntryIdSelector),
                state2
                    .OrderBy(EntryIdSelector));

            return difference;
        }

        public List<DifferenceEntry<T>> GetDbDifferenceProgression(DbSet<DifferenceEntry<T>> state1, DbSet<DifferenceEntry<T>> state2)
        {
            // TODO: Maybe we should introduce a data class for difference entries with an Id??
            var entryRefDifferenceList = GetEntryRefDifferenceProgression(
                state1
                    .Select(d => d.ToTrivialEntryRefDifference(EntryIdSelector))
                    .OrderBy(d => d.Id)
                    .ToList(),
                state2
                    .Select(d => d.ToTrivialEntryRefDifference(EntryIdSelector))
                    .OrderBy(d => d.Id)
                    .ToList()
            );
            var differenceProgression = GetDifferenceProgression(
                entryRefDifferenceList,
                state1,
                state2);

            return differenceProgression;
        }

        public List<DifferenceEntry<T>> GetDbSquashedDifference(params DbSet<DifferenceEntry<T>>[] stateArray)
        {
            var stateList = stateArray
                .Select(dbSet => (ICollection<DifferenceEntry<T>>)dbSet.ToList())
                .ToArray();
            var squashedDifference = GetSquashedDifference(stateList);

            return squashedDifference;
        }
    }
}
