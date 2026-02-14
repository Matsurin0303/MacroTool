using MacroTool.Domain.Macros;

namespace MacroTool.WinForms.Editors;

internal sealed class GoToTargetEditForm : Form
{
    private readonly GoToTarget _target;
    private readonly ComboBox _cmb;
    private readonly Button _ok;
    private readonly Button _cancel;

    private const string ITEM_START = "Start";
    private const string ITEM_NEXT  = "Next";
    private const string ITEM_END   = "End";

    public GoToTargetEditForm(GoToTarget target, IReadOnlyList<string> labels)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));

        Text = "GoTo";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Width = 420;
        Height = 160;

        var lbl = new Label
        {
            Text = "Target",
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _cmb = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var items = BuildItems(_target, labels);
        _cmb.Items.AddRange(items.ToArray());
        _cmb.SelectedItem = GetInitialSelection(_target, items);
        if (_cmb.SelectedIndex < 0 && _cmb.Items.Count > 0) _cmb.SelectedIndex = 0;

        _ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90, Height = 28 };
        _cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 28 };

        _ok.Click += (_, __) => ApplySelection();

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(12),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        grid.Controls.Add(lbl, 0, 0);
        grid.Controls.Add(_cmb, 1, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12),
            Height = 52
        };
        buttons.Controls.Add(_ok);
        buttons.Controls.Add(_cancel);

        Controls.Add(grid);
        Controls.Add(buttons);

        AcceptButton = _ok;
        CancelButton = _cancel;
    }

    private static List<string> BuildItems(GoToTarget current, IReadOnlyList<string> labels)
    {
        var items = new List<string> { ITEM_START, ITEM_NEXT, ITEM_END };

        // ラベル一覧（空白除外・重複除外・登場順維持）
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var l in labels ?? Array.Empty<string>())
        {
            var s = (l ?? string.Empty).Trim();
            if (s.Length == 0) continue;
            if (seen.Add(s)) items.Add(s);
        }

        // 現在値が Label なのに一覧に無い場合も、編集不能にならないよう追加
        if (current.Kind == GoToKind.Label)
        {
            var cur = (current.Label ?? string.Empty).Trim();
            if (cur.Length > 0 && !items.Contains(cur, StringComparer.Ordinal))
                items.Add(cur);
        }

        return items;
    }

    private static string? GetInitialSelection(GoToTarget current, List<string> items)
        => current.Kind switch
        {
            GoToKind.Start => ITEM_START,
            GoToKind.Next  => ITEM_NEXT,
            GoToKind.End   => ITEM_END,
            GoToKind.Label => (current.Label ?? string.Empty).Trim(),
            _ => ITEM_NEXT
        };

    private void ApplySelection()
    {
        var sel = _cmb.SelectedItem?.ToString() ?? ITEM_NEXT;
        switch (sel)
        {
            case ITEM_START:
                _target.Kind = GoToKind.Start;
                _target.Label = string.Empty;
                break;
            case ITEM_NEXT:
                _target.Kind = GoToKind.Next;
                _target.Label = string.Empty;
                break;
            case ITEM_END:
                _target.Kind = GoToKind.End;
                _target.Label = string.Empty;
                break;
            default:
                _target.Kind = GoToKind.Label;
                _target.Label = sel;
                break;
        }
    }
}