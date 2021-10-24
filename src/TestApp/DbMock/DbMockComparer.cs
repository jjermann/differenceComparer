using System.Collections.Generic;
using DifferenceComparer;

namespace TestApp.DbMock
{
    public class DbMockComparer<T>
        where T : class
    {
        private readonly DifferenceComparer<T> _differenceComparer;

        public DbMockComparer(
            IEqualityComparer<T> entryIdEqualityComparer,
            IEqualityComparer<T>? entryEqualityComparer = null)
        {
            _differenceComparer = new DifferenceComparer<T>(
                entryIdEqualityComparer,
                entryEqualityComparer ?? EqualityComparer<T>.Default);
        }

        public List<DifferenceEntry<T>> GetDifference(DbMock<T> dbMockBefore, DbMock<T> dbMockAfter)
        {
            var listBefore = dbMockBefore.GetAll();
            var listAfter = dbMockAfter.GetAll();

            return _differenceComparer.GetDifference(listBefore, listAfter);
        }
    }
}
