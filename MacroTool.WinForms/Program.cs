using MacroTool.Application.Abstractions;
using MacroTool.Application.Playback;
using MacroTool.Application.Services;
using MacroTool.Infrastructure.Windows.Persistence;
using MacroTool.Infrastructure.Windows.Playback;
using MacroTool.Infrastructure.Windows.Recording;
using MacroTool.WinForms.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MacroTool.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        System.Windows.Forms.ApplicationConfiguration.Initialize();

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                // Settings（LocalAppData\MacroTool\settings.json）
                var store = new SettingsStore(SettingsStore.DefaultPath());
                var s = store.Load();

                var initial = new PlaybackOptions
                {
                    EnableStabilizeWait = s.Playback.EnableStabilizeWait,
                    CursorSettleDelayMs = s.Playback.CursorSettleDelayMs,
                    ClickHoldDelayMs = s.Playback.ClickHoldDelayMs
                };

                // ★即時反映の要：アクセサをSingletonで保持
                services.AddSingleton<IPlaybackOptionsAccessor>(new PlaybackOptionsAccessor(initial));

                // Core
                services.AddSingleton<IRecorder, LowLevelHookRecorder>();
                services.AddSingleton<IPlayer, SendInputPlayer>();
                services.AddSingleton<IMacroRepository, JsonMacroRepository>();
                services.AddSingleton<MacroAppService>();

                // Form
                services.AddTransient<Form1>();
            })
            .Build();

        var form = host.Services.GetRequiredService<Form1>();
        System.Windows.Forms.Application.Run(form);
    }
}
