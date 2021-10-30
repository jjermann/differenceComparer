using System.Collections.Generic;
using DifferenceComparer.Model;

namespace TestApp
{
    public interface IDifferenceDbMock<T, TU>: IDbMock<DifferenceEntry<T, TU>, TU>
        where T : class
        where TU : notnull
    {
        ICollection<EntryRefDifference<TU>> GetAllEntryRefDifferences();
    }
}
