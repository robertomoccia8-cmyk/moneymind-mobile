namespace MoneyMindApp.Behaviors;

/// <summary>
/// Behavior that adds touch feedback animation to any View
/// Scales down on press, scales back on release
/// </summary>
public class TouchFeedbackBehavior : Behavior<View>
{
    private View? _element;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _element = bindable;

        // Add tap gesture recognizer
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnTapped;
        bindable.GestureRecognizers.Add(tapGesture);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        base.OnDetachingFrom(bindable);
        _element = null;
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (_element == null) return;

        // Quick pulse animation
        await _element.ScaleTo(0.95, 50, Easing.CubicOut);
        await _element.ScaleTo(1.0, 100, Easing.CubicIn);
    }
}

/// <summary>
/// Behavior that adds fade-in animation when page appears
/// </summary>
public class FadeInBehavior : Behavior<VisualElement>
{
    public static readonly BindableProperty DurationProperty =
        BindableProperty.Create(nameof(Duration), typeof(uint), typeof(FadeInBehavior), (uint)300);

    public uint Duration
    {
        get => (uint)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Opacity = 0;
        bindable.Loaded += OnLoaded;
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.Loaded -= OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            await element.FadeTo(1, Duration, Easing.CubicOut);
        }
    }
}

/// <summary>
/// Behavior that adds slide-up animation when element appears
/// </summary>
public class SlideUpBehavior : Behavior<VisualElement>
{
    public static readonly BindableProperty DurationProperty =
        BindableProperty.Create(nameof(Duration), typeof(uint), typeof(SlideUpBehavior), (uint)400);

    public static readonly BindableProperty OffsetProperty =
        BindableProperty.Create(nameof(Offset), typeof(double), typeof(SlideUpBehavior), 50.0);

    public uint Duration
    {
        get => (uint)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public double Offset
    {
        get => (double)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Opacity = 0;
        bindable.TranslationY = Offset;
        bindable.Loaded += OnLoaded;
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.Loaded -= OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            await Task.WhenAll(
                element.FadeTo(1, Duration, Easing.CubicOut),
                element.TranslateTo(0, 0, Duration, Easing.CubicOut)
            );
        }
    }
}
