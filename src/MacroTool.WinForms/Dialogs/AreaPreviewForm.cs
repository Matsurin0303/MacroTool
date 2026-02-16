namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// 指定されたスクリーン矩形を、画面上にオーバーレイ表示して確認する。
/// クリックまたは Esc で閉じる。
/// </summary>
public sealed class AreaPreviewForm : Form
{
    private readonly Rectangle _virtual;
    private readonly Rectangle _screenRect;

    public AreaPreviewForm(Rectangle screenRect)
    {
        _virtual = SystemInformation.VirtualScreen;
        _screenRect = screenRect;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        KeyPreview = true;

        Location = new Point(_virtual.Left, _virtual.Top);
        Size = new Size(_virtual.Width, _virtual.Height);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
        };
        MouseDown += (_, __) => Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // 半透明の暗幕
        using var overlay = new SolidBrush(Color.FromArgb(90, Color.Black));
        e.Graphics.FillRectangle(overlay, ClientRectangle);

        // 画面座標 -> クライアント座標
        var r = new Rectangle(
            _screenRect.Left - _virtual.Left,
            _screenRect.Top - _virtual.Top,
            _screenRect.Width,
            _screenRect.Height);

        // ハイライト
        using var fill = new SolidBrush(Color.FromArgb(60, Color.Yellow));
        e.Graphics.FillRectangle(fill, r);

        using var pen = new Pen(Color.Red, 3);
        e.Graphics.DrawRectangle(pen, r);
    }

    public static void ShowPreview(IWin32Window owner, Rectangle screenRect)
    {
        using var f = new AreaPreviewForm(screenRect);
        // モーダルにして、ユーザーの確認が終わるまで待つ
        f.ShowDialog(owner);
    }
}
