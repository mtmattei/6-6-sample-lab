using A11yCapture.Presentation.Sections;

namespace A11yCapture.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => LogoQuadrantLoop.Stop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
        {
            LogoQuadrantLoop.Begin();
        }
        // With animations disabled, the top arm stays filled (its XAML default).
    }

    private void NavItem_Click(object sender, RoutedEventArgs e)
    {
        // Streaming feed must not keep inserting while its section is hidden.
        ScrollAnchorView.Deactivate();

        var target = (Button)sender;
        ShowSection(
            accessibility: target == NavAccessibility,
            scrollAnchor: target == NavScrollAnchor,
            input: target == NavInput);
    }

    private void ShowSection(bool accessibility, bool scrollAnchor, bool input)
    {
        AccessibilityView.Visibility = accessibility ? Visibility.Visible : Visibility.Collapsed;
        ScrollAnchorView.Visibility = scrollAnchor ? Visibility.Visible : Visibility.Collapsed;
        InputView.Visibility = input ? Visibility.Visible : Visibility.Collapsed;

        CrumbText.Text = accessibility ? "accessibility" : scrollAnchor ? "scroll-anchoring" : "input-ime";

        SetNavState(NavAccessibility, TickAccessibility, LabelAccessibility, accessibility);
        SetNavState(NavScrollAnchor, TickScrollAnchor, LabelScrollAnchor, scrollAnchor);
        SetNavState(NavInput, TickInput, LabelInput, input);

        MainScroll.ChangeView(null, 0, null, disableAnimation: true);
    }

    private void SetNavState(Button item, Microsoft.UI.Xaml.Shapes.Rectangle tick, TextBlock label, bool active)
    {
        item.Background = active
            ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LabGray1Brush"]
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        tick.Fill = active
            ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LabAccentBrush"]
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        label.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[active ? "LabInkBrush" : "LabMutedBrush"];
        label.FontFamily = (FontFamily)Application.Current.Resources[active ? "LabUiMediumFont" : "LabUiFont"];
    }
}
