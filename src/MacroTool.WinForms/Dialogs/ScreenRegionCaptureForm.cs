using System.Drawing.Imaging;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// 画面上の矩形範囲をドラッグで指定してPNGとして返す簡易キャプチャ。
/// </summary>
public sealed class ScreenRegionCaptureForm : Form
{
    private Bitmap? _screen;
    private bool _dragging;
    private Point _start;
    private Point _end;

    public byte[] CapturedPngBytes { get; private set; } = Array.Empty<byte>();

    public ScreenRegionCaptureForm()
    {
        Text = "Capture";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = true;
        ShowInTaskbar = false;
        KeyPreview = true;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        MouseDown += (_, e) =>
        {
            _dragging = true;
            _start = e.Location;
            _end = e.Location;
            Invalidate();
        };
        MouseMove += (_, e) =>
        {
            if (!_dragging) return;
            _end = e.Location;
            Invalidate();
        };
        MouseUp += (_, e) =>
        {
            if (!_dragging) return;
            _dragging = false;
            _end = e.Location;

            var rect = NormalizeRect(_start, _end);
            if (rect.Width < 5 || rect.Height < 5)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (_screen == null)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            using var cropped = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(cropped))
            {
                g.DrawImage(_screen, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }
            using var ms = new MemoryStream();
            cropped.Save(ms, ImageFormat.Png);
            CapturedPngBytes = ms.ToArray();

            DialogResult = DialogResult.OK;
            Close();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        var vs = GetVirtualScreen();
        Location = new Point(vs.Left, vs.Top);
        Size = new Size(vs.Width, vs.Height);

        _screen?.Dispose();
        _screen = new Bitmap(vs.Width, vs.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(_screen);
        g.CopyFromScreen(vs.Left, vs.Top, 0, 0, new Size(vs.Width, vs.Height), CopyPixelOperation.SourceCopy);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_screen != null)
        {
            e.Graphics.DrawImageUnscaled(_screen, 0, 0);
        }

        // overlay
        using var overlay = new SolidBrush(Color.FromArgb(80, Color.Black));
        e.Graphics.FillRectangle(overlay, ClientRectangle);

        if (_dragging)
        {
            var rect = NormalizeRect(_start, _end);
            using var pen = new Pen(Color.Red, 2);
            e.Graphics.DrawRectangle(pen, rect);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _screen?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Rectangle NormalizeRect(Point a, Point b)
    {
        int left = Math.Min(a.X, b.X);
        int top = Math.Min(a.Y, b.Y);
        int right = Math.Max(a.X, b.X);
        int bottom = Math.Max(a.Y, b.Y);
        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static Rectangle GetVirtualScreen()
    {
        // WinFormsが持つVirtualScreen情報を利用（マルチモニタ対応）
        return SystemInformation.VirtualScreen;
    }
}
