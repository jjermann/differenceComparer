using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DifferenceComparer;
using DifferenceComparer.Model;
using Microsoft.EntityFrameworkCore;
using TestApp.TestData;

namespace TestApp.EF
{
    public class EfSimpleDifferenceInMemoryContext
        : EfInMemoryContext<DifferenceEntry<SimpleTestEntry, string>, string>,
            IDifferenceDbMock<SimpleTestEntry, string>
    {
        public DbSet<SimpleTestEntry> Foo { get; set; } = null!;

        public EfSimpleDifferenceInMemoryContext(string? dbName = null)
            : base(x => x.ExampleEntry.Id, dbName)
        { }

        public static EfSimpleDifferenceInMemoryContext InitializeFromCollection(
            ICollection<DifferenceEntry<SimpleTestEntry, string>> entryCollection,
            string? dbName = null)
        {
            var dbMock = new EfSimpleDifferenceInMemoryContext(dbName);
            dbMock.Add(entryCollection.ToArray());

            return dbMock;
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DifferenceEntry<SimpleTestEntry, string>>(b =>
            {
                b.OwnsOne(e => e.EntryBefore).WithOwner();
                b.OwnsOne(e => e.EntryAfter).WithOwner();
            });
        }

        public static EfSimpleDifferenceInMemoryContext InitializeFromJson(
            string json,
            JsonSerializerOptions? options = null,
            string? dbName = null)
        {
            var dbMock = new EfSimpleDifferenceInMemoryContext(dbName);
            dbMock.AddFromJson(json, options);

            return dbMock;
        }

        public ICollection<EntryRefDifference<string>> GetAllEntryRefDifferences()
        {
            return EntrySet
                .CustomChunk(ChunkSize)
                .SelectMany(g => g
                    .Select(d => d.ToTrivialEntryRefDifference()))
                .ToList();
        }
    }
}
