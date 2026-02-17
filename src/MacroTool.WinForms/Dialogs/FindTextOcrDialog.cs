using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MacroTool.Domain.Macros;

// WinForms の Control.MousePosition（Point）と、ドメイン enum の MousePosition が衝突するため別名を付ける
using DomainMousePosition = MacroTool.Domain.Macros.MousePosition;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// Find text (OCR)（2-7-2）設定ダイアログ。
/// 
/// VS の WinForms デザイナで表示できるよう、UI は .Designer.cs 側の InitializeComponent() に配置。
/// 実処理（Define/Confirm/Test/OK）は本ファイルに集約。
/// </summary>
public partial class FindTextOcrDialog : Form
{
    private SearchArea _area = new() { Kind = SearchAreaKind.EntireDesktop };
    private Rectangle _definedScreenRect = Rectangle.Empty;

    // Test 実行中ガード
    private CancellationTokenSource? _testCts;
    private bool _testing;
    private bool _savedControlBox;

    public FindTextOcrAction Result { get; private set; } = new();

    public static FindTextOcrAction? Show(IWin32Window owner, FindTextOcrAction? initial)
    {
        using var dlg = new FindTextOcrDialog(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
    }

    /// <summary>
    /// デザイナ用（VS で [デザイン] を開くため）。
    /// </summary>
    public FindTextOcrDialog()
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

    private FindTextOcrDialog(FindTextOcrAction? initial)
    {
        InitializeComponent();
        InitializeRuntime(initial);
    }

    partial void DisposeManagedResources()
    {
        // Test 実行中なら止める
        try { _testCts?.Cancel(); } catch { /* ignore */ }
        _testCts?.Dispose();
        _testCts = null;
    }

    private void InitializeRuntime(FindTextOcrAction? initial)
    {
        // 破棄中に Test が帰ってくるのを止める
        FormClosing += (_, __) => _testCts?.Cancel();

        var init = initial ?? CreateDefault();

        _area = init.SearchArea ?? init.Area ?? new SearchArea { Kind = SearchAreaKind.EntireDesktop };
        _definedScreenRect = Rectangle.Empty;

        _txtText.Text = init.TextToSearchFor ?? string.Empty;
        _cmbLang.SelectedItem = init.Language.ToString();
        _cmbArea.SelectedItem = ToAreaText(_area);

        _chkMouseAction.Checked = init.MouseActionEnabled;
        _cmbMouseAction.SelectedItem = init.MouseAction.ToString();
        _cmbMousePos.SelectedItem = ToMousePosText(init.MousePosition);

        _chkSaveCoord.Checked = init.SaveCoordinateEnabled;
        _txtSaveX.Text = init.SaveXVariable ?? "X";
        _txtSaveY.Text = init.SaveYVariable ?? "Y";

        SetGoToSelection(_cmbTrueGoTo, init.TrueGoTo);
        SetGoToSelection(_cmbFalseGoTo, init.FalseGoTo);

        _numTimeoutSec.Value = init.TimeoutMs <= 0 ? 0 : Math.Clamp(init.TimeoutMs / 1000, 0, 86400);

        ApplyEnableState();
        UpdateAreaButtons();

        // events
        _chkMouseAction.CheckedChanged += (_, __) => ApplyEnableState();
        _chkSaveCoord.CheckedChanged += (_, __) => ApplyEnableState();
        _cmbArea.SelectedIndexChanged += (_, __) => { OnAreaSelectionChanged(); UpdateAreaButtons(); };

        _cmbTrueGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbTrueGoTo);
        _cmbFalseGoTo.SelectedIndexChanged += (_, __) => OnGoToSelected(_cmbFalseGoTo);

        _btnDefineArea.Click += (_, __) => DefineArea();
        _btnConfirmArea.Click += (_, __) => ConfirmArea();
        _btnTest.Click += async (_, __) => await TestAsync();

        _btnOk.Click += (_, __) => Result = BuildResult();
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
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        bool isArea = sel is "Area of desktop" or "Area of focused window";

        _btnDefineArea.Enabled = isArea && !_testing;
        _btnConfirmArea.Enabled = isArea && _definedScreenRect != Rectangle.Empty && !_testing;
    }

    private void OnAreaSelectionChanged()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        _area = sel switch
        {
            "Focused window" => new SearchArea { Kind = SearchAreaKind.FocusedWindow },
            "Area of desktop" => _area.Kind == SearchAreaKind.AreaOfDesktop ? _area : new SearchArea { Kind = SearchAreaKind.AreaOfDesktop },
            "Area of focused window" => _area.Kind == SearchAreaKind.AreaOfFocusedWindow ? _area : new SearchArea { Kind = SearchAreaKind.AreaOfFocusedWindow },
            _ => new SearchArea { Kind = SearchAreaKind.EntireDesktop }
        };

