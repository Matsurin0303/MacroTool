namespace MacroTool.Infrastructure.Windows.Interop;

internal static class InjectionTag
{
    // dwExtraInfo に入れる目印（任意の固定値でOK）
    public static readonly IntPtr Value = (IntPtr)unchecked((nint)0x4D435254); // 'MCRT' っぽい値
}
