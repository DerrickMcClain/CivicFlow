namespace CivicFlow.Domain.Workflow;

public static class RequestNumberFormatter
{
    public static string Format(int year, int sequence)
    {
        if (year is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        return $"CIV-{year}-{sequence:D6}";
    }
}
