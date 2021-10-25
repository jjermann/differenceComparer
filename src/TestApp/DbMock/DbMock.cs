using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DifferenceComparer;

namespace TestApp.DbMock
{
    /// <summary>
    /// Memory DB Mock.
    /// Warning: No clones are used, entries are returned directly.
    /// </summary>
    public class DbMock<T>
        where T : class
    {
        private readonly IEqualityComparer<T> _entryIdEqualityComparer;
        private readonly IDictionary<int, T> _entryDictionary;

        public DbMock(IEqualityComparer<T> entryIdEqualityComparer)
        {
            _entryIdEqualityComparer = entryIdEqualityComparer;
            _entryDictionary = new Dictionary<int, T>();
        }

        public static DbMock<T> InitializeFromJson(
            IEqualityComparer<T> entryIdEqualityComparer,
            string json,
            JsonSerializerOptions? options = null)
        {
            var dbMock = new DbMock<T>(entryIdEqualityComparer);
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

            return JsonSerializer.Serialize(_entryDictionary.Values, options);
        }

        public IEnumerable<T> GetAllAsOrderedEnumerable()
        {
            foreach (var entry in _entryDictionary.Keys
                         .OrderBy(id => id)
                         .Select(id => _entryDictionary[id]))
            {
                yield return entry;
            }
        }

        public IEnumerable<T> GetAllEnumerableByIdList(IList<int> idList)
        {
            foreach (var entry in idList
                .Select(id => _entryDictionary[id]))
            {
                yield return entry;
            }
        }

        public IList<T> GetAll()
        {
            return _entryDictionary.Values.ToList();
        }

        public IList<T> GetAllByIdList(IList<int> idList)
        {
            return idList
                .Select(id => _entryDictionary[id])
                .ToList();
        }

        public HashSet<int> GetAllIds()
        {
            return _entryDictionary.Keys.ToHashSet();
        }

        public T Get(int id)
        {
            return _entryDictionary[id];
        }

        public int Add(params T[] entryArray)
        {
            foreach (var entry in entryArray)
            {
                var id = GetId(entry);

                if (_entryDictionary.ContainsKey(id))
                {
                    var msg = $"Can't Add: Entry for Id={id} already present!";
                    throw new InvalidOperationException(msg);
                }

                _entryDictionary[id] = entry;
            }

            return entryArray.Length;
        }

        public int Update(params T[] entryArray)
        {
            foreach (var entry in entryArray)
            {
                var id = GetId(entry);

                if (!_entryDictionary.ContainsKey(id))
                {
                    var msg = $"Can't Update: Entry for Id={id} not yet present!";
                    throw new InvalidOperationException(msg);
                }

                _entryDictionary[id] = entry;
            }

            return entryArray.Length;
        }

        public int AddOrUpdate(params T[] entryArray)
        {
            foreach (var entry in entryArray)
            {
                var id = GetId(entry);
                _entryDictionary[id] = entry;
            }

            return entryArray.Length;
        }

        public int Delete(params int[] idArray)
        {
            foreach (var id in idArray)
            {
                if (!_entryDictionary.ContainsKey(id))
                {
                    var msg = $"Can't Delete: Entry for Id={id} not yet present!";
                    throw new InvalidOperationException(msg);
                }

                _entryDictionary.Remove(id);
            }

            return idArray.Length;
        }

        public int Reset()
        {
            var count = _entryDictionary.Count;

            _entryDictionary.Clear();

            return count;
        }

        public int ApplyDifference(params EquatableDifferenceEntry<T>[] differenceArray)
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
                    if (!_entryDictionary.ContainsKey(id))
                    {
                        var msg = $"Can't ApplyDifference: Entry for Id={id} not yet present!";
                        throw new InvalidOperationException(msg);
                    }

                    count += Update(differenceEntry.EntryAfter!);
                }
            }

            return count;
        }

        private int GetId(T entry)
        {
            return _entryIdEqualityComparer.GetHashCode(entry);
        }
    }
}
