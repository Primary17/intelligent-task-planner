using System.Globalization;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;

namespace TaskPlanner.Maui.Converters;

public sealed class PriorityLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TaskPriority priority)
        {
            return string.Empty;
        }

        var key = priority switch
        {
            TaskPriority.Low => "Priority_Low",
            TaskPriority.Medium => "Priority_Medium",
            TaskPriority.High => "Priority_High",
            TaskPriority.Critical => "Priority_Critical",
            _ => null
        };
        return key is null ? priority.ToString() : Localizer.Current[key];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StatusLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not StudyTaskStatus status)
        {
            return string.Empty;
        }

        var key = status switch
        {
            StudyTaskStatus.NotStarted => "Status_NotStarted",
            StudyTaskStatus.InProgress => "Status_InProgress",
            StudyTaskStatus.Completed => "Status_Completed",
            _ => null
        };
        return key is null ? status.ToString() : Localizer.Current[key];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SessionStatusLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SessionStatus status)
        {
            return string.Empty;
        }

        var key = status switch
        {
            SessionStatus.Completed => "Session_Completed",
            SessionStatus.Missed => "Session_Missed",
            _ => "Session_Planned"
        };
        return Localizer.Current[key];
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

        return utc.ToLocalTime().ToString("d MMMM, HH:mm", Localizer.Current.Culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class RefreshConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 0 || values[0] is null ? string.Empty : values[0];

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class LocFormatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string key || values.Length == 0 || values[0] is null)
        {
            return string.Empty;
        }

        return Localizer.Current.Format(key, values[0]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
