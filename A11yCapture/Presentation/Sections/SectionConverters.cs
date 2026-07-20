using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace A11yCapture.Presentation.Sections;

/// <summary>
/// Anchored -> the lab accent (state colour), otherwise the faint hairline.
/// Border thickness stays constant so toggling the highlight never reflows the row.
/// </summary>
public sealed class AnchorToAccentBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => Application.Current.Resources[value is true ? "LabAccentBrush" : "LabLineBrush"] as Brush;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>true -> Visible, false -> Collapsed.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Visible;
}

/// <summary>
/// Maps a <see cref="CompositionPhase"/> to the lab value-ramp brush: state reads by
/// darkness (idle = faint hairline, committed = ink); candidate uses the accent.
/// </summary>
public sealed class PhaseToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var key = value is CompositionPhase phase
            ? phase switch
            {
                CompositionPhase.Composing => "PhaseComposingBrush",
                CompositionPhase.Candidate => "PhaseCandidateBrush",
                CompositionPhase.Committed => "PhaseCommittedBrush",
                _ => "PhaseIdleBrush",
            }
            : "PhaseIdleBrush";

        return Application.Current.Resources.TryGetValue(key, out var brush)
            ? brush
            : new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
