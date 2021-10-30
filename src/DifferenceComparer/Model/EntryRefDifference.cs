namespace DifferenceComparer.Model
{
    public class EntryRefDifference<TU> : EquatableDifferenceEntry<EntryRef<TU>, TU>
        where TU : notnull
    {
        public EntryRefDifference(TU id, int? index1, int? index2)
            : base(
                index1.HasValue ? new EntryRef<TU>(id, index1.Value) : null,
                index2.HasValue ? new EntryRef<TU>(id, index2.Value) : null,
                x => x.Id)
        { }
    }
}