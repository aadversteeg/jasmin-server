namespace Core.Domain.Paging;

public record DateRangeFilter(DateTime? From, DateTime? To)
{
    public bool IsInRange(DateTime timestamp)
    {
        if (From.HasValue && timestamp < From.Value)
            return false;
        if (To.HasValue && timestamp > To.Value)
            return false;
        return true;
    }
}
