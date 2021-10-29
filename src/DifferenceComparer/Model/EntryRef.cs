namespace DifferenceComparer.Model
{
    public record EntryRef<TU>(TU Id, int Index = 0);
}