using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace A11yCapture.Presentation.Sections;

public sealed partial class ScrollAnchorSection : UserControl
{
    private static readonly string[] Senders = { "Ava", "Ben", "Cleo", "Dan" };

    private static readonly string[] Bodies =
    {
        "Build is green on desktop and WASM.",
        "Pushed the anchoring fix, give it a try when you get a sec.",
        "The viewport no longer jumps when items come in above the fold.",
        "Reading position stays put even as the feed keeps streaming. Nice.",
        "Logs are flowing in fast now; this is exactly the case we wanted to cover.",
        "Dashboard tiles refresh without yanking me away from what I was looking at.",
        "Timeline scroll feels stable now that CurrentAnchor holds the focused row.",
        "Tested with variable-height bubbles and it still holds the anchor cleanly.",
    };

    private readonly DispatcherTimer _feedTimer = new() { Interval = TimeSpan.FromMilliseconds(800) };
    private int _nextId;
    private ChatMessage? _anchorMessage;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ScrollAnchorSection()
    {
        this.InitializeComponent();

        DataContext = this;

        _feedTimer.Tick += FeedTimer_Tick;
        Loaded += Section_Loaded;
        Unloaded += Section_Unloaded;

        SeedMessages();
    }

    /// <summary>Called by the shell when the user switches away: stop the streaming feed.</summary>
    public void Deactivate()
        => FeedButton.IsChecked = false;

    private void SeedMessages()
    {
        Messages.Clear();

        // Newest at the top, like a feed/timeline. Seed #30 (top) down to #01 (bottom).
        for (var id = 30; id >= 1; id--)
        {
            Messages.Add(BuildMessage(id, isLive: false));
        }

        _nextId = 31;
        UpdateCount();
    }

    private static ChatMessage BuildMessage(int id, bool isLive)
    {
        var sender = Senders[id % Senders.Length];
        var body = Bodies[id % Bodies.Length];
        return new ChatMessage(id, sender, body, isLive);
    }

    private void Section_Loaded(object sender, RoutedEventArgs e)
        => ScrollToTop();

    private void Section_Unloaded(object sender, RoutedEventArgs e)
        => _feedTimer.Stop();

    // Each realized item registers as an anchor candidate when anchoring is on.
    // (Uno 6.6 implements RegisterAnchorCandidate, not the UIElement.CanBeScrollAnchor opt-in.)
    // No candidates registered => the ScrollViewer has nothing to anchor on => content jumps.
    private void Repeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (AnchoringSwitch.IsOn)
        {
            Sv.RegisterAnchorCandidate(args.Element);
        }
    }

    // Recycled elements must stop being candidates or the ScrollViewer holds stale references.
    private void Repeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
        => Sv.UnregisterAnchorCandidate(args.Element);

    private void AnchoringSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        var enabled = AnchoringSwitch.IsOn;

        // The real on/off switch is the anchor ratio, not the candidate list.
        // ItemsRepeater is a virtualizing control: it auto-registers its realized
        // rows as anchor candidates, so unregistering them can't actually stop
        // anchoring — rows realized during an insert just re-opt-in. Setting the
        // ratio to NaN disables the ScrollViewer's anchoring entirely; 0.5 anchors
        // on the candidate nearest the viewport centre.
        Sv.VerticalAnchorRatio = enabled ? 0.5 : double.NaN;
        RatioValue.Text = enabled ? "0.5" : "NaN (off)";

        // Keep the candidate list in sync too, so CurrentAnchor reports cleanly.
        for (var i = 0; i < Messages.Count; i++)
        {
            if (Repeater.TryGetElement(i) is { } element)
            {
                if (enabled)
                {
                    Sv.RegisterAnchorCandidate(element);
                }
                else
                {
                    Sv.UnregisterAnchorCandidate(element);
                }
            }
        }

        if (enabled)
        {
            DispatcherQueue.TryEnqueue(UpdateAnchorHighlight);
        }
        else
        {
            ClearAnchorHighlight();
        }
    }

    private void InsertAbove_Click(object sender, RoutedEventArgs e)
    {
        for (var i = 0; i < 5; i++)
        {
            InsertOneAbove();
        }
    }

    private void Feed_Checked(object sender, RoutedEventArgs e)
        => _feedTimer.Start();

    private void Feed_Unchecked(object sender, RoutedEventArgs e)
        => _feedTimer.Stop();

    private void FeedTimer_Tick(object? sender, object e)
        => InsertOneAbove();

    private void InsertOneAbove()
    {
        Messages.Insert(0, BuildMessage(_nextId++, isLive: true));
        UpdateCount();

        // CurrentAnchor / offset settle after the next arrange pass.
        DispatcherQueue.TryEnqueue(UpdateAnchorHighlight);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        FeedButton.IsChecked = false;
        ClearAnchorHighlight();
        SeedMessages();
        DispatcherQueue.TryEnqueue(ScrollToTop);
    }

    private void Sv_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        => UpdateAnchorHighlight();

    // Start at the top of the feed (newest), the natural "reading the latest" position.
    // It also keeps index-0 inserts inside ItemsRepeater's realized range, so anchoring
    // has a realized row to hold: with anchoring on the view stays put while new items
    // pile up above; with it off the view jumps to the newest. Centred, those inserts
    // land outside the realized range and neither mode moves — no visible contrast.
    private void ScrollToTop()
        => Sv.ChangeView(null, 0, null, disableAnimation: true);

    private void UpdateAnchorHighlight()
    {
        var message = (Sv.CurrentAnchor as FrameworkElement)?.DataContext as ChatMessage;
        if (ReferenceEquals(message, _anchorMessage))
        {
            return;
        }

        if (_anchorMessage is not null)
        {
            _anchorMessage.IsAnchor = false;
        }

        _anchorMessage = message;

        if (message is not null)
        {
            message.IsAnchor = true;
        }

        AnchorRun.Text = message is null ? "—" : message.Label;
    }

    private void ClearAnchorHighlight()
    {
        if (_anchorMessage is not null)
        {
            _anchorMessage.IsAnchor = false;
            _anchorMessage = null;
        }

        AnchorRun.Text = "—";
    }

    private void UpdateCount()
        => CountRun.Text = Messages.Count.ToString();
}
