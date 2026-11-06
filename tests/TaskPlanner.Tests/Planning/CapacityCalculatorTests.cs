using TaskPlanner.Application.DTOs.Planning;
using TaskPlanner.Application.Planning;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Planning;

public sealed class CapacityCalculatorTests
{
    private static readonly DateTime Monday8 = new(2030, 1, 7, 8, 0, 0, DateTimeKind.Utc);

    private static AvailabilityWindow Window(DayOfWeek day, int startHour, int endHour) => new()
    {
        DayOfWeek = day,
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(endHour, 0)
    };

    private static readonly AvailabilityWindow[] Weekly =
    {
        Window(DayOfWeek.Monday, 17, 20),
        Window(DayOfWeek.Tuesday, 18, 22)
    };

    private static CapacityTaskInput Task(string title, DateTime deadline, int remaining, int planned = 0) =>
        new(Guid.NewGuid(), title, Guid.NewGuid(), "OS", deadline, remaining, planned);

    private static CapacityReportDto Run(
        IReadOnlyCollection<CapacityTaskInput> tasks,
        DateTime? now = null,
        int offset = 0,
        IReadOnlyCollection<AvailabilityWindow>? windows = null,
        IReadOnlyCollection<AvailabilityException>? exceptions = null) =>
        CapacityCalculator.Calculate(now ?? Monday8, offset, windows ?? Weekly, exceptions ?? Array.Empty<AvailabilityException>(), tasks);

    private static readonly DateTime TuesdayEnd = new(2030, 1, 8, 23, 59, 0, DateTimeKind.Utc);

    [Fact]
    public void EnoughTime_IsOk()
    {
        var row = Assert.Single(Run(new[] { Task("Lab", TuesdayEnd, 120) }).Tasks);

        Assert.Equal(CapacityStatus.Ok, row.Status);
        Assert.Equal(420, row.AvailableMinutes);
        Assert.Equal(120, row.RequiredMinutes);
        Assert.Equal(0, row.ShortfallMinutes);
    }

    [Fact]
    public void NotEnoughTime_ReportsShortfall()
    {
        var report = Run(new[] { Task("Lab", TuesdayEnd, 600) });

        var row = Assert.Single(report.Tasks);
        Assert.Equal(CapacityStatus.Shortfall, row.Status);
        Assert.Equal(180, row.ShortfallMinutes);
        Assert.Equal(180, report.TotalShortfallMinutes);
    }

    [Fact]
    public void EarlierTasksUseUpTimeForLaterOnes()
    {
        var first = Task("A", new DateTime(2030, 1, 7, 23, 0, 0, DateTimeKind.Utc), 120);
        var second = Task("B", TuesdayEnd, 400);

        var report = Run(new[] { second, first });

        Assert.Equal(new[] { "A", "B" }, report.Tasks.Select(t => t.Title));
        Assert.Equal(CapacityStatus.Ok, report.Tasks[0].Status);
        Assert.Equal(CapacityStatus.Shortfall, report.Tasks[1].Status);
        Assert.Equal(520, report.Tasks[1].RequiredMinutes);
        Assert.Equal(100, report.Tasks[1].ShortfallMinutes);
        Assert.Equal(100, report.TotalShortfallMinutes);
    }

    [Fact]
    public void UnavailableException_RemovesTheDay()
    {
        var exception = new AvailabilityException
        {
            Date = new DateOnly(2030, 1, 8),
            Kind = AvailabilityExceptionKind.Unavailable
        };

        var row = Assert.Single(Run(new[] { Task("Lab", TuesdayEnd, 100) }, exceptions: new[] { exception }).Tasks);

        Assert.Equal(180, row.AvailableMinutes);
    }

    [Fact]
    public void CustomHoursException_ReplacesWeeklyWindows()
    {
        var exception = new AvailabilityException
        {
            Date = new DateOnly(2030, 1, 8),
            Kind = AvailabilityExceptionKind.CustomHours,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0)
        };

        var row = Assert.Single(Run(new[] { Task("Lab", TuesdayEnd, 100) }, exceptions: new[] { exception }).Tasks);

        Assert.Equal(240, row.AvailableMinutes);
    }

    [Fact]
    public void WindowInProgress_IsCountedFromNow()
    {
        var now = new DateTime(2030, 1, 7, 18, 0, 0, DateTimeKind.Utc);

        var row = Assert.Single(Run(new[] { Task("Lab", new DateTime(2030, 1, 7, 23, 0, 0, DateTimeKind.Utc), 60) }, now).Tasks);

        Assert.Equal(120, row.AvailableMinutes);
    }

    [Fact]
    public void DeadlineInsideWindow_CutsTheWindow()
    {
        var deadline = new DateTime(2030, 1, 7, 18, 30, 0, DateTimeKind.Utc);

        var row = Assert.Single(Run(new[] { Task("Lab", deadline, 60) }).Tasks);

        Assert.Equal(90, row.AvailableMinutes);
    }

    [Fact]
    public void UtcOffset_ShiftsLocalWindowsToUtc()
    {
        var deadline = new DateTime(2030, 1, 7, 17, 0, 0, DateTimeKind.Utc);

        var row = Assert.Single(Run(new[] { Task("Lab", deadline, 60) }, offset: 120).Tasks);

        Assert.Equal(120, row.AvailableMinutes);
    }

    [Fact]
    public void PastDeadline_IsOverdue_AndDoesNotBlockOthers()
    {
        var overdue = Task("Old", new DateTime(2030, 1, 6, 12, 0, 0, DateTimeKind.Utc), 300);
        var fresh = Task("New", TuesdayEnd, 120);

        var report = Run(new[] { overdue, fresh });

        Assert.Equal(CapacityStatus.Overdue, report.Tasks[0].Status);
        Assert.Equal(300, report.Tasks[0].ShortfallMinutes);
        Assert.Equal(CapacityStatus.Ok, report.Tasks[1].Status);
        Assert.Equal(120, report.Tasks[1].RequiredMinutes);
        Assert.Equal(0, report.TotalShortfallMinutes);
    }

    [Fact]
    public void FinishedTasks_AreIgnored()
    {
        Assert.Empty(Run(new[] { Task("Done", TuesdayEnd, 0) }).Tasks);
    }

    [Fact]
    public void PlannedSessions_ReduceUnplannedMinutes()
    {
        var row = Assert.Single(Run(new[] { Task("Lab", TuesdayEnd, 200, planned: 150) }).Tasks);

        Assert.Equal(50, row.UnplannedMinutes);
        Assert.Equal(0, Assert.Single(Run(new[] { Task("Lab", TuesdayEnd, 100, planned: 150) }).Tasks).UnplannedMinutes);
    }

    [Fact]
    public void NoAvailability_MeansEverythingIsShortfall()
    {
        var report = Run(new[] { Task("Lab", TuesdayEnd, 90) }, windows: Array.Empty<AvailabilityWindow>());

        Assert.Equal(90, Assert.Single(report.Tasks).ShortfallMinutes);
    }

    [Fact]
    public void FarDeadline_UsesAtMostOneYearOfWindows()
    {
        var deadline = Monday8.AddDays(1000);

        var row = Assert.Single(Run(new[] { Task("Lab", deadline, 10) }).Tasks);

        Assert.InRange(row.AvailableMinutes, 1, 60 * 24 * 366);
    }
}
