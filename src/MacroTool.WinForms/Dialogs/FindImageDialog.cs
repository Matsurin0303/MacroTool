using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MacroTool.Domain.Macros;

// WinForms の Control.MousePosition（Point）と、ドメイン enum の MousePosition が衝突するため別名を付ける
using DomainMousePosition = MacroTool.Domain.Macros.MousePosition;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Find image（2-7-1）設定ダイアログ。
/// 
/// VS の WinForms デザイナで表示できるよう、UI は .Designer.cs 側の InitializeComponent() に配置。
/// 実処理（Capture/Open/Clear / Define/Confirm/Test / OK）は本ファイルに集約。
/// </summary>
public partial class FindImageDialog : Form
{
    private SearchArea _area = new() { Kind = SearchAreaKind.EntireDesktop };
    private Rectangle _definedScreenRect = Rectangle.Empty; // Confirm Area 用（画面座標）

    private ImageTemplate _template = new();
    private Image? _preview;

    // Test 実行中ガード
    private CancellationTokenSource? _testCts;
    private bool _testing;
    private bool _savedControlBox;

    public FindImageAction Result { get; private set; } = new();

    public static FindImageAction? Show(IWin32Window owner, FindImageAction? initial)
    {
        using var dlg = new FindImageDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }

    /// <summary>
    /// デザイナ用（VS で [デザイン] を開くため）。
    /// </summary>
    public FindImageDialog()
    {
        InitializeComponent();

        // デザイナではロジック/イベントを極力実行しない（例外やデザイナフリーズ回避）
        if (IsDesignTime())
            return;

        InitializeRuntime(null);
    }

    private static bool IsDesignTime()
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return true;

        // VS の out-of-proc デザイナでは UsageMode が Runtime になるケースがあるため保険
        try
        {
            var pn = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            if (pn.Equals("devenv", StringComparison.OrdinalIgnoreCase))
                return true;
            if (pn.Contains("DesignToolsServer", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private FindImageDialog(FindImageAction? initial)
    {
        InitializeComponent();
        InitializeRuntime(initial);
    }

    partial void DisposeManagedResources()
    {
        try { _testCts?.Cancel(); } catch { /* ignore */ }
        _testCts?.Dispose();
        _testCts = null;

        _preview?.Dispose();
        _preview = null;
    }

    private void InitializeRuntime(FindImageAction? initial)
    {
        // 破棄中に Test が帰ってくるのを止める
        FormClosing += (_, __) => _testCts?.Cancel();

        var init = initial ?? CreateDefault();

        _template = init.Template ?? new ImageTemplate();
        _area = init.SearchArea ?? init.Area ?? new SearchArea { Kind = SearchAreaKind.EntireDesktop };
        _definedScreenRect = Rectangle.Empty;

        _cmbArea.SelectedItem = ToAreaText(_area);
        _numTolerance.Value = Math.Clamp(init.ColorTolerancePercent, 0, 100);

        _chkMouseAction.Checked = init.MouseActionEnabled;
        _cmbMouseAction.SelectedItem = init.MouseAction.ToString();
        _cmbMousePos.SelectedItem = ToMousePosText(init.MousePosition);

        _chkSaveCoord.Checked = init.SaveCoordinateEnabled;
        _txtSaveX.Text = init.SaveXVariable ?? "X";
        _txtSaveY.Text = init.SaveYVariable ?? "Y";

        SetGoToSelection(_cmbTrueGoTo, init.TrueGoTo);
        SetGoToSelection(_cmbFalseGoTo, init.FalseGoTo);

        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

        LoadPreviewFromTemplate();

        ApplyEnableState();
        UpdateAreaButtons();

        // ---- events ----
        _chkMouseAction.CheckedChanged += (_, __) => ApplyEnableState();
        _chkSaveCoord.CheckedChanged += (_, __) => ApplyEnableState();

        _cmbArea.SelectedIndexChanged += (_, __) =>
        {
            OnAreaSelectionChanged();
            UpdateAreaButtons();
        };

        _cmbTrueGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbTrueGoTo);
        _cmbFalseGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbFalseGoTo);

        _btnCapture.Click += (_, __) => CaptureTemplate();
        _btnOpen.Click += (_, __) => OpenTemplate();
        _btnClear.Click += (_, __) => ClearTemplate();

        _btnDefineArea.Click += (_, __) => DefineArea();
        _btnConfirmArea.Click += (_, __) => ConfirmArea();

        _btnTest.Click += async (_, __) => await TestAsync();

        _btnOk.Click += (_, __) => Result = BuildResult();

        // 初期状態の補正
        if (_cmbMouseAction.SelectedIndex < 0) _cmbMouseAction.SelectedIndex = 0;
        if (_cmbMousePos.SelectedIndex < 0) _cmbMousePos.SelectedIndex = 0;
        if (_cmbArea.SelectedIndex < 0) _cmbArea.SelectedIndex = 0;
    }

    private void ApplyEnableState()
    {
        _cmbMouseAction.Enabled = _chkMouseAction.Checked && !_testing;
        _cmbMousePos.Enabled = _chkMouseAction.Checked && !_testing;

        _txtSaveX.Enabled = _chkSaveCoord.Checked && !_testing;
        _txtSaveY.Enabled = _chkSaveCoord.Checked && !_testing;
    }

    private void UpdateAreaButtons()
    {
        bool isArea = IsAreaSelection();

        // Define/Confirm は常に表示し、Area 選択時だけ有効化する。
        _btnDefineArea.Enabled = isArea && !_testing;
        _btnConfirmArea.Enabled = isArea && !_testing && _definedScreenRect != Rectangle.Empty;
    }

    private bool IsAreaSelection()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        return sel is "Area of desktop" or "Area of focused window";
    }

