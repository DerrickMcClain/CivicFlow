using CivicFlow.Domain.Workflow;

namespace CivicFlow.Tests;

public class RequestNumberFormatterTests
{
    [Fact]
    public void Formats_year_and_six_digit_sequence()
    {
        Assert.Equal("CIV-2026-000184", RequestNumberFormatter.Format(2026, 184));
    }

    [Fact]
    public void Rejects_non_positive_sequence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RequestNumberFormatter.Format(2026, 0));
    }
}
