using MacroTool.Domain.Macros;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace MacroTool.WinForms.Editors;

/// <summary>
/// GoToTarget を「Start/Next/End/Label(一覧)」の単一選択で編集するための PropertyGrid エディタ。
/// Domain に依存を逆流させないため、Form 起動時に TypeDescriptor で登録する。
/// </summary>
public sealed class GoToTargetEditor : UITypeEditor
{
    private static bool _registered;

    /// <summary>現在のマクロからラベル一覧を返す（呼び出し時点の最新を返す想定）</summary>
    public static Func<IReadOnlyList<string>>? LabelsProvider { get; set; }

    public static void Register()
    {
        if (_registered) return;
        _registered = true;

        TypeDescriptor.AddAttributes(
            typeof(GoToTarget),
            new EditorAttribute(typeof(GoToTargetEditor), typeof(UITypeEditor)));
    }

    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        => UITypeEditorEditStyle.Modal;

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider? provider, object? value)
    {
        var current = value as GoToTarget ?? new GoToTarget();
        var working = new GoToTarget { Kind = current.Kind, Label = current.Label };

        var labels = LabelsProvider?.Invoke() ?? Array.Empty<string>();
        using var dlg = new GoToTargetEditForm(working, labels);

        var svc = provider?.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
        var result = svc is not null ? svc.ShowDialog(dlg) : dlg.ShowDialog();

        return result == DialogResult.OK? working : current;
    }
}