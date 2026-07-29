using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;

namespace A11yCapture.Presentation;

public sealed partial class MainPage : Page
{
    /// <summary>One row of the nav rail, paired with the section it reveals.</summary>
    private sealed record NavEntry(
        string Slug,
        Button Item,
        Rectangle Tick,
        TextBlock Label,
        UIElement Section,
        string Crumb);

    private readonly List<NavEntry> _entries;

    public MainPage()
    {
        this.InitializeComponent();

        _entries = new List<NavEntry>
        {
            new("index", NavHome, TickHome, LabelHome, HomeView, "index"),
            new("accessibility", NavAccessibility, TickAccessibility, LabelAccessibility, AccessibilityView, "accessibility"),
            new("scroll-anchoring", NavScrollAnchor, TickScrollAnchor, LabelScrollAnchor, ScrollAnchorView, "scroll-anchoring"),
            new("input-ime", NavInput, TickInput, LabelInput, InputView, "input-ime"),
            new("text-features", NavText, TickText, LabelText, TextView, "text-features"),
            new("element-theming", NavElementTheme, TickElementTheme, LabelElementTheme, ElementThemeView, "element-theming"),
            new("vector-graphics", NavVectorGraphics, TickVectorGraphics, LabelVectorGraphics, VectorGraphicsView, "vector-graphics"),
            new("projection", NavProjection, TickProjection, LabelProjection, ProjectionView, "projection"),
            new("menus-context", NavMenuFlyout, TickMenuFlyout, LabelMenuFlyout, MenuFlyoutView, "menus-context"),
        };

        Loaded += OnLoaded;
        SizeChanged += OnFirstSizeChanged;
        Unloaded += (_, _) => LogoQuadrantLoop.Stop();
    }

    // Narrow hosts (blog iframes) start with the source panel collapsed; the
    // first measured width decides the default, then only the user toggles it.
    private const double SourceAutoCollapseWidth = 1100;
    private bool _sourceCollapsed;
    private bool _sourceDefaultApplied;

    private void OnFirstSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_sourceDefaultApplied)
        {
            return;
        }

        _sourceDefaultApplied = true;
        _sourceCollapsed = e.NewSize.Width < SourceAutoCollapseWidth;
        ApplySourceVisibility();
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
        var key = _entries.FirstOrDefault(x => x.Item == (Button)sender)?.Slug;
        if (key is not null)
        {
            Navigate(key);
        }
    }

    private void OnSampleSelected(object? sender, string key)
        => Navigate(key);

    private void Navigate(string key)
    {
        // The streaming feed must not keep inserting while its section is hidden.
        ScrollAnchorView.Deactivate();

        // Same for the projection spin: a collapsed section still holds its
        // CompositionTarget.Rendering hook unless it is told to let go. Guarded on
        // the target, so arriving at the section does not land on a paused card.
        if (key == "projection")
        {
            ProjectionView.Activate();
        }
        else
        {
            ProjectionView.Deactivate();
        }

        foreach (var entry in _entries)
        {
            var active = entry.Slug == key;

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

        UpdateSourcePanel(key);

        MainScroll.ChangeView(null, 0, null, disableAnimation: true);
    }

    // ---- Source panel -------------------------------------------------

    private SampleSource? _activeSource;
    private string _activeTab = "xaml";

    private void UpdateSourcePanel(string slug)
    {
        if (SampleSources.BySlug.TryGetValue(slug, out var source))
        {
            _activeSource = source;
            ApplySourceVisibility();
            RenderSource();
        }
        else
        {
            _activeSource = null;
            ApplySourceVisibility();
        }
    }

    private void ApplySourceVisibility()
    {
        var hasSource = _activeSource is not null;
        SourcePanel.Visibility = hasSource && !_sourceCollapsed ? Visibility.Visible : Visibility.Collapsed;
        SourceRail.Visibility = hasSource && _sourceCollapsed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CollapseSource_Click(object sender, RoutedEventArgs e)
    {
        _sourceCollapsed = true;
        ApplySourceVisibility();
    }

    private void ExpandSource_Click(object sender, RoutedEventArgs e)
    {
        _sourceCollapsed = false;
        ApplySourceVisibility();
    }

    private void SourceTab_Click(object sender, RoutedEventArgs e)
    {
        _activeTab = sender == TabCSharpBtn ? "csharp"
                   : sender == TabNotesBtn ? "notes"
                   : "xaml";
        RenderSource();
    }

    private void RenderSource()
    {
        if (_activeSource is null)
        {
            return;
        }

        SetTabVisual(TabXamlBtn, TabXamlLine, _activeTab == "xaml");
        SetTabVisual(TabCSharpBtn, TabCSharpLine, _activeTab == "csharp");
        SetTabVisual(TabNotesBtn, TabNotesLine, _activeTab == "notes");

        var text = ActiveTabText();
        CodeHost.Children.Clear();

        if (_activeTab == "notes")
        {
            // Notes are prose: no gutter, one wrapped block.
            CodeHost.Children.Add(MakeCodeText(text, "LabInkBrush"));
        }
        else
        {
            // One row per line so wrapped continuations indent under the
            // code column instead of drifting the gutter out of sync.
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var row = new Grid { ColumnSpacing = 12 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var number = MakeCodeText((i + 1).ToString(), "LabLine2Brush");
                number.TextAlignment = TextAlignment.Right;

                // An empty TextBlock measures to zero height; keep blank lines tall.
                var code = MakeCodeText(lines[i].Length == 0 ? " " : lines[i], "LabInkBrush");
                Grid.SetColumn(code, 1);

                row.Children.Add(number);
                row.Children.Add(code);
                CodeHost.Children.Add(row);
            }
        }

        TipText.Text = _activeSource.Tip;
        CopyLabel.Text = "COPY";
        CodeScroll.ChangeView(0, 0, null, disableAnimation: true);
    }

    private static TextBlock MakeCodeText(string text, string brushKey)
        => new()
        {
            Text = text,
            FontFamily = (FontFamily)Application.Current.Resources["LabMonoFont"],
            FontSize = 12,
            LineHeight = 19,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush(brushKey),
            IsTextSelectionEnabled = true,
        };

    private string ActiveTabText()
        => _activeTab switch
        {
            "csharp" => _activeSource?.CSharp ?? "",
            "notes" => _activeSource?.Notes ?? "",
            _ => _activeSource?.Xaml ?? "",
        };

    private void SetTabVisual(Button tab, Rectangle line, bool active)
    {
        tab.Foreground = Brush(active ? "LabInkBrush" : "LabMutedBrush");
        tab.FontFamily = (FontFamily)Application.Current.Resources[
            active ? "LabMonoMediumFont" : "LabMonoFont"];
        line.Fill = active ? Brush("LabInkBrush") : Brush(null);
    }

    private async void CopySource_Click(object sender, RoutedEventArgs e)
    {
        var text = ActiveTabText();
        if (text.Length == 0)
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);

        CopyLabel.Text = "COPIED";
        await Task.Delay(1400);
        CopyLabel.Text = "COPY";
    }

    private static Brush Brush(string? key)
        => key is null
            ? new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            : (Brush)Application.Current.Resources[key];
}
