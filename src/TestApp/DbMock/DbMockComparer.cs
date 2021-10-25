using System.Collections.Generic;
using DifferenceComparer;

namespace TestApp.DbMock
{
    public class DbMockComparer<T>
        where T : class
    {
        public DifferenceComparer<T> DifferenceComparer { get; }

        public DbMockComparer(
            IEqualityComparer<T> entryIdEqualityComparer,
            IEqualityComparer<T>? entryEqualityComparer = null)
        {
            DifferenceComparer = new DifferenceComparer<T>(
                entryIdEqualityComparer,
                entryEqualityComparer ?? EqualityComparer<T>.Default);
        }

        public List<DifferenceEntry<T>> GetDifference(DbMock<T> dbMockBefore, DbMock<T> dbMockAfter)
        {
            var listBefore = dbMockBefore.GetAll();
            var listAfter = dbMockAfter.GetAll();

            return DifferenceComparer.GetDifference(listBefore, listAfter);
        }
    }
}
