using CivicFlow.Domain.Enums;
using CivicFlow.Domain.Workflow;

namespace CivicFlow.Tests;

public class SlaPolicyTests
{
    [Theory]
    [InlineData(Priority.Low, 10)]
    [InlineData(Priority.Medium, 5)]
    [InlineData(Priority.High, 2)]
    public void ComputeDueAt_uses_priority_based_day_offsets(Priority priority, int expectedDays)
    {
        var submitted = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var due = SlaPolicy.ComputeDueAt(priority, submitted);
        Assert.Equal(submitted.AddDays(expectedDays), due);
    }

    [Fact]
    public void IsOverdue_is_false_for_completed_cases()
    {
        var due = DateTime.UtcNow.AddDays(-1);
        Assert.False(SlaPolicy.IsOverdue(RequestStatusName.Completed, due));
    }

    [Fact]
    public void IsOverdue_is_true_for_open_cases_past_due()
    {
        var due = DateTime.UtcNow.AddDays(-1);
        Assert.True(SlaPolicy.IsOverdue(RequestStatusName.UnderReview, due));
    }
}
