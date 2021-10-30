using System.Collections.Generic;
using System.Text.Json;
using DifferenceComparer.Model;

namespace TestApp
{
    public interface IDbMock<T, TU>
        where T : class
        where TU : notnull
    {
        void AddFromJson(string json, JsonSerializerOptions? options = null);
        string SerializeToJson(JsonSerializerOptions? options = null);
        IEnumerable<T> GetAllAsOrderedEnumerable();
        IEnumerable<T> GetAllEnumerableByIdList(IList<TU> idList);
        IList<T> GetAll();
        IList<T> GetAllByIdList(IList<TU> idList);
        HashSet<TU> GetAllIds();
        T Get(TU id);
        void Add(params T[] entryArray);
        void Update(params T[] entryArray);
        void Delete(params TU[] idArray);
        int Reset();
        void ApplyDifference(params EquatableDifferenceEntry<T, TU>[] differenceArray);
    }
}
