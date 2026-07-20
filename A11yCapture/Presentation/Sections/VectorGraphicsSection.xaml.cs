using Microsoft.UI.Xaml.Media.Animation;

namespace A11yCapture.Presentation.Sections;

public sealed partial class VectorGraphicsSection : UserControl
{
    private Storyboard? _spin;

    public VectorGraphicsSection()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _spin?.Stop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_spin is not null)
        {
            return;
        }

        // Angle is not an independently-animatable property, so this needs
        // EnableDependentAnimation to run at all.
        var anim = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = new Duration(TimeSpan.FromSeconds(4)),
            RepeatBehavior = RepeatBehavior.Forever,
            EnableDependentAnimation = true,
        };

        Storyboard.SetTarget(anim, SpinTransform);
        Storyboard.SetTargetProperty(anim, "Angle");

        _spin = new Storyboard();
        _spin.Children.Add(anim);

        if (new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
        {
            _spin.Begin();
        }
        else
        {
            SpinToggle.IsChecked = false;
            SpinValue.Text = "paused (reduced motion)";
        }
    }

    private void OnSpinToggled(object sender, RoutedEventArgs e)
    {
        if (_spin is null)
        {
            return;
        }

        if (SpinToggle.IsChecked == true)
        {
            _spin.Resume();
            SpinValue.Text = "rotating · 360° / 4s";
        }
        else
        {
            _spin.Pause();
            SpinValue.Text = "paused";
        }
    }
}
