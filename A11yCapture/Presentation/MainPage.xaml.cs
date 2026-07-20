using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace A11yCapture.Presentation;

public sealed partial class MainPage : Page
{
    /// <summary>One row of the nav rail, paired with the section it reveals.</summary>
    private sealed record NavEntry(Button Item, Rectangle Tick, TextBlock Label, UIElement Section, string Crumb);

    private readonly List<NavEntry> _entries;

    public MainPage()
    {
        this.InitializeComponent();

        _entries = new List<NavEntry>
        {
            new(NavAccessibility, TickAccessibility, LabelAccessibility, AccessibilityView, "accessibility"),
            new(NavScrollAnchor, TickScrollAnchor, LabelScrollAnchor, ScrollAnchorView, "scroll-anchoring"),
            new(NavInput, TickInput, LabelInput, InputView, "input-ime"),
            new(NavText, TickText, LabelText, TextView, "text-features"),
            new(NavElementTheme, TickElementTheme, LabelElementTheme, ElementThemeView, "element-theming"),
            new(NavVectorGraphics, TickVectorGraphics, LabelVectorGraphics, VectorGraphicsView, "vector-graphics"),
            new(NavMenuFlyout, TickMenuFlyout, LabelMenuFlyout, MenuFlyoutView, "menus-context"),
        };

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
        // The streaming feed must not keep inserting while its section is hidden.
        ScrollAnchorView.Deactivate();

        var target = (Button)sender;
        foreach (var entry in _entries)
        {
            var active = entry.Item == target;

            entry.Section.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            entry.Item.Background = Brush(active ? "LabGray1Brush" : null);
            entry.Tick.Fill = active ? Brush("LabAccentBrush") : Brush(null);
            entry.Label.Foreground = Brush(active ? "LabInkBrush" : "LabMutedBrush");
            entry.Label.FontFamily = (FontFamily)Application.Current.Resources[
                active ? "LabUiMediumFont" : "LabUiFont"];

            if (active)
            {
                CrumbText.Text = entry.Crumb;
            }
        }

        MainScroll.ChangeView(null, 0, null, disableAnimation: true);
    }

    private static Brush Brush(string? key)
        => key is null
            ? new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            : (Brush)Application.Current.Resources[key];
}
