using TaskPlanner.Maui.Api;

namespace TaskPlanner.Maui.Services;

public interface IDeadlineReminderService
{
    Task SyncAsync(
        IReadOnlyList<StudyTaskDto> tasks,
        IReadOnlyList<EffectiveDayDto> days,
        CancellationToken cancellationToken = default);
}
