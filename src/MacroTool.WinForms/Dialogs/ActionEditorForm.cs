using MacroTool.Domain.Macros;
using System.Text.Json;

namespace MacroTool.WinForms.Dialogs;

/// <summary>
/// 汎用アクション編集（PropertyGrid）。
/// UIは簡易実装（要件: 機能重視 / 将来実装予定は非表示）。
/// </summary>
public sealed class ActionEditorForm : Form
{
    private readonly PropertyGrid _grid;
    private readonly Button _ok;
    private readonly Button _cancel;

    public MacroAction EditedAction { get; private set; }
    public static MacroAction? EditAction(IWin32Window owner, MacroAction action, string? title = null)
    => Edit(owner, action, title);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    private ActionEditorForm(MacroAction original, string? title)
    {
        Text = title ?? "Edit Action";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        Width = 560;
        Height = 520;

        // Deep clone (recordのwithは参照型プロパティが共有されうるため)
        EditedAction = DeepClone(original);

        _grid = new PropertyGrid
        {
            Dock = DockStyle.Fill,
            SelectedObject = EditedAction,
            HelpVisible = true,
            ToolbarVisible = false
        };

        _ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90, Height = 28 };
        _cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 28 };

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10),
            Height = 50
        };
        bottom.Controls.Add(_ok);
        bottom.Controls.Add(_cancel);

        Controls.Add(_grid);
        Controls.Add(bottom);

        AcceptButton = _ok;
        CancelButton = _cancel;
    }

    public static MacroAction? Edit(IWin32Window owner, MacroAction action, string? title = null)
    {
        using var dlg = new ActionEditorForm(action, title);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.EditedAction : null;
    }

    private static MacroAction DeepClone(MacroAction action)
    {
        var json = JsonSerializer.Serialize<MacroAction>(action, _jsonOptions);
        return JsonSerializer.Deserialize<MacroAction>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to clone action.");
    }
}
