using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Media3D;
using Windows.Foundation;

namespace A11yCapture.Presentation.Sections;

public sealed partial class ProjectionSection : UserControl
{
    private const double SweepDegrees = 60.0;       // ±60°, so the card never turns past
    private const double SweepPeriodSeconds = 6.0;  // 90° and read back-to-front

    private DateTimeOffset _spinStart;
    private bool _spinning;

    public ProjectionSection()
    {
        this.InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += (_, _) => StopSpin();

        // The readout has to be right whether or not the card is moving, so it
        // follows layout rather than the animation.
        SizeChanged += (_, _) => UpdateLayoutReadout();
    }

    /// <summary>Called by the shell when the user switches away: drop the per-frame hook.</summary>
    public void Deactivate()
        => SpinToggle.IsChecked = false;

    /// <summary>
    /// Called by the shell on arrival. Loaded only fires once for the section, so
    /// without this the card stays paused from the last Deactivate for the rest of
    /// the run. Honours reduced motion, same as first load.
    /// </summary>
    public void Activate()
    {
        if (new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
        {
            SpinToggle.IsChecked = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        OnAxisChanged(RotX, null!);
        OnMatrixChanged(MatrixAngle, null!);

        if (new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
        {
            StartSpin();
        }
        else
        {
            SpinToggle.IsChecked = false;
            SpinValue.Text = "paused (reduced motion)";
        }

        // ActualWidth is still 0 during Loaded, and the matrix needs the card's
        // centre; both have to wait for arrange.
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLayoutReadout();
            OnMatrixChanged(MatrixAngle, null!);
        });
    }

    private void OnSpinToggled(object sender, RoutedEventArgs e)
    {
        if (SpinToggle.IsChecked == true)
        {
            StartSpin();
        }
        else
        {
            StopSpin();
            SpinValue.Text = $"paused · {PlaneProj.RotationY:0.0}°";
            UpdateLayoutReadout();
        }
    }

    private void StartSpin()
    {
        if (_spinning)
        {
            return;
        }

        // Driving RotationY per frame rather than through a Storyboard: the readout
        // below needs the live angle every frame anyway, so one hook covers both.
        _spinStart = DateTimeOffset.UtcNow;
        CompositionTarget.Rendering += OnRendering;
        _spinning = true;
    }

    private void StopSpin()
    {
        if (!_spinning)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _spinning = false;
    }

    private void OnRendering(object? sender, object e)
    {
        var elapsed = (DateTimeOffset.UtcNow - _spinStart).TotalSeconds;
        var angle = SweepDegrees * Math.Sin(elapsed / SweepPeriodSeconds * 2.0 * Math.PI);

        PlaneProj.RotationY = angle;
        SpinValue.Text = $"{angle:0.0}° · sweep ±60° / 6s";

        UpdateLayoutReadout();
    }

    private void OnAxisChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        AxisProj.RotationX = RotX.Value;
        AxisProj.RotationY = RotY.Value;
        AxisProj.RotationZ = RotZ.Value;

        RotXValue.Text = $"{RotX.Value:0}°";
        RotYValue.Text = $"{RotY.Value:0}°";
        RotZValue.Text = $"{RotZ.Value:0}°";

        AxisSummary.Text = $"X {RotX.Value:0}° · Y {RotY.Value:0}° · Z {RotZ.Value:0}°";
    }

    private void OnMatrixChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var angleRad = MatrixAngle.Value * Math.PI / 180.0;
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);

        // Measured on Skia (Uno 6.6): the M34 perspective term has no effect here —
        // sweeping it end to end changes zero pixels, while PlaneProjection's own
        // perspective does foreshorten. So this stage drives terms that render.
        var m21 = Skew.Value / 100.0;

        var rotate = new Matrix3D(
            cos, 0, -sin, 0,
            m21, 1, 0, 0,
            sin, 0, cos, 0,
            0, 0, 0, 1);

        // Matrix3DProjection has no CenterOfRotation properties: the matrix acts on
        // the element's own origin, so a bare rotation squashes it toward the left
        // edge. Composing translate-to-centre, rotate, translate-back is what
        // PlaneProjection does for you.
        var cx = MatrixCard.ActualWidth / 2.0;
        var cy = MatrixCard.ActualHeight / 2.0;

        MatrixProj.ProjectionMatrix =
            Multiply(Multiply(Translate(-cx, -cy), rotate), Translate(cx, cy));

        MatrixAngleValue.Text = $"{MatrixAngle.Value:0}°";
        SkewValue.Text = m21.ToString("0.00");
        MatrixSummary.Text = $"M11 {cos:0.000} · M13 {-sin:0.000} · M21 {m21:0.00}";
    }

    private static Matrix3D Translate(double x, double y)
        => new(1, 0, 0, 0,
               0, 1, 0, 0,
               0, 0, 1, 0,
               x, y, 0, 1);

    // Row-vector convention: a point is transformed as p * a * b.
    // Row 4 of Matrix3D is exposed as OffsetX/OffsetY/OffsetZ rather than M41..M43.
    private static Matrix3D Multiply(Matrix3D a, Matrix3D b)
        => new(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.OffsetX,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.OffsetY,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.OffsetZ,
            a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.OffsetX,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.OffsetY,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.OffsetZ,
            a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.OffsetX,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.OffsetY,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.OffsetZ,
            a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

            a.OffsetX * b.M11 + a.OffsetY * b.M21 + a.OffsetZ * b.M31 + a.M44 * b.OffsetX,
            a.OffsetX * b.M12 + a.OffsetY * b.M22 + a.OffsetZ * b.M32 + a.M44 * b.OffsetY,
            a.OffsetX * b.M13 + a.OffsetY * b.M23 + a.OffsetZ * b.M33 + a.M44 * b.OffsetZ,
            a.OffsetX * b.M14 + a.OffsetY * b.M24 + a.OffsetZ * b.M34 + a.M44 * b.M44);

    // The point of the sample, read live every frame while the card turns.
    // TransformToVisual walks the render transform chain, so the projection is
    // baked into it and the origin drifts as the card sweeps. The layout slot is
    // what measure/arrange assigned, which a render-time transform cannot touch,
    // so it holds still. Both are measured, neither is asserted.
    private void UpdateLayoutReadout()
    {
        if (PlaneCard.ActualWidth is 0)
        {
            return;
        }

        var visual = PlaneCard.TransformToVisual(PlaneStage).TransformPoint(new Point(0, 0));
        VisualValue.Text = $"origin ({visual.X:0},{visual.Y:0}) · moves";

        // The slot is the cell arrange handed the card; the arranged size is what it
        // took inside that cell. Both are layout results, so both hold.
        var slot = LayoutInformation.GetLayoutSlot(PlaneCard);
        LayoutValue.Text =
            $"slot {slot.Width:0}×{slot.Height:0} @ ({slot.X:0},{slot.Y:0}) · " +
            $"arranged {PlaneCard.ActualWidth:0}×{PlaneCard.ActualHeight:0} · holds";
    }
}