    private void OnAreaSelectionChanged()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        _area = sel switch
        {
            "Focused window" => new SearchArea { Kind = SearchAreaKind.FocusedWindow },
            "Area of desktop" => new SearchArea { Kind = SearchAreaKind.AreaOfDesktop },
            "Area of focused window" => new SearchArea { Kind = SearchAreaKind.AreaOfFocusedWindow },
            _ => new SearchArea { Kind = SearchAreaKind.EntireDesktop }
        };

        if (!IsAreaSelection())
            _definedScreenRect = Rectangle.Empty;
    }

    private void CaptureTemplate()
    {
        using var f = new ScreenRegionCaptureForm();
        if (f.ShowDialog(this) != DialogResult.OK)
            return;

        var bytes = f.CapturedPngBytes;
        if (bytes is null || bytes.Length == 0)
            return;

        _template = new ImageTemplate
        {
            Kind = ImageTemplateKind.EmbeddedPng,
            PngBytes = bytes,
            FilePath = string.Empty
        };

        LoadPreviewFromTemplate();
    }

    private void OpenTemplate()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*",
            Title = "Open image"
        };
        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        _template = new ImageTemplate
        {
            Kind = ImageTemplateKind.FilePath,
            FilePath = ofd.FileName,
            PngBytes = Array.Empty<byte>()
        };

        LoadPreviewFromTemplate();
    }

    private void ClearTemplate()
    {
        _template = new ImageTemplate();
        LoadPreviewFromTemplate();
    }

    private void LoadPreviewFromTemplate()
    {
        _preview?.Dispose();
        _preview = null;

        try
        {
            if (_template.Kind == ImageTemplateKind.EmbeddedPng && _template.PngBytes is { Length: > 0 })
            {
                _preview = Image.FromStream(new MemoryStream(_template.PngBytes));
            }
            else if (_template.Kind == ImageTemplateKind.FilePath && !string.IsNullOrWhiteSpace(_template.FilePath) && File.Exists(_template.FilePath))
            {
                _preview = Image.FromFile(_template.FilePath);
            }
        }
        catch
        {
            _preview?.Dispose();
            _preview = null;
        }

        _picTemplate.Image = _preview;
    }

    private void DefineArea()
    {
        using var f = new ScreenRegionCaptureForm();
        if (f.ShowDialog(this) != DialogResult.OK)
            return;

        var r = f.CapturedScreenRectangle;
        if (r.Width <= 0 || r.Height <= 0)
            return;

        _definedScreenRect = r;

        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        if (sel == "Area of desktop")
        {
            _area = new SearchArea
            {
                Kind = SearchAreaKind.AreaOfDesktop,
                X1 = r.Left,
                Y1 = r.Top,
                X2 = r.Right,
                Y2 = r.Bottom
            };
        }
        else
        {
            // focused window 基準に相対化できれば相対化
            var center = new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
            if (TryGetWindowRectFromPoint(center, out var win))
            {
                _area = new SearchArea
                {
                    Kind = SearchAreaKind.AreaOfFocusedWindow,
                    X1 = r.Left - win.Left,
                    Y1 = r.Top - win.Top,
                    X2 = r.Right - win.Left,
                    Y2 = r.Bottom - win.Top
                };
            }
            else
            {
                _area = new SearchArea
                {
                    Kind = SearchAreaKind.AreaOfFocusedWindow,
                    X1 = r.Left,
                    Y1 = r.Top,
                    X2 = r.Right,
                    Y2 = r.Bottom
                };
            }
        }

        UpdateAreaButtons();
    }

    private void ConfirmArea()
    {
        if (!IsAreaSelection())
            return;

        var rect = _definedScreenRect != Rectangle.Empty
            ? _definedScreenRect
            : DetectionTestUtil.ResolveSearchRectangle(_area);

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var wasVisible = Visible;
        try
        {
            Hide();
            AreaPreviewForm.ShowPreview(this, rect);
        }
        finally
        {
            SafeRestoreVisibility(wasVisible);
        }
    }

    private async Task TestAsync()
    {
        if (_testing) return;

        bool hasTemplate = _template.Kind switch
        {
            ImageTemplateKind.EmbeddedPng => _template.PngBytes is { Length: > 0 },
            ImageTemplateKind.FilePath => !string.IsNullOrWhiteSpace(_template.FilePath),
            _ => false
        };

        if (!hasTemplate)
        {
            SafeMessage("Template image is not set.", MessageBoxIcon.Warning);
            return;
        }

        _testing = true;
        _testCts?.Cancel();
        _testCts?.Dispose();
        _testCts = new CancellationTokenSource();

        SetTestingUi(true);

        var wasVisible = Visible;
        try
        {
            UseWaitCursor = true;

            // 自分が写り込むのを避ける
            Hide();
            await Task.Delay(150, _testCts.Token);

            if (IsDisposed || Disposing) return;

            var rect = IsAreaSelection()
                ? (_definedScreenRect != Rectangle.Empty ? _definedScreenRect : DetectionTestUtil.ResolveSearchRectangle(_area))
                : DetectionTestUtil.ResolveSearchRectangle(_area);

            var action = BuildResult();
            var testAction = action with
            {
                MouseActionEnabled = false,
                SaveCoordinateEnabled = false
            };

            var (success, pt, _) = await DetectionTestUtil.TestFindImageAsync(testAction, rect, _testCts.Token);

            if (IsDisposed || Disposing) return;

            SafeRestoreVisibility(wasVisible);

            SafeMessage(success && pt is not null
                    ? $"Found at ({pt.Value.X}, {pt.Value.Y})."
                    : "Not found.",
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            // closing / canceled
        }
        catch (Exception ex)
        {
            if (IsDisposed || Disposing) return;
            SafeRestoreVisibility(wasVisible);
            SafeMessage(ex.Message, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed && !Disposing)
            {
                UseWaitCursor = false;
                SetTestingUi(false);
                _testing = false;
                UpdateAreaButtons();
            }
        }
    }

    private void SetTestingUi(bool testing)
    {
        if (!testing)
        {
            ControlBox = _savedControlBox;
        }
        else
        {
            _savedControlBox = ControlBox;
            ControlBox = false;
        }

        _btnOk.Enabled = !testing;
        _btnCancel.Enabled = !testing;

        _btnTest.Enabled = !testing;

        _btnCapture.Enabled = !testing;
        _btnOpen.Enabled = !testing;
        _btnClear.Enabled = !testing;

        _cmbArea.Enabled = !testing;
        _numTolerance.Enabled = !testing;

        _chkMouseAction.Enabled = !testing;
        _cmbMouseAction.Enabled = !testing && _chkMouseAction.Checked;
        _cmbMousePos.Enabled = !testing && _chkMouseAction.Checked;

        _chkSaveCoord.Enabled = !testing;
        _txtSaveX.Enabled = !testing && _chkSaveCoord.Checked;
        _txtSaveY.Enabled = !testing && _chkSaveCoord.Checked;

        _cmbTrueGoTo.Enabled = !testing;

        _numTimeoutSec.Enabled = !testing;
        _cmbFalseGoTo.Enabled = !testing;

        UpdateAreaButtons();
    }

    private void SafeRestoreVisibility(bool wasVisible)
    {
        if (!wasVisible) return;
        if (IsDisposed || Disposing) return;

        if (!Visible) Show();
        Activate();
    }

    private void SafeMessage(string msg, MessageBoxIcon icon)
    {
        if (IsDisposed || Disposing) return;

        if (IsHandleCreated)
            MessageBox.Show(this, msg, "Test", MessageBoxButtons.OK, icon);
        else
            MessageBox.Show(msg, "Test", MessageBoxButtons.OK, icon);
    }

    // --- result mapping ---
    private FindImageAction BuildResult()
    {
        return new FindImageAction
        {
            Template = _template,

            SearchArea = _area,
            Area = _area,

            ColorTolerancePercent = (int)_numTolerance.Value,

            MouseActionEnabled = _chkMouseAction.Checked,
            MouseAction = ParseMouseAction(_cmbMouseAction.SelectedItem?.ToString()),
            MousePosition = ParseMousePos(_cmbMousePos.SelectedItem?.ToString()),

            SaveCoordinateEnabled = _chkSaveCoord.Checked,
            SaveXVariable = _txtSaveX.Text,
            SaveYVariable = _txtSaveY.Text,

            TrueGoTo = ParseGoToText(_cmbTrueGoTo.SelectedItem?.ToString()),

            TimeoutMs = (int)_numTimeoutSec.Value <= 0 ? 0 : (int)_numTimeoutSec.Value * 1000,
            FalseGoTo = ParseGoToText(_cmbFalseGoTo.SelectedItem?.ToString()),
        };
    }

    private static FindImageAction CreateDefault()
        => new()
        {
            Template = new ImageTemplate(),
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            ColorTolerancePercent = 0,

            MouseActionEnabled = true,
            MouseAction = MouseActionBehavior.Positioning,
            MousePosition = DomainMousePosition.Center,

            SaveCoordinateEnabled = false,
            SaveXVariable = "X",
            SaveYVariable = "Y",

            TrueGoTo = GoToTarget.Next(),
            FalseGoTo = GoToTarget.End(),

            TimeoutMs = 120000
        };

    private void OnGoToSelected(ComboBox cmb)
    {
        if (cmb.SelectedItem?.ToString() != "Label...")
            return;

        var label = SimpleTextPrompt.Show(this, title: "Go to label", message: "Enter label:");
        if (string.IsNullOrWhiteSpace(label))
        {
            cmb.SelectedItem = "Next";
            return;
        }

        var text = $"Label:{label}";
        if (!cmb.Items.Contains(text))
            cmb.Items.Insert(2, text);

        cmb.SelectedItem = text;
    }

    private static void SetGoToSelection(ComboBox cmb, GoToTarget target)
    {
        var text = ToGoToText(target);
        if (!cmb.Items.Contains(text) && text.StartsWith("Label:", StringComparison.Ordinal))
            cmb.Items.Insert(2, text);
        cmb.SelectedItem = text;
    }

    private static string ToGoToText(GoToTarget t)
        => t.Kind switch
        {
            GoToKind.End => "End",
            GoToKind.Label => $"Label:{t.Label}",
            _ => "Next"
        };

    private static GoToTarget ParseGoToText(string? text)
    {
        text ??= "Next";
        if (text == "End") return GoToTarget.End();
        if (text.StartsWith("Label:", StringComparison.Ordinal))
            return GoToTarget.ToLabel(text["Label:".Length..]);
        return GoToTarget.Next();
    }

    private static string ToAreaText(SearchArea a)
        => a.Kind switch
        {
            SearchAreaKind.FocusedWindow => "Focused window",
            SearchAreaKind.AreaOfDesktop => "Area of desktop",
            SearchAreaKind.AreaOfFocusedWindow => "Area of focused window",
            _ => "Entire desktop"
        };

    private static MouseActionBehavior ParseMouseAction(string? text)
        => text switch
        {
            nameof(MouseActionBehavior.LeftClick) => MouseActionBehavior.LeftClick,
            nameof(MouseActionBehavior.RightClick) => MouseActionBehavior.RightClick,
            nameof(MouseActionBehavior.MiddleClick) => MouseActionBehavior.MiddleClick,
            nameof(MouseActionBehavior.DoubleClick) => MouseActionBehavior.DoubleClick,
            _ => MouseActionBehavior.Positioning
        };

    private static string ToMousePosText(DomainMousePosition pos)
        => pos switch
        {
            DomainMousePosition.TopLeft => "Top-left",
            DomainMousePosition.TopRight => "Top-right",
            DomainMousePosition.BottomLeft => "Bottom-left",
            DomainMousePosition.BottomRight => "Bottom-right",
            _ => "Centered"
        };

    private static DomainMousePosition ParseMousePos(string? text)
        => text switch
        {
            "Top-left" => DomainMousePosition.TopLeft,
            "Top-right" => DomainMousePosition.TopRight,
            "Bottom-left" => DomainMousePosition.BottomLeft,
            "Bottom-right" => DomainMousePosition.BottomRight,
            _ => DomainMousePosition.Center
        };

    // --- P/Invoke: focused window rect ---
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point pt);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    private static bool TryGetWindowRectFromPoint(Point pt, out Rectangle rect)
    {
        rect = default;
        var h = WindowFromPoint(pt);
        if (h == IntPtr.Zero) return false;
        if (!GetWindowRect(h, out var r)) return false;
        rect = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        return rect.Width > 0 && rect.Height > 0;
    }
}
