namespace A11yCapture.Presentation;

/// <summary>
/// Curated source excerpts for one demo section, shown in the source panel.
/// Excerpts are trimmed by hand from the real section files — keep them in
/// sync when a section's mechanism changes.
/// </summary>
public sealed record SampleSource(string Xaml, string CSharp, string Notes, string Tip);

public static class SampleSources
{
    public static IReadOnlyDictionary<string, SampleSource> BySlug { get; } =
        new Dictionary<string, SampleSource>
        {
            ["accessibility"] = new(
                Xaml: """
                <!-- Name, role, value and state come from
                     the control's automation peer. -->
                <TextBox AutomationProperties.Name="Customer name"
                         PlaceholderText="Contoso" />

                <ToggleSwitch AutomationProperties.Name="Wi-Fi" />

                <Slider AutomationProperties.Name="Volume"
                        Minimum="0" Maximum="100"
                        Value="50" StepFrequency="5" />

                <ListView AutomationProperties.Name="Customers"
                          SelectionMode="Single">
                  <x:String>Contoso</x:String>
                  <x:String>Fabrikam</x:String>
                </ListView>

                <!-- Landmarks and headings for rotor nav -->
                <TextBlock Text="Accessibility"
                    AutomationProperties.HeadingLevel="Level1" />
                <Border
                    uut:AutomationPropertiesExtensions.Role="tablist" />
                """,
                CSharp: """
                namespace A11yCapture.Presentation.Sections;

                // The whole automation surface is declared in
                // XAML. Each control's built-in AutomationPeer
                // projects name, role, value and state into
                // UIA on Windows and ARIA on WebAssembly —
                // no code required.
                public sealed partial class AccessibilitySection
                    : UserControl
                {
                    public AccessibilitySection()
                    {
                        this.InitializeComponent();
                    }
                }
                """,
                Notes: """
                The same automation-peer tree backs every target: UIA on Windows, ARIA in the browser.

                Standard controls announce themselves once AutomationProperties.Name is set; nothing else is wired up.

                The segmented tabs are plain Borders, which have no peer of their own, so the Role attached property fully defines what the screen reader hears.
                """,
                Tip: "Run Narrator (Win+Ctrl+Enter) and Tab through the form: each stop reads name, role, value, state."),

            ["scroll-anchoring"] = new(
                Xaml: """
                <ScrollViewer x:Name="Sv"
                              VerticalAnchorRatio="0.5"
                              ViewChanged="Sv_ViewChanged">
                  <ItemsRepeater x:Name="Repeater"
                      ItemsSource="{Binding Messages}"
                      ElementPrepared="Repeater_ElementPrepared"
                      ElementClearing="Repeater_ElementClearing">
                    <ItemsRepeater.Layout>
                      <StackLayout Spacing="8" />
                    </ItemsRepeater.Layout>
                  </ItemsRepeater>
                </ScrollViewer>
                """,
                CSharp: """
                // Each realized row registers as an anchor
                // candidate. (Uno 6.6 implements
                // RegisterAnchorCandidate.)
                private void Repeater_ElementPrepared(
                    ItemsRepeater sender,
                    ItemsRepeaterElementPreparedEventArgs args)
                {
                    if (AnchoringSwitch.IsOn)
                    {
                        Sv.RegisterAnchorCandidate(args.Element);
                    }
                }

                // NaN disables anchoring entirely; 0.5 anchors
                // on the candidate nearest the viewport centre.
                Sv.VerticalAnchorRatio =
                    enabled ? 0.5 : double.NaN;
                """,
                Notes: """
                With anchoring on, the ScrollViewer tracks the registered candidate nearest VerticalAnchorRatio and compensates the vertical offset whenever content lands above it.

                With the ratio set to NaN there is nothing to anchor on, so every insert above the fold shoves the viewport.

                The accent outline marks the row currently reported by CurrentAnchor.
                """,
                Tip: "Turn on Simulate feed, scroll mid-list, then flip anchoring off to feel the jumps."),

            ["input-ime"] = new(
                Xaml: """
                <!-- Composed input lands in a plain TextBox;
                     nothing to enable. -->
                <TextBox x:Name="ImeBox"
                         AutomationProperties.Name="Customer name"
                         PlaceholderText="Enter customer name" />

                <TextBox AutomationProperties.Name="Arabic"
                         FlowDirection="RightToLeft"
                         Text="مرحبا بالعالم" />

                <TextBox AutomationProperties.Name="Devanagari"
                         Text="नमस्ते दुनिया" />

                <TextBox AutomationProperties.Name="Thai"
                         Text="สวัสดีชาวโลก" />
                """,
                CSharp: """
                // The section reads the live TextBox on
                // TextChanging / SelectionChanged and pushes
                // values to the readout rows.
                public void OnSelectionChanged(
                    string text, int selStart, int selLength)
                {
                    SelectionStart = selStart;
                    SelectionLength = selLength;

                    // Grapheme clusters via StringInfo, so
                    // caret stepping is cluster-aware, not
                    // code-unit-aware.
                }
                """,
                Notes: """
                The IME lifecycle (composing, candidate, committed) is driven by the OS; the readouts narrate what reaches the TextBox.

                The RTL, Devanagari and Thai boxes exercise bidi layout, combining marks, and word segmentation without spaces — all resolved by the same Skia text stack on every target.
                """,
                Tip: "Switch to a Japanese or Chinese IME and type in the top box to watch the phase ramp move."),

            ["text-features"] = new(
                Xaml: """
                <!-- Spell check -->
                <TextBox IsSpellCheckEnabled="True"
                         AcceptsReturn="True"
                         Text="The quik brown fox..." />

                <!-- Trimming: same string, same box -->
                <TextBlock TextTrimming="CharacterEllipsis"
                           TextWrapping="NoWrap" />
                <TextBlock TextTrimming="WordEllipsis"
                           TextWrapping="NoWrap" />
                """,
                CSharp: """
                // TextHighlighters paint character ranges over
                // the existing run — the text never reflows.
                HighlightTarget.TextHighlighters.Clear();

                var highlighter = new TextHighlighter
                {
                    Background = accentSoftBrush,
                    Foreground = inkBrush,
                };

                var at = body.IndexOf(query, comparison);
                while (at >= 0)
                {
                    highlighter.Ranges.Add(
                        new TextRange(at, query.Length));
                    at = body.IndexOf(
                        query, at + 1, comparison);
                }

                HighlightTarget.TextHighlighters.Add(
                    highlighter);
                """,
                Notes: """
                Highlighting is a paint pass, so match churn costs no layout: the run is never split into extra inlines.

                Trimming resolves at the glyph the line actually ends on, which is why CharacterEllipsis and WordEllipsis cut the same string differently.

                Font fallback picks a capable font per Unicode run, keeping mixed-script text readable.
                """,
                Tip: "Type in the query box — matches repaint live while the paragraph stays perfectly still."),

            ["element-theming"] = new(
                Xaml: """
                <!-- RequestedTheme applies to a subtree -->
                <Border RequestedTheme="Light"
                        Background="{ThemeResource SurfaceBrush}"
                        BorderBrush="{ThemeResource OutlineBrush}">
                  ...
                </Border>

                <Border RequestedTheme="Dark"
                        Background="{ThemeResource SurfaceBrush}"
                        BorderBrush="{ThemeResource OutlineBrush}">
                  ...
                </Border>

                <!-- Islands nest: a dark card can hold a
                     light one, and so on down. -->
                <Border x:Name="NestedHost">
                  <Border x:Name="NestedIsland"
                          RequestedTheme="Dark" />
                </Border>
                """,
                CSharp: """
                public ElementThemeSection()
                {
                    this.InitializeComponent();
                    Loaded += (_, _) => UpdateReadouts();
                    ActualThemeChanged +=
                        (_, _) => UpdateReadouts();
                }

                private void UpdateReadouts()
                {
                    // Keep the nested island opposite to
                    // whatever its host resolves to.
                    var opposite =
                        ActualTheme == ElementTheme.Dark
                            ? ElementTheme.Light
                            : ElementTheme.Dark;
                    NestedIsland.RequestedTheme = opposite;
                }
                """,
                Notes: """
                RequestedTheme is a request; ActualTheme is what the element resolved after walking its ancestors.

                ThemeResource lookups re-evaluate per island, so the same SurfaceBrush key yields different brushes side by side.

                ActualThemeChanged fires when an ancestor flips, which is what keeps the matryoshka card in opposition.
                """,
                Tip: "Flip the page theme and watch the readouts: RequestedTheme stays put, ActualTheme follows the ancestors."),

            ["vector-graphics"] = new(
                Xaml: """
                <!-- Caps read best at heavy stroke widths -->
                <Line StrokeThickness="16"
                      StrokeStartLineCap="Round"
                      StrokeEndLineCap="Round"
                      X1="8" Y1="12" X2="330" Y2="12" />

                <Line StrokeThickness="16"
                      StrokeStartLineCap="Triangle"
                      StrokeEndLineCap="Triangle"
                      X1="8" Y1="12" X2="330" Y2="12" />

                <!-- Dash pattern is in stroke-width units -->
                <Line StrokeThickness="7"
                      StrokeDashArray="4 2"
                      StrokeDashCap="Flat"
                      X1="8" Y1="8" X2="330" Y2="8" />
                """,
                CSharp: """
                // A Transform applied directly to a Geometry,
                // spun by animating Angle. Angle is not an
                // independently-animatable property, so this
                // needs EnableDependentAnimation to run.
                var anim = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = new Duration(
                        TimeSpan.FromSeconds(4)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    EnableDependentAnimation = true,
                };

                Storyboard.SetTarget(anim, SpinTransform);
                Storyboard.SetTargetProperty(anim, "Angle");
                """,
                Notes: """
                Uno 6.6's Skia renderer honours the full stroke vocabulary: Flat, Round, Square and Triangle caps, dash arrays with their own dash cap, and Miter, Bevel and Round joins.

                Geometry.Transform rotates the path itself — the element's layout slot is untouched, so nothing around it re-arranges.
                """,
                Tip: "Compare FLAT and SQUARE: both are square-ended, but SQUARE extends the line by half its width."),

            ["projection"] = new(
                Xaml: """
                <!-- The dashed guide marks the layout rect;
                     the card turns, the rect never moves. -->
                <Border x:Name="PlaneCard"
                        Width="240" Height="140">
                  <Border.Projection>
                    <PlaneProjection x:Name="PlaneProj" />
                  </Border.Projection>
                </Border>

                <Slider x:Name="RotX"
                        Minimum="-60" Maximum="60"
                        ValueChanged="OnAxisChanged" />
                """,
                CSharp: """
                // RotationY is driven per frame instead of by
                // a Storyboard: the angle readout needs the
                // live value every frame anyway.
                CompositionTarget.Rendering += OnRendering;

                private void OnAxisChanged(object sender,
                    RangeBaseValueChangedEventArgs e)
                {
                    AxisProj.RotationX = RotX.Value;
                    AxisProj.RotationY = RotY.Value;
                    AxisProj.RotationZ = RotZ.Value;
                }
                """,
                Notes: """
                Uno 6.6 implements PlaneProjection and Matrix3DProjection on Skia.

                A projection is a render-time transform: the element is measured and arranged flat, then turned in 3D when drawn. Hit-testing and layout keep using the flat rect.

                The CompositionTarget.Rendering hook is released whenever the section deactivates, so a hidden card costs nothing.
                """,
                Tip: "Watch the dashed guide while the card spins — the layout slot never moves."),

            ["menus-context"] = new(
                Xaml: """
                <Button Content="File"
                        Style="{StaticResource LabMenuButton}">
                  <Button.Flyout>
                    <MenuFlyout
                        Placement="BottomEdgeAlignedLeft">
                      <MenuFlyoutSubItem Text="New">
                        <MenuFlyoutItem Text="Project"
                            Click="OnMenuItemClick">
                          <MenuFlyoutItem.KeyboardAccelerators>
                            <KeyboardAccelerator
                                Key="N" Modifiers="Control" />
                          </MenuFlyoutItem.KeyboardAccelerators>
                        </MenuFlyoutItem>
                        <MenuFlyoutSubItem Text="From template">
                          <MenuFlyoutItem Text="Blank app" />
                        </MenuFlyoutSubItem>
                      </MenuFlyoutSubItem>
                      <MenuFlyoutSeparator />
                      <ToggleMenuFlyoutItem Text="Word wrap"
                          IsChecked="True" />
                    </MenuFlyout>
                  </Button.Flyout>
                </Button>
                """,
                CSharp: """
                // TryGetPosition returns false for keyboard
                // invocation (Shift+F10) — that distinction is
                // the point of the demo.
                private void OnContextRequested(
                    UIElement sender,
                    ContextRequestedEventArgs args)
                {
                    if (args.TryGetPosition(
                        sender, out var position))
                    {
                        LogEvent($"pointer at ({position.X:F0},"
                            + $" {position.Y:F0})");
                    }
                    else
                    {
                        LogEvent("keyboard (no position)");
                    }
                }
                """,
                Notes: """
                Nested MenuFlyoutSubItems, separators, toggle items and accelerator hint text all render inside the flyout on Skia.

                KeyboardAccelerators fire while the menu is closed, which is what makes the hint text honest.

                The bar itself is lab-styled Buttons hosting the flyouts; the flyout content is the capability on show.
                """,
                Tip: "Open File with the pointer, then press Ctrl+N with the menu closed — same command, two routes."),
        };
}
