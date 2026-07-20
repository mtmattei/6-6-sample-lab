namespace A11yCapture.Presentation.Sections;

public sealed partial class InputSection : UserControl
{
    private readonly InputViewModel _viewModel = new();

    public InputSection()
    {
        this.InitializeComponent();

        DataContext = _viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            // Pulse the state chip on each phase change so the lifecycle reads on camera.
            if (e.PropertyName == nameof(InputViewModel.Phase))
            {
                PhasePulse.Begin();
            }
        };

        // IME zone: observe content mutations (commit signal on the Skia head).
        ImeBox.TextChanging += OnImeTextChanging;
        ImeBox.GotFocus += OnImeGotFocus;

        // Unicode zone: observe caret/selection so the panel shows cluster-aware stepping.
        foreach (var box in new[] { ArabicBox, DevanagariBox, ThaiBox })
        {
            box.SelectionChanged += OnUnicodeSelectionChanged;
            box.GotFocus += OnUnicodeSelectionChanged;
        }
    }

    private void OnImeTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        => _viewModel.OnImeTextChanged(sender.Text, args.IsContentChanging);

    private void OnImeGotFocus(object sender, RoutedEventArgs e)
        => _viewModel.ActiveImeCaption =
            "The active Windows IME drives this field. Switch to Microsoft Pinyin, Japanese, or Korean.";

    private void OnUnicodeSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box)
        {
            _viewModel.OnSelectionChanged(box.Text, box.SelectionStart, box.SelectionLength);
        }
    }
}
