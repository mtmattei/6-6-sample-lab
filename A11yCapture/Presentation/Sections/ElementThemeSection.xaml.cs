namespace A11yCapture.Presentation.Sections;

public sealed partial class ElementThemeSection : UserControl
{
    public ElementThemeSection()
    {
        this.InitializeComponent();
        Loaded += (_, _) => UpdateReadouts();
        ActualThemeChanged += (_, _) => UpdateReadouts();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        RequestedTheme = ((Button)sender).Tag switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        UpdateReadouts();
    }

    /// <summary>
    /// The nested island always shows the opposite of whatever its host resolved to,
    /// so the matryoshka stays legible no matter which way the section is switched.
    /// </summary>
    private void UpdateReadouts()
    {
        var opposite = ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        NestedIsland.RequestedTheme = opposite;

        RequestedValue.Text = RequestedTheme.ToString();
        ActualValue.Text = ActualTheme.ToString();
        NestedHostLabel.Text = ActualTheme == ElementTheme.Dark ? "INHERITED · DARK" : "INHERITED · LIGHT";
        NestedIslandLabel.Text = opposite == ElementTheme.Dark ? "NESTED · DARK" : "NESTED · LIGHT";
        NestedValue.Text = $"{opposite} (opposite of inherited)";

        SetSegState(ThemeDefaultButton, RequestedTheme == ElementTheme.Default);
        SetSegState(ThemeLightButton, RequestedTheme == ElementTheme.Light);
        SetSegState(ThemeDarkButton, RequestedTheme == ElementTheme.Dark);
    }

    private static void SetSegState(Button button, bool active)
    {
        button.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[
            active ? "LabInkBrush" : "LabPaperBrush"];
        button.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[
            active ? "LabPaperBrush" : "LabMutedBrush"];
    }
}
