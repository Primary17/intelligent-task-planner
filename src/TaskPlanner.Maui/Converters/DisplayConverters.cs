using System.Globalization;
using TaskPlanner.Maui.Api;

namespace TaskPlanner.Maui.Converters;

public sealed class PriorityLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TaskPriority priority
            ? priority switch
            {
                TaskPriority.Low => "Низький",
                TaskPriority.Medium => "Середній",
                TaskPriority.High => "Високий",
                TaskPriority.Critical => "Критичний",
                _ => priority.ToString()
            }
            : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StatusLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is StudyTaskStatus status
            ? status switch
            {
                StudyTaskStatus.NotStarted => "Не розпочато",
                StudyTaskStatus.InProgress => "У процесі",
                StudyTaskStatus.Completed => "Виконано",
                _ => status.ToString()
            }
            : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class LocalDeadlineConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime deadline)
        {
            return string.Empty;
        }

        var utc = deadline.Kind switch
        {
            DateTimeKind.Local => deadline.ToUniversalTime(),
            DateTimeKind.Utc => deadline,
            _ => DateTime.SpecifyKind(deadline, DateTimeKind.Utc)
        };

        return utc.ToLocalTime().ToString("d MMMM, HH:mm", new CultureInfo("uk-UA"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
