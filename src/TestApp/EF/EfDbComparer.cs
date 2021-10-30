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

        public List<DifferenceEntry<T, TU>> GetDbDifference(DbSet<T> state1, DbSet<T> state2)
        {
            var entryRefDifference = GetEntryRefDifference(
                state1
                    .OrderBy(EntryIdSelector)
                    .Select(EntryIdSelector)
                    .ToList(),
                state2
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

        public List<DifferenceEntry<T, TU>> GetDbDifferenceProgression(
            DbSet<DifferenceEntry<T, TU>> state1,
            DbSet<DifferenceEntry<T, TU>> state2,
            int chunkSize = 1000)
        {
            var entryRefDifferenceList = GetEntryRefDifferenceProgression(
                state1
                    .CustomChunk(chunkSize)
                    .SelectMany(g => g
                        .Select(d => d.ToTrivialEntryRefDifference()))
                    .OrderBy(d => d.Id)
                    .ToList(),
                state2
                    .CustomChunk(chunkSize)
                    .SelectMany(g => g
                        .Select(d => d.ToTrivialEntryRefDifference()))
                    .OrderBy(d => d.Id)
                    .ToList()
            );
            var differenceProgression = GetDifferenceProgression(
                entryRefDifferenceList,
                state1,
                state2);

            return differenceProgression;
        }

        public List<DifferenceEntry<T, TU>> GetDbSquashedDifference(params DbSet<DifferenceEntry<T, TU>>[] stateArray)
        {
            var stateList = stateArray
                .Select(dbSet => (ICollection<DifferenceEntry<T, TU>>)dbSet.ToList())
                .ToArray();
            var squashedDifference = GetSquashedDifference(stateList);

            return squashedDifference;
        }
    }
}
