using MacroTool.Application.Abstractions;
using MacroTool.Application.Playback;
using MacroTool.Application.Services;
using MacroTool.Infrastructure.Windows.Persistence;
using MacroTool.Infrastructure.Windows.Playback;
using MacroTool.Infrastructure.Windows.Recording;
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
                services.AddSingleton<IRecorder, LowLevelHookRecorder>();
                services.AddSingleton<IPlayer, SendInputPlayer>();
                services.AddSingleton<IMacroRepository, JsonMacroRepository>();
                services.AddSingleton<MacroAppService>();

                services.Configure<PlaybackOptions>(ctx.Configuration.GetSection("Playback"));

                services.AddTransient<Form1>();
            })
            .Build();

        var form = host.Services.GetRequiredService<Form1>();
        System.Windows.Forms.Application.Run(form);
    }
}
