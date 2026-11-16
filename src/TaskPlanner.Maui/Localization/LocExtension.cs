namespace TaskPlanner.Maui.Localization;

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Key))]
public sealed class LocExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider) => new Binding
    {
        Mode = BindingMode.OneWay,
        Path = $"[{Key}]",
        Source = Localizer.Current
    };

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
