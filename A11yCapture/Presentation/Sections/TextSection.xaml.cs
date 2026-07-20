using System;
using Microsoft.UI.Xaml.Documents;

namespace A11yCapture.Presentation.Sections;

public sealed partial class TextSection : UserControl
{
    public TextSection()
    {
        this.InitializeComponent();
        Loaded += (_, _) => ApplyHighlights();
    }

    private void OnQueryChanged(object sender, TextChangedEventArgs e)
        => ApplyHighlights();

    /// <summary>
    /// Rebuilds the highlight ranges for the current query. TextHighlighters paint over
    /// character ranges of the existing run, so the text never reflows as matches change.
    /// </summary>
    private void ApplyHighlights()
    {
        HighlightTarget.TextHighlighters.Clear();

        var query = QueryBox.Text;
        var body = HighlightTarget.Text;

        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(body))
        {
            MatchCountValue.Text = "0";
            RangesValue.Text = "—";
            return;
        }

        var highlighter = new TextHighlighter
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LabAccentSoftBrush"],
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LabInkBrush"],
        };

        var count = 0;
        var first = -1;
        var index = body.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            highlighter.Ranges.Add(new TextRange { StartIndex = index, Length = query.Length });
            if (first < 0)
            {
                first = index;
            }

            count++;
            index = body.IndexOf(query, index + query.Length, StringComparison.OrdinalIgnoreCase);
        }

        if (count > 0)
        {
            HighlightTarget.TextHighlighters.Add(highlighter);
        }

        MatchCountValue.Text = count.ToString();
        RangesValue.Text = count == 0
            ? "no match"
            : $"first at {first} · length {query.Length}";
    }
}
