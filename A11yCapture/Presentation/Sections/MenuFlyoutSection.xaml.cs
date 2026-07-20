using System.Linq;
using Microsoft.UI.Xaml.Input;

namespace A11yCapture.Presentation.Sections;

public sealed partial class MenuFlyoutSection : UserControl
{
    private int _eventCounter;

    public MenuFlyoutSection()
    {
        this.InitializeComponent();
    }

    private void LogEvent(string message)
    {
        _eventCounter++;
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        EventLogText.Text = $"[{_eventCounter}] {timestamp}  {message}\n" + EventLogText.Text;

        if (EventLogText.Text.Length > 4000)
        {
            EventLogText.Text = EventLogText.Text.Substring(0, 3500);
        }
    }

    // Button has no "flyout is open" visual state, so the open menu's button is
    // held in the selected fill for as long as its flyout is showing.
    private void OnMenuOpened(object? sender, object e)
        => SetOpenState(sender, open: true);

    private void OnMenuClosed(object? sender, object e)
        => SetOpenState(sender, open: false);

    private void SetOpenState(object? flyout, bool open)
    {
        var owner = (flyout as MenuFlyout)?.Target as Button
            ?? new[] { FileMenuButton, EditMenuButton, ViewMenuButton }
                .FirstOrDefault(b => ReferenceEquals(b.Flyout, flyout));

        if (owner is not null)
        {
            owner.Background = open
                ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LabGray2Brush"]
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }

    private void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item)
        {
            MenuStatus.Text = $"Invoked: {item.Text}";
            LogEvent($"MenuFlyoutItem '{item.Text}'");
        }
    }

    private void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem toggle)
        {
            MenuStatus.Text = $"{toggle.Text}: {(toggle.IsChecked ? "on" : "off")}";
            LogEvent($"ToggleMenuFlyoutItem '{toggle.Text}' -> {(toggle.IsChecked ? "checked" : "unchecked")}");
        }
    }

    // TryGetPosition returns false for keyboard invocation (Shift+F10) — that
    // distinction is the point of the demo, so both branches are surfaced.
    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (args.TryGetPosition(sender, out var position))
        {
            PositionText.Text = $"Position: ({position.X:F0}, {position.Y:F0})";
            LogEvent($"ContextRequested (pointer) at ({position.X:F0}, {position.Y:F0})");
        }
        else
        {
            PositionText.Text = "Position: keyboard (Shift+F10)";
            LogEvent("ContextRequested (keyboard), TryGetPosition returned false");
        }
    }

    private void OnContextCanceled(UIElement sender, RoutedEventArgs args)
    {
        PositionText.Text = "Context canceled";
        LogEvent("ContextCanceled, gesture dragged away before the menu opened");
    }

    private void OnClearLogClick(object sender, RoutedEventArgs e)
    {
        _eventCounter = 0;
        EventLogText.Text = "Events will be logged here.";
        PositionText.Text = "Position: (waiting)";
        MenuStatus.Text = "No item invoked yet";
    }
}
