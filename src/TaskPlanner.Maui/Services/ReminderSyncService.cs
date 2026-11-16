using TaskPlanner.Maui.Api;

namespace TaskPlanner.Maui.Services;

/// <summary>
/// Central place to refresh platform deadline / study reminders from the API.
/// </summary>
public sealed class ReminderSyncService
{
    private readonly TaskPlannerApiClient _api;
    private readonly IDeadlineReminderService _reminders;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ReminderSyncService(TaskPlannerApiClient api, IDeadlineReminderService reminders)
    {
        _api = api;
        _reminders = reminders;
    }

    public async Task SyncIfPossibleAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IReadOnlyList<StudyTaskDto> tasks;
            try
            {
                tasks = await _api.GetTasksAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiException)
            {
                return;
            }
            catch (HttpRequestException)
            {
                return;
            }

            var active = tasks
                .Where(t => t.Status != StudyTaskStatus.Completed)
                .ToList();

            IReadOnlyList<EffectiveDayDto> days = [];
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                days = await _api
                    .GetEffectiveAvailabilityAsync(today, today.AddDays(6), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiException)
            {
                // Reminders still work for fixed task deadlines.
            }
            catch (HttpRequestException)
            {
                // Reminders still work for fixed task deadlines.
            }

            await _reminders.SyncAsync(active, days, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}