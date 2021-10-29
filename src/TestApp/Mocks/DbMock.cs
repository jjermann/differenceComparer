﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DifferenceComparer.Model;

namespace TestApp.Mocks
{
    /// <summary>
    /// Memory DB Mock.
    /// Warning: No clones are used, entries are returned directly.
    /// </summary>
    public class DbMock<T, TU>
        where T : class
        where TU : notnull
    {
        protected readonly Func<T, TU> EntryIdSelector;
        protected readonly IDictionary<TU, T> EntryDictionary;

        public DbMock(Func<T, TU> entryIdSelector)
        {
            EntryIdSelector = entryIdSelector;
            EntryDictionary = new Dictionary<TU, T>();
        }

        public static DbMock<T, TU> InitializeFromJson(
            Func<T, TU> entryIdSelector,
            string json,
            JsonSerializerOptions? options = null)
        {
            var dbMock = new DbMock<T, TU>(entryIdSelector);
            dbMock.AddFromJson(json, options);

            return dbMock;
        }

        public int AddFromJson(string json, JsonSerializerOptions? options = null)
        {
            var entryList = JsonSerializer.Deserialize<ICollection<T>>(json, options);

            if (entryList == null)
            {
                throw new InvalidOperationException("Unable to deserialize collection!");
            }

            return Add(entryList.ToArray());
        }

        public string SerializeToJson(JsonSerializerOptions? options = null)
        {
            options ??= new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(EntryDictionary.Values, options);
        }

        public IEnumerable<T> GetAllAsOrderedEnumerable()
        {
            foreach (var entry in EntryDictionary.Keys
                         .OrderBy(id => id)
                         .Select(id => EntryDictionary[id]))
            {
                yield return entry;
            }
        }

        public IEnumerable<T> GetAllEnumerableByIdList(IList<TU> idList)
        {
            foreach (var entry in idList
                .Select(id => EntryDictionary[id]))
            {
                yield return entry;
            }
        }

        public IList<T> GetAll()
        {
            return EntryDictionary.Values.ToList();
        }

        public IList<T> GetAllByIdList(IList<TU> idList)
        {
            return idList
                .Select(id => EntryDictionary[id])
                .ToList();
        }

        public HashSet<TU> GetAllIds()
        {
            return EntryDictionary.Keys.ToHashSet();
        }

        public T Get(TU id)
        {
            return EntryDictionary[id];
        }

        public int Add(params T[] entryArray)
        {
            foreach (var entry in entryArray)
            {
                var id = GetId(entry);

                if (EntryDictionary.ContainsKey(id))
                {
                    var msg = $"Can't Add: Entry for Id={id} already present!";
                    throw new InvalidOperationException(msg);
                }

                EntryDictionary[id] = entry;
            }

            return entryArray.Length;
        }

        public int Update(params T[] entryArray)
        {
            foreach (var entry in entryArray)
            {
                var id = GetId(entry);

                if (!EntryDictionary.ContainsKey(id))
                {
                    var msg = $"Can't Update: Entry for Id={id} not yet present!";
                    throw new InvalidOperationException(msg);
                }

                EntryDictionary[id] = entry;
            }

            return entryArray.Length;
        }

        public int AddOrUpdate(params T[] entryArray)
        {
            foreach (var entry in entryArray)
            {
                var id = GetId(entry);
                EntryDictionary[id] = entry;
            }

            return entryArray.Length;
        }

        public int Delete(params TU[] idArray)
        {
            foreach (var id in idArray)
            {
                if (!EntryDictionary.ContainsKey(id))
                {
                    var msg = $"Can't Delete: Entry for Id={id} not yet present!";
                    throw new InvalidOperationException(msg);
                }

                EntryDictionary.Remove(id);
            }

            return idArray.Length;
        }

        public int Reset()
        {
            var count = EntryDictionary.Count;

            EntryDictionary.Clear();

            return count;
        }

        public int ApplyDifference(params EquatableDifferenceEntry<T, TU>[] differenceArray)
        {
            var count = 0;

            foreach (var differenceEntry in differenceArray)
            {
                var id = differenceEntry.Id;

                if (differenceEntry.DifferenceType == DifferenceType.Add)
                {
                    count += Add(differenceEntry.EntryAfter!);
                }
                else if (differenceEntry.DifferenceType == DifferenceType.Delete)
                {
                    count += Delete(id);
                }
                else
                {
                    if (!EntryDictionary.ContainsKey(id))
                    {
                        var msg = $"Can't ApplyDifference: Entry for Id={id} not yet present!";
                        throw new InvalidOperationException(msg);
                    }

                    count += Update(differenceEntry.EntryAfter!);
                }
            }

            return count;
        }

        private TU GetId(T entry)
        {
            return EntryIdSelector(entry);
        }
    }
}
