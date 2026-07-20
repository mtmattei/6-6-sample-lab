using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace A11yCapture.Presentation.Sections;

/// <summary>
/// The IME composition lifecycle the state panel narrates on camera.
/// </summary>
public enum CompositionPhase
{
    Idle,
    Composing,
    Candidate,
    Committed,
}

/// <summary>
/// Observable state surface for the input section. The section's code-behind reads
/// the live TextBox (caret, selection, text) on TextChanging/SelectionChanged and
/// pushes values here; the readout rows bind to them so the invisible IME lifecycle
/// becomes visible to the screen recorder.
/// </summary>
public partial class InputViewModel : ObservableObject
{
    // ---- IME zone --------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseLabel))]
    private CompositionPhase phase = CompositionPhase.Idle;

    /// <summary>The in-flight pre-edit string (best-effort on the Skia head).</summary>
    [ObservableProperty]
    private string compositionText = string.Empty;

    /// <summary>What has actually landed in the field.</summary>
    [ObservableProperty]
    private string committedText = string.Empty;

    /// <summary>Candidates currently offered (best-effort; the OS window is the guaranteed visual).</summary>
    public ObservableCollection<string> Candidates { get; } = new();

    [ObservableProperty]
    private string activeImeCaption = "—";

    public string PhaseLabel => Phase switch
    {
        CompositionPhase.Composing => "composing",
        CompositionPhase.Candidate => "candidate",
        CompositionPhase.Committed => "committed",
        _ => "idle",
    };

    // ---- Unicode zone ----------------------------------------------------

    [ObservableProperty]
    private int caretIndex;

    [ObservableProperty]
    private int selectionStart;

    [ObservableProperty]
    private int selectionLength;

    /// <summary>The grapheme cluster under the caret, to demonstrate cluster-aware stepping.</summary>
    [ObservableProperty]
    private string activeGraphemeCluster = string.Empty;

    /// <summary>Hex code points of the active cluster, e.g. "U+0928 U+093E" for a Devanagari cluster.</summary>
    [ObservableProperty]
    private string activeClusterCodePoints = string.Empty;

    // ---- Event sinks (called from section code-behind) -------------------

    /// <summary>
    /// Drives the composition phase from the IME field's TextChanging.
    /// <paramref name="isContentChanging"/> mirrors TextBoxTextChangingEventArgs.IsContentChanging:
    /// true while the user/IME is actively mutating content (composing/committing), false on
    /// programmatic/no-op changes.
    /// </summary>
    public void OnImeTextChanged(string text, bool isContentChanging)
    {
        CommittedText = text;

        if (string.IsNullOrEmpty(text))
        {
            Phase = CompositionPhase.Idle;
            CompositionText = string.Empty;
            return;
        }

        // Heuristic: active content mutation reads as a commit on the Skia head, where the
        // pre-edit string isn't reliably surfaced. The OS candidate window covers the
        // composing/candidate visual; the recording runbook tunes the live phase narration.
        Phase = isContentChanging ? CompositionPhase.Committed : CompositionPhase.Idle;
    }

    /// <summary>Resets the IME zone to its at-rest state (field focus lost / cleared).</summary>
    public void ResetImeState()
    {
        Phase = CompositionPhase.Idle;
        CompositionText = string.Empty;
        Candidates.Clear();
    }

    /// <summary>
    /// Drives the Unicode-zone readout from a TextBox's caret/selection. Computes the
    /// active grapheme cluster using StringInfo so stepping is cluster-aware, not code-unit-aware.
    /// </summary>
    public void OnSelectionChanged(string text, int selStart, int selLength)
    {
        SelectionStart = selStart;
        SelectionLength = selLength;
        CaretIndex = selStart + selLength;

        var cluster = GraphemeClusterAt(text, CaretIndex);
        ActiveGraphemeCluster = cluster;
        ActiveClusterCodePoints = DescribeCodePoints(cluster);
    }

    // ---- Helpers ---------------------------------------------------------

    /// <summary>
    /// Returns the grapheme cluster that contains (or immediately precedes) the caret.
    /// Caret at the end of the string returns the last cluster.
    /// </summary>
    private static string GraphemeClusterAt(string text, int caret)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        caret = Math.Clamp(caret, 0, text.Length);

        var en = StringInfo.GetTextElementEnumerator(text);
        var last = string.Empty;
        while (en.MoveNext())
        {
            var element = (string)en.Current;
            var start = en.ElementIndex;
            var end = start + element.Length;

            // Caret sitting inside or at the trailing edge of this cluster.
            if (caret > start && caret <= end)
            {
                return element;
            }

            // Caret exactly at a cluster start: the cluster to the right is "active".
            if (caret == start)
            {
                return element;
            }

            last = element;
        }

        return last;
    }

    private static string DescribeCodePoints(string cluster)
    {
        if (string.IsNullOrEmpty(cluster))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var i = 0;
        while (i < cluster.Length)
        {
            var cp = char.ConvertToUtf32(cluster, i);
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append("U+").Append(cp.ToString("X4", CultureInfo.InvariantCulture));
            i += char.IsSurrogatePair(cluster, i) ? 2 : 1;
        }

        return sb.ToString();
    }
}