        // 別選択に切り替えたら定義矩形は一旦クリア
        if (sel is not ("Area of desktop" or "Area of focused window"))
            _definedScreenRect = Rectangle.Empty;
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
            _area = new SearchArea { Kind = SearchAreaKind.AreaOfDesktop, X1 = r.Left, Y1 = r.Top, X2 = r.Right, Y2 = r.Bottom };
        }
        else
        {
            // focused window 基準にできるなら相対化
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
                _area = new SearchArea { Kind = SearchAreaKind.AreaOfFocusedWindow, X1 = r.Left, Y1 = r.Top, X2 = r.Right, Y2 = r.Bottom };
            }
        }

        UpdateAreaButtons();
    }

    private void ConfirmArea()
    {
        var sel = _cmbArea.SelectedItem?.ToString() ?? "Entire desktop";
        if (sel is not ("Area of desktop" or "Area of focused window"))
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
        if (string.IsNullOrWhiteSpace(_txtText.Text))
        {
            SafeMessage("Text to search is empty.", MessageBoxIcon.Warning);
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

            Hide();
            await Task.Delay(150, _testCts.Token);
            if (IsDisposed || Disposing) return;

            var rect = (_cmbArea.SelectedItem?.ToString() ?? "") is "Area of desktop" or "Area of focused window"
                ? (_definedScreenRect != Rectangle.Empty ? _definedScreenRect : DetectionTestUtil.ResolveSearchRectangle(_area))
                : DetectionTestUtil.ResolveSearchRectangle(_area);

            var action = BuildResult();
            var testAction = action with
            {
                MouseActionEnabled = false,
                SaveCoordinateEnabled = false
            };

            var (success, pt, _) = await DetectionTestUtil.TestFindTextOcrAsync(testAction, rect, _testCts.Token);
            if (IsDisposed || Disposing) return;

            SafeRestoreVisibility(wasVisible);

            if (success && pt is not null)
                SafeMessage($"Found at ({pt.Value.X}, {pt.Value.Y})", MessageBoxIcon.Information);
            else
                SafeMessage("Not found.", MessageBoxIcon.Information);
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
                ApplyEnableState();
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

        _cmbLang.Enabled = !testing;
        _cmbArea.Enabled = !testing;
        _txtText.Enabled = !testing;
        // 仕様上は存在するが v1.0 では未処理のオプション（UIのみ）
        _chkRegex.Enabled = !testing;
        _chkOptimizeContrast.Enabled = !testing;
        _chkOptimizeShortText.Enabled = !testing;
        _numTimeoutSec.Enabled = !testing;
        _cmbTrueGoTo.Enabled = !testing;
        _cmbFalseGoTo.Enabled = !testing;

        _chkMouseAction.Enabled = !testing;
        _chkSaveCoord.Enabled = !testing;

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

    // --- GoTo selection helpers ---
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

    private static OcrLanguage ParseLanguage(string? text)
        => Enum.TryParse<OcrLanguage>(text, out var v) ? v : OcrLanguage.English;

    private FindTextOcrAction BuildResult()
    {
        return new FindTextOcrAction
        {
            TextToSearchFor = _txtText.Text ?? string.Empty,
            Language = ParseLanguage(_cmbLang.SelectedItem?.ToString()),
            SearchArea = _area,
            Area = _area,

            MouseActionEnabled = _chkMouseAction.Checked,
            MouseAction = Enum.TryParse<MouseActionBehavior>(_cmbMouseAction.SelectedItem?.ToString(), out var ma) ? ma : MouseActionBehavior.Positioning,
            MousePosition = ParseMousePos(_cmbMousePos.SelectedItem?.ToString()),

            SaveCoordinateEnabled = _chkSaveCoord.Checked,
            SaveXVariable = _txtSaveX.Text ?? "X",
            SaveYVariable = _txtSaveY.Text ?? "Y",

            TrueGoTo = ParseGoToText(_cmbTrueGoTo.SelectedItem?.ToString()),
            FalseGoTo = ParseGoToText(_cmbFalseGoTo.SelectedItem?.ToString()),

            TimeoutMs = (int)_numTimeoutSec.Value <= 0 ? 0 : (int)_numTimeoutSec.Value * 1000,
        };
    }

    private static FindTextOcrAction CreateDefault()
        => new()
        {
            TextToSearchFor = string.Empty,
            Language = OcrLanguage.English,
            SearchArea = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
            Area = new SearchArea { Kind = SearchAreaKind.EntireDesktop },
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
