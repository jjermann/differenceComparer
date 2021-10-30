using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using DifferenceComparer;
using DifferenceComparer.Model;
using Microsoft.EntityFrameworkCore;

namespace TestApp.EF
{
    /// <summary>
    /// EF DbContext with an InMemoryDatabase.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public class EfInMemoryContext<T, TU> : DbContext, IDbMock<T, TU>
        where T : class
        where TU : notnull
    {
        protected const int ChunkSize = 1000;
        private static readonly DbContextOptionsBuilder<EfInMemoryContext<T, TU>> OptionsBuilder = new();

        public DbSet<T> EntrySet { get; set; } = null!;

        protected readonly Func<T, TU> EntryIdSelector;

        public EfInMemoryContext(Func<T, TU> entryIdSelector, string? dbName = null)
            : base(OptionsBuilder
                .UseInMemoryDatabase(dbName ?? $"EfInMemoryContext{Guid.NewGuid()}")
                .Options)
        {
            EntryIdSelector = entryIdSelector;
        }

        // ReSharper disable once UnusedMember.Global
        public static EfInMemoryContext<T, TU> InitializeFromJson(
            Func<T, TU> entryIdSelector,
            string json,
            JsonSerializerOptions? options = null,
            string? dbName = null)
        {
            var dbMock = new EfInMemoryContext<T, TU>(entryIdSelector, dbName);
            dbMock.AddFromJson(json, options);

            return dbMock;
        }

        public void AddFromJson(string json, JsonSerializerOptions? options = null)
        {
            var entryList = JsonSerializer.Deserialize<ICollection<T>>(json, options);

            if (entryList == null)
            {
                throw new InvalidOperationException("Unable to deserialize collection!");
            }

            Add(entryList.ToArray());
        }

        public string SerializeToJson(JsonSerializerOptions? options = null)
        {
            options ??= new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(EntrySet.ToList(), options);
        }

        public IEnumerable<T> GetAllAsOrderedEnumerable()
        {
            return EntrySet;
        }

        public IEnumerable<T> GetAllEnumerableByIdList(IList<TU> idList)
        {
            return idList.Chunk(ChunkSize)
                .SelectMany(subList => EntrySet.Where(e => subList.Contains(EntryIdSelector(e))));
        }

        public IList<T> GetAll()
        {
            return EntrySet.ToList();
        }

        public IList<T> GetAllByIdList(IList<TU> idList)
        {
            return GetAllEnumerableByIdList(idList).ToList();
        }

        public HashSet<TU> GetAllIds()
        {
            return EntrySet
                .Select(EntryIdSelector)
                .ToHashSet();
        }

        public T Get(TU id)
        {
            return EntrySet.Single(e => EntryIdSelector(e).Equals(id));
        }

        public void Add(params T[] entryArray)
        {
            EntrySet.AddRange(entryArray);
            SaveChanges();
        }

        public void Update(params T[] entryArray)
        {
            EntrySet.UpdateRange(entryArray);
            SaveChanges();
        }

        public void Delete(params TU[] idArray)
        {
            var entryList = GetAllByIdList(idArray);
            EntrySet.RemoveRange(entryList);
            SaveChanges();
        }

        public int Reset()
        {
            var count = EntrySet.Count();
            RemoveRange(EntrySet);
            SaveChanges();

            return count;
        }

        public void ApplyDifference(params EquatableDifferenceEntry<T, TU>[] differenceArray)
        {
            foreach (var differenceEntry in differenceArray)
            {
                var id = differenceEntry.Id;

                if (differenceEntry.DifferenceType == DifferenceType.Add)
                {
                    Add(differenceEntry.EntryAfter!);
                }
                else if (differenceEntry.DifferenceType == DifferenceType.Delete)
                {
                    Delete(id);
                }
                else
                {
                    Update(differenceEntry.EntryAfter!);
                }
            }
        }
    }
}
