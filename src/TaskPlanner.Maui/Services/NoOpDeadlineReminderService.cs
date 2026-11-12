using TaskPlanner.Maui.Api;

namespace TaskPlanner.Maui.Services;

public sealed class NoOpDeadlineReminderService : IDeadlineReminderService
{
    public Task SyncAsync(
        IReadOnlyList<StudyTaskDto> tasks,
        IReadOnlyList<EffectiveDayDto> days,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
