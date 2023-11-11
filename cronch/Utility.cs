namespace cronch;

public static class Utility
{
    public static string CreateExecutionName()
    {
        return $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmssfff}_{Path.GetRandomFileName()}";
    }
}
