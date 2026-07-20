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
}
