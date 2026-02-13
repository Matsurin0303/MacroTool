using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;
using System.Text.Json;

namespace MacroTool.Infrastructure.Windows.Persistence;

/// <summary>
/// MacroファイルのJSON永続化。
/// - v3: v1.0仕様に合わせ、Delayではなく Wait アクションとして待機を表現
/// - v1/v2: 旧フォーマット互換読み込み（DelayMsを Wait に変換）
/// </summary>
public sealed class JsonMacroRepository : IMacroRepository
{
    private const int CurrentVersion = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void Save(string path, Macro macro)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty", nameof(path));

        var dto = new MacroFileV3Dto
        {
            Version = CurrentVersion,
            Steps = macro.Steps
                .Select(s => new StepV3Dto
                {
                    Action = s.Action,
                    Label = s.Label,
                    Comment = s.Comment
                })
                .ToList()
        };

        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    public Macro Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty", nameof(path));

        var json = File.ReadAllText(path);

        // まずVersionだけ判定
        var ver = JsonSerializer.Deserialize<VersionProbeDto>(json, JsonOptions)?.Version ?? 1;

        return ver switch
        {
            3 => LoadV3(json),
            1 or 2 => LoadLegacyV1V2(json, ver),
            _ => throw new InvalidDataException($"Unsupported macro version: {ver}")
        };
    }

    private static Macro LoadV3(string json)
    {
        var dto = JsonSerializer.Deserialize<MacroFileV3Dto>(json, JsonOptions)
                  ?? throw new InvalidDataException("Invalid macro file.");

        var macro = new Macro();
        foreach (var s in dto.Steps)
        {
            if (s.Action is null)
                throw new InvalidDataException("Step.Action is null.");

            macro.AddStep(new MacroStep(s.Action, s.Label ?? string.Empty, s.Comment ?? string.Empty));
        }

        return macro;
    }

    private static Macro LoadLegacyV1V2(string json, int version)
    {
        var dto = JsonSerializer.Deserialize<MacroFileLegacyDto>(json, JsonOptions)
                  ?? throw new InvalidDataException("Invalid macro file.");

        if (dto.Version != version)
        {
            // JsonSerializerの設定差などでVersionが消えた場合でも、ここで安全に扱う
            dto.Version = version;
        }

        var macro = new Macro();
        foreach (var s in dto.Steps)
        {
            // DelayMs → Wait アクション（ラベル/コメントは後続アクション行へ）
            if (s.DelayMs > 0)
            {
                macro.AddStep(new MacroStep(new WaitTimeAction { Milliseconds = s.DelayMs }));
            }

            var action = ConvertLegacyAction(s.Action);
            macro.AddStep(new MacroStep(action, s.Label ?? string.Empty, s.Comment ?? string.Empty));
        }

        return macro;
    }

    private static MacroAction ConvertLegacyAction(ActionLegacyDto dto)
    {
        if (dto is null)
            throw new InvalidDataException("Step.Action is null.");

        return dto.Kind switch
        {
            "MouseClick" => new MouseClickAction
            {
                Action = MouseClickType.Click,
                Button = ParseLegacyButton(RequiredString(dto.Button, "Action.Button")),
                Relative = false,
                X = RequiredInt(dto.X, "Action.X"),
                Y = RequiredInt(dto.Y, "Action.Y")
            },

            "KeyDown" => new KeyPressAction
            {
                Option = KeyPressOption.Down,
                Key = new VirtualKey(RequiredUShort(dto.Vk, "Action.Vk")),
                Count = 1
            },

            "KeyUp" => new KeyPressAction
            {
                Option = KeyPressOption.Up,
                Key = new VirtualKey(RequiredUShort(dto.Vk, "Action.Vk")),
                Count = 1
            },

            _ => throw new InvalidDataException($"Unknown legacy action kind: {dto.Kind}")
        };
    }

    private static MouseButton ParseLegacyButton(string s)
    {
        if (string.Equals(s, "Left", StringComparison.OrdinalIgnoreCase)) return MouseButton.Left;
        if (string.Equals(s, "Right", StringComparison.OrdinalIgnoreCase)) return MouseButton.Right;
        throw new InvalidDataException($"Invalid mouse button: {s}");
    }

    private static int RequiredInt(int? v, string name)
        => v ?? throw new InvalidDataException($"Missing required field: {name}");

    private static ushort RequiredUShort(ushort? v, string name)
        => v ?? throw new InvalidDataException($"Missing required field: {name}");

    private static string RequiredString(string? v, string name)
        => string.IsNullOrWhiteSpace(v)
            ? throw new InvalidDataException($"Missing required field: {name}")
            : v;

    // ===== DTOs =====

    private sealed class VersionProbeDto
    {
        public int Version { get; set; } = 1;
    }

    // v3
    private sealed class MacroFileV3Dto
    {
        public int Version { get; set; } = CurrentVersion;
        public List<StepV3Dto> Steps { get; set; } = new();
    }

    private sealed class StepV3Dto
    {
        public MacroAction? Action { get; set; }
        public string? Label { get; set; }
        public string? Comment { get; set; }
    }

    // v1/v2 legacy
    private sealed class MacroFileLegacyDto
    {
        public int Version { get; set; } = 1;
        public List<StepLegacyDto> Steps { get; set; } = new();
    }

    private sealed class StepLegacyDto
    {
        public int DelayMs { get; set; }
        public ActionLegacyDto Action { get; set; } = new();
        public string? Label { get; set; }
        public string? Comment { get; set; }
    }

    private sealed class ActionLegacyDto
    {
        public string Kind { get; set; } = ""; // MouseClick / KeyDown / KeyUp

        // MouseClick
        public int? X { get; set; }
        public int? Y { get; set; }
        public string? Button { get; set; }

        // Key
        public ushort? Vk { get; set; }
    }
}
