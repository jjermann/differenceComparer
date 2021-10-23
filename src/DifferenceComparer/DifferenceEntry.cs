using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DifferenceComparer
{
    public class DifferenceEntry<T>
        where T : class
    {
        private readonly IEqualityComparer<T> _entryIdEqualityComparer;

        public T? EntryBefore { get; }
        public T? EntryAfter { get; }

        public DifferenceEntry(
            T? entryBefore,
            T? entryAfter,
            IEqualityComparer<T> entryIdEqualityComparer)
        {
            _entryIdEqualityComparer = entryIdEqualityComparer;
            EntryBefore = entryBefore;
            EntryAfter = entryAfter;

            if (EntryBefore == null
                && EntryAfter == null)
            {
                var msg = "At least one entry must not be null!";
                throw new ArgumentException(msg);
            }

            if (EntryBefore != null
                && EntryAfter != null
                && !_entryIdEqualityComparer.Equals(EntryBefore, EntryAfter))
            {
                var msg = "The given entries can't have different ids!";
                throw new ArgumentException(msg);
            }
        }

        public static IEqualityComparer<DifferenceEntry<T>> GetEqualityComparer(IEqualityComparer<T> entryEqualityComparer)
        {
            return new DifferenceEntryEqualityComparer<T>(entryEqualityComparer);
        }

        public DifferenceType DifferenceType
        {
            get
            {
                if (EntryBefore == null)
                {
                    return DifferenceType.Add;
                }

                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (EntryAfter == null)
                {
                    return DifferenceType.Delete;
                }

                return DifferenceType.Update;
            }
        }

        [NotNull]
        public T ExampleEntry => EntryBefore ?? EntryAfter!;

        public int Id => _entryIdEqualityComparer.GetHashCode(ExampleEntry);

        public DifferenceEntry<T> Clone()
        {
            return new DifferenceEntry<T>(EntryBefore, EntryAfter, _entryIdEqualityComparer);
        }

        public string GetStringRepresentation(
            Func<T, string>? entryToStringFunc = null,
            Func<T, T, string>? entryDifferenceToStringFunc = null)
        {
            entryToStringFunc ??= entry => $"{typeof(T).Name}({_entryIdEqualityComparer.GetHashCode(entry)})";
            entryDifferenceToStringFunc ??= (entryBefore, entryAfter) => $"{entryToStringFunc(entryBefore)} -> {entryToStringFunc(entryAfter)}";

            var result = DifferenceType switch
            {
                DifferenceType.Add =>    $"Add:    {entryToStringFunc(EntryAfter!)}",
                DifferenceType.Delete => $"Delete: {entryToStringFunc(EntryBefore!)}",
                DifferenceType.Update => $"Update: {entryDifferenceToStringFunc(EntryBefore!, EntryAfter!)}",
                _ => throw new NotImplementedException()
            };

            return result;
        }
    }
}