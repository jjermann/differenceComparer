using System.Collections.Generic;

namespace TestApp.TestData
{
    public class SimpleTestEntryIdEqualityComparer : IEqualityComparer<SimpleTestEntry>
    {
        public bool Equals(SimpleTestEntry? x, SimpleTestEntry? y)
        {
            return x?.Id == y?.Id;
        }

        public int GetHashCode(SimpleTestEntry obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
