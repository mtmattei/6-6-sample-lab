using System;

namespace A11yCapture.Presentation.Sections;

public sealed partial class HomeSection : UserControl
{
    /// <summary>Raised with the section key of the card that was picked.</summary>
    public event EventHandler<string>? SampleSelected;

    public HomeSection()
    {
        this.InitializeComponent();
    }

    private void OnCardClick(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is string key)
        {
            SampleSelected?.Invoke(this, key);
        }
    }
}
