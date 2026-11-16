using System.ComponentModel;
using System.Globalization;

namespace TaskPlanner.Maui.Localization;

public sealed class Localizer : INotifyPropertyChanged
{
    public const string Ukrainian = "uk";
    public const string English = "en";
    private const string PreferenceKey = "app_language";

    public static Localizer Current { get; } = new();

    private readonly Dictionary<string, Dictionary<string, string>> _tables = BuildTables();
    private string _language;

    private Localizer()
    {
        _language = Preferences.Default.Get(PreferenceKey, Ukrainian);
        if (_language is not (Ukrainian or English))
        {
            _language = Ukrainian;
        }

        ApplyCulture();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LanguageChanged;

    public string Language => _language;

    public CultureInfo Culture { get; private set; } = new("uk-UA");

    public string this[string key] =>
        _tables[_language].TryGetValue(key, out var value) ? value : key;

    public string Format(string key, params object?[] args) =>
        string.Format(Culture, this[key], args);

    public void SetLanguage(string language)
    {
        if (language is not (Ukrainian or English) || language == _language)
        {
            return;
        }

        _language = language;
        Preferences.Default.Set(PreferenceKey, language);
        ApplyCulture();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyCulture()
    {
        Culture = _language == English ? new CultureInfo("en-US") : new CultureInfo("uk-UA");
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }

    private static Dictionary<string, Dictionary<string, string>> BuildTables() => new()
    {
        [Ukrainian] = new Dictionary<string, string>
        {
            ["Nav_Disciplines"] = "Дисципліни",
            ["Nav_Tasks"] = "Завдання",
            ["Nav_Upcoming"] = "Найближчі",
            ["Nav_Settings"] = "Налаштування",
            ["Nav_Back"] = "Назад",
            ["Login_Subtitle"] = "Дисципліни, дедлайни й оцінки часу в одному місці.",
            ["Login_Password"] = "Пароль",
            ["Login_PasswordPlaceholder"] = "Пароль",
            ["Login_RegisterHint"] = "Лише для нового облікового запису",
            ["Login_Name"] = "Ім’я",
            ["Login_NamePlaceholder"] = "Як до вас звертатися",
            ["Login_SignIn"] = "Увійти",
            ["Login_CreateAccount"] = "Створити обліковий запис",
            ["Login_NeedCredentials"] = "Вкажіть email і пароль.",
            ["Login_NoConnection"] = "Не вдалося з’єднатися з сервером. Перевірте базову URL у налаштуваннях.",
            ["Common_NoConnection"] = "Немає з’єднання з API.",
            ["Common_NotFound"] = "Не знайдено.",
            ["Common_Edit"] = "Редагувати",
            ["Common_Delete"] = "Видалити",
            ["Common_DeleteTitle"] = "Видалити?",
            ["Common_Cancel"] = "Скасувати",
            ["Common_Save"] = "Зберегти",
            ["Common_Add"] = "Додати",
            ["Common_Name"] = "Назва",
            ["Common_NeedTitle"] = "Вкажіть назву.",
            ["Tasks_Hint"] = "Найближчі дедлайни зверху",
            ["Tasks_Empty"] = "Завдань ще немає.\nСтворіть перше й оберіть дисципліну.",
            ["Upcoming_Hint"] = "Лише перегляд. Розклад складається у вебклієнті.",
            ["Upcoming_Empty"] = "На найближчий тиждень сесій немає.",
            ["Upcoming_When"] = "{0} – {1}",
            ["Session_Planned"] = "Заплановано",
            ["Session_Completed"] = "Виконано",
            ["Session_Missed"] = "Пропущено",
            ["Session_NotFound"] = "Сесію не знайдено.",
            ["Session_Start"] = "Початок: {0}",
            ["Session_End"] = "Кінець: {0}",
            ["Session_Status"] = "Статус: {0}",
            ["Session_PlannedMinutes"] = "Заплановано: {0} хв",
            ["Session_ActualMinutes"] = "Факт: {0} хв",
            ["Session_Locked"] = "Фіксована в розкладі",
            ["Tasks_NotFound"] = "Завдання не знайдено.",
            ["Tasks_Due"] = "До {0}",
            ["Tasks_Deadline"] = "Дедлайн: {0}",
            ["Tasks_Status"] = "Статус: {0}",
            ["Tasks_Priority"] = "Пріоритет: {0}",
            ["Tasks_Complexity"] = "Складність: {0} з 5",
            ["Tasks_Estimate"] = "Оцінка: {0} хв",
            ["Tasks_Actual"] = "Факт: {0} хв",
            ["Tasks_New"] = "Нове завдання",
            ["Tasks_Editing"] = "Редагування",
            ["Tasks_Discipline"] = "Дисципліна",
            ["Tasks_What"] = "Що треба зробити",
            ["Tasks_DeadlineField"] = "Дедлайн",
            ["Tasks_TimeField"] = "Час дедлайну",
            ["Tasks_PriorityField"] = "Пріоритет",
            ["Tasks_StatusField"] = "Статус",
            ["Tasks_EstimateField"] = "Оцінка, хвилини",
            ["Tasks_ActualField"] = "Фактично витрачено, хвилини",
            ["Tasks_NeedDiscipline"] = "Оберіть дисципліну (спочатку створіть дисципліну).",
            ["Tasks_DeleteBody"] = "Видалити «{0}»?",
            ["Disciplines_Hint"] = "Потягніть список униз, щоб оновити",
            ["Disciplines_Empty"] = "Ще немає дисциплін.\nДодайте першу, щоб планувати завдання.",
            ["Disciplines_NotFound"] = "Дисципліну не знайдено.",
            ["Disciplines_Count"] = "{0} завдань",
            ["Disciplines_CountLabel"] = "завдань",
            ["Disciplines_New"] = "Нова дисципліна",
            ["Disciplines_Description"] = "Опис",
            ["Disciplines_Optional"] = "Необов’язково",
            ["Disciplines_Color"] = "Колір",
            ["Disciplines_Example"] = "Наприклад, Бази даних",
            ["Disciplines_DeleteBody"] = "Видалити дисципліну «{0}»? Пов’язані завдання стануть недоступні.",
            ["Settings_Language"] = "Мова",
            ["Settings_LanguageHint"] = "Інтерфейс одразу перемкнеться. Назва кнопок унизу теж.",
            ["Settings_Notifications"] = "Сповіщення",
            ["Settings_NotifyHour"] = "За годину до дедлайну",
            ["Settings_NotifyAt"] = "У момент дедлайну",
            ["Settings_NotifyStudyStart"] = "Коли починається час на навчання",
            ["Settings_NotifyMorning"] = "Вранці: план на сьогодні",
            ["Settings_MorningTime"] = "Час ранкового повідомлення",
            ["Settings_NotifyHint"] = "",
            ["Settings_Connection"] = "Сервер",
            ["Settings_ConnectionHint"] = "",
            ["Settings_BaseUrl"] = "Базова URL API",
            ["Settings_Emulator"] = "Емулятор",
            ["Settings_Localhost"] = "localhost",
            ["Settings_SaveUrl"] = "Зберегти адресу",
            ["Settings_Saved"] = "Збережено. Наступний запит піде на цю адресу.",
            ["Settings_EmulatorSet"] = "Встановлено URL для емулятора (10.0.2.2).",
            ["Settings_LocalhostSet"] = "Встановлено http://localhost:5109/.",
            ["Settings_Logout"] = "Вийти з облікового запису",
            ["Priority_Low"] = "Низький",
            ["Priority_Medium"] = "Середній",
            ["Priority_High"] = "Високий",
            ["Priority_Critical"] = "Критичний",
            ["Status_NotStarted"] = "Не розпочато",
            ["Status_InProgress"] = "У процесі",
            ["Status_Completed"] = "Виконано",
            ["Reminder_Soon"] = "Дедлайн скоро",
            ["Reminder_Now"] = "Дедлайн зараз",
            ["Reminder_Channel"] = "Дедлайни",
            ["Reminder_MorningTitle"] = "Сьогоднішнє навчання",
            ["Reminder_MorningBody"] = "Сьогодні варто вчитися {0}.",
            ["Reminder_DayOffTitle"] = "Сьогодні вихідний",
            ["Reminder_DayOffBody"] = "Навчання не заплановане. Можна зайнятися іншими справами.",
            ["Reminder_StudyStarts"] = "Починається час на навчання"
        },
        [English] = new Dictionary<string, string>
        {
            ["Nav_Disciplines"] = "Disciplines",
            ["Nav_Tasks"] = "Tasks",
            ["Nav_Upcoming"] = "Upcoming",
            ["Nav_Settings"] = "Settings",
            ["Nav_Back"] = "Back",
            ["Login_Subtitle"] = "Disciplines, deadlines, and time estimates in one place.",
            ["Login_Password"] = "Password",
            ["Login_PasswordPlaceholder"] = "Password",
            ["Login_RegisterHint"] = "Only for a new account",
            ["Login_Name"] = "Name",
            ["Login_NamePlaceholder"] = "How should we address you",
            ["Login_SignIn"] = "Sign in",
            ["Login_CreateAccount"] = "Create account",
            ["Login_NeedCredentials"] = "Enter an email and a password.",
            ["Login_NoConnection"] = "Could not reach the server. Check the API base URL in Settings.",
            ["Common_NoConnection"] = "No connection to the API.",
            ["Common_NotFound"] = "Not found.",
            ["Common_Edit"] = "Edit",
            ["Common_Delete"] = "Delete",
            ["Common_DeleteTitle"] = "Delete?",
            ["Common_Cancel"] = "Cancel",
            ["Common_Save"] = "Save",
            ["Common_Add"] = "Add",
            ["Common_Name"] = "Name",
            ["Common_NeedTitle"] = "Enter a name.",
            ["Tasks_Hint"] = "Nearest deadlines first",
            ["Tasks_Empty"] = "No tasks yet.\nCreate one and pick a discipline.",
            ["Upcoming_Hint"] = "Read only. The schedule is built in the web client.",
            ["Upcoming_Empty"] = "No sessions in the next week.",
            ["Upcoming_When"] = "{0} – {1}",
            ["Session_Planned"] = "Planned",
            ["Session_Completed"] = "Completed",
            ["Session_Missed"] = "Missed",
            ["Session_NotFound"] = "Session not found.",
            ["Session_Start"] = "Start: {0}",
            ["Session_End"] = "End: {0}",
            ["Session_Status"] = "Status: {0}",
            ["Session_PlannedMinutes"] = "Planned: {0} min",
            ["Session_ActualMinutes"] = "Actual: {0} min",
            ["Session_Locked"] = "Locked in schedule",
            ["Tasks_NotFound"] = "Task was not found.",
            ["Tasks_Due"] = "Due {0}",
            ["Tasks_Deadline"] = "Deadline: {0}",
            ["Tasks_Status"] = "Status: {0}",
            ["Tasks_Priority"] = "Priority: {0}",
            ["Tasks_Complexity"] = "Complexity: {0} of 5",
            ["Tasks_Estimate"] = "Estimate: {0} min",
            ["Tasks_Actual"] = "Actual: {0} min",
            ["Tasks_New"] = "New task",
            ["Tasks_Editing"] = "Edit",
            ["Tasks_Discipline"] = "Discipline",
            ["Tasks_What"] = "What needs to be done",
            ["Tasks_DeadlineField"] = "Deadline",
            ["Tasks_TimeField"] = "Deadline time",
            ["Tasks_PriorityField"] = "Priority",
            ["Tasks_StatusField"] = "Status",
            ["Tasks_EstimateField"] = "Estimate, minutes",
            ["Tasks_ActualField"] = "Time already spent, minutes",
            ["Tasks_NeedDiscipline"] = "Pick a discipline (create one first).",
            ["Tasks_DeleteBody"] = "Delete “{0}”?",
            ["Disciplines_Hint"] = "Pull the list down to refresh",
            ["Disciplines_Empty"] = "No disciplines yet.\nAdd one before planning tasks.",
            ["Disciplines_NotFound"] = "Discipline was not found.",
            ["Disciplines_Count"] = "{0} tasks",
            ["Disciplines_CountLabel"] = "tasks",
            ["Disciplines_New"] = "New discipline",
            ["Disciplines_Description"] = "Description",
            ["Disciplines_Optional"] = "Optional",
            ["Disciplines_Color"] = "Color",
            ["Disciplines_Example"] = "For example, Databases",
            ["Disciplines_DeleteBody"] = "Delete discipline “{0}”? Related tasks will become unavailable.",
            ["Settings_Language"] = "Language",
            ["Settings_LanguageHint"] = "The interface switches immediately, including the tabs below.",
            ["Settings_Notifications"] = "Notifications",
            ["Settings_NotifyHour"] = "One hour before the deadline",
            ["Settings_NotifyAt"] = "At the deadline",
            ["Settings_NotifyStudyStart"] = "When study time starts",
            ["Settings_NotifyMorning"] = "Morning plan for today",
            ["Settings_MorningTime"] = "Morning reminder time",
            ["Settings_NotifyHint"] = "",
            ["Settings_Connection"] = "Server",
            ["Settings_ConnectionHint"] = "",
            ["Settings_BaseUrl"] = "API base URL",
            ["Settings_Emulator"] = "Emulator",
            ["Settings_Localhost"] = "localhost",
            ["Settings_SaveUrl"] = "Save address",
            ["Settings_Saved"] = "Saved. The next request uses this address.",
            ["Settings_EmulatorSet"] = "Emulator URL set (10.0.2.2).",
            ["Settings_LocalhostSet"] = "Set to http://localhost:5109/.",
            ["Settings_Logout"] = "Sign out",
            ["Priority_Low"] = "Low",
            ["Priority_Medium"] = "Medium",
            ["Priority_High"] = "High",
            ["Priority_Critical"] = "Critical",
            ["Status_NotStarted"] = "Not started",
            ["Status_InProgress"] = "In progress",
            ["Status_Completed"] = "Completed",
            ["Reminder_Soon"] = "Deadline soon",
            ["Reminder_Now"] = "Deadline now",
            ["Reminder_Channel"] = "Deadlines",
            ["Reminder_MorningTitle"] = "Today's study",
            ["Reminder_MorningBody"] = "Study today {0}.",
            ["Reminder_DayOffTitle"] = "Day off",
            ["Reminder_DayOffBody"] = "No study time today. The day is free for other things.",
            ["Reminder_StudyStarts"] = "Study time is starting"
        }
    };
}
