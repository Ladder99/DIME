namespace ConsoleApp2;

public static class DateTimeExtensions
{
    private static readonly DateTime Epoch = 
        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ToEpochMilliseconds(this DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - Epoch).TotalMilliseconds;
    }

    public static DateTime FromEpochMilliseconds(long epochMilliseconds)
    {
        return Epoch.AddMilliseconds(epochMilliseconds);
    }
}