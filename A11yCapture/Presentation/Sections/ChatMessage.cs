using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace A11yCapture.Presentation.Sections;

/// <summary>
/// A single chat/feed item. <see cref="IsAnchor"/> is mutable so the section can
/// light up whichever message the ScrollViewer currently reports as its
/// <c>CurrentAnchor</c>; everything else is set once at construction.
/// </summary>
public sealed class ChatMessage : INotifyPropertyChanged
{
    public ChatMessage(int id, string sender, string text, bool isLive = false)
    {
        Id = id;
        Sender = sender;
        Text = text;
        IsLive = isLive;
    }

    public int Id { get; }

    public string Sender { get; }

    public string Text { get; }

    /// <summary>True for messages inserted at runtime (feed/insert), used to badge them as NEW.</summary>
    public bool IsLive { get; }

    public string Label => $"#{Id:00}";

    private bool _isAnchor;

    /// <summary>Set by the section to the message the ScrollViewer is anchoring on.</summary>
    public bool IsAnchor
    {
        get => _isAnchor;
        set
        {
            if (_isAnchor == value)
            {
                return;
            }

            _isAnchor = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
