using MacroTool.Application.Abstractions;
using MacroTool.Domain.Macros;
using System.Text.Json;

namespace MacroTool.Infrastructure.Windows.Persistence;

public sealed class JsonMacroRepository : IMacroRepository
{
    private const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void Save(string path, Macro macro)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty", nameof(path));

        var dto = new MacroFileDto { Version = CurrentVersion };

        foreach (var s in macro.Steps)
            dto.Steps.Add(ToDto(s));

        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    public Macro Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty", nameof(path));

        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<MacroFileDto>(json, JsonOptions)
                  ?? throw new InvalidDataException("Invalid macro file.");

        if (dto.Version is not 1 and not 2)
            throw new InvalidDataException($"Unsupported macro version: {dto.Version}");


        var macro = new Macro();
        foreach (var s in dto.Steps)
            macro.AddStep(FromDto(s));

        return macro;
    }

    // ===== DTO =====
    private sealed class MacroFileDto
    {
        public int Version { get; set; } = CurrentVersion;
        public List<StepDto> Steps { get; set; } = new();
    }

    private sealed class StepDto
    {
        public int DelayMs { get; set; }
        public ActionDto Action { get; set; } = new();

        public string? Label { get; set; }
        public string? Comment { get; set; }
    }


    private sealed class ActionDto
    {
        public string Kind { get; set; } = ""; // MouseClick / KeyDown / KeyUp

        // MouseClick
        public int? X { get; set; }
        public int? Y { get; set; }
        public string? Button { get; set; } // Left / Right

        // Key
        public ushort? Vk { get; set; }
    }

    private static StepDto ToDto(MacroStep step)
        => new()
        {
            DelayMs = step.Delay.TotalMilliseconds,
            Action = ToDto(step.Action),
            Label = step.Label,
            Comment = step.Comment
        };


    private static ActionDto ToDto(MacroAction action)
    {
        return action switch
        {
            MouseClick mc => new ActionDto
            {
                Kind = "MouseClick",
                X = mc.Point.X,
                Y = mc.Point.Y,
                Button = mc.Button.ToString()
            },

            KeyDown kd => new ActionDto
            {
                Kind = "KeyDown",
                Vk = kd.Key.Code
            },

            KeyUp ku => new ActionDto
            {
                Kind = "KeyUp",
                Vk = ku.Key.Code
            },

            _ => throw new NotSupportedException($"Unsupported action: {action.GetType().Name}")
        };
    }

    private static MacroStep FromDto(StepDto dto)
    {
        var delay = MacroDelay.FromMilliseconds(dto.DelayMs);
        var action = FromDto(dto.Action);

        var label = dto.Label ?? "";
        var comment = dto.Comment ?? "";

        return new MacroStep(delay, action, label, comment);
    }


    private static int RequiredInt(int? v, string name)
        => v ?? throw new InvalidDataException($"Missing required field: {name}");

    private static ushort RequiredUShort(ushort? v, string name)
        => v ?? throw new InvalidDataException($"Missing required field: {name}");

    private static string RequiredString(string? v, string name)
        => string.IsNullOrWhiteSpace(v)
            ? throw new InvalidDataException($"Missing required field: {name}")
            : v;

    private static MacroAction FromDto(ActionDto dto)
    {
        return dto.Kind switch
        {
            "MouseClick" => new MouseClick(
                new ScreenPoint(
                    RequiredInt(dto.X, "Action.X"),
                    RequiredInt(dto.Y, "Action.Y")),
                ParseButton(RequiredString(dto.Button, "Action.Button"))
            ),

            "KeyDown" => new KeyDown(new VirtualKey(RequiredUShort(dto.Vk, "Action.Vk"))),
            "KeyUp" => new KeyUp(new VirtualKey(RequiredUShort(dto.Vk, "Action.Vk"))),

            _ => throw new InvalidDataException($"Unknown action kind: {dto.Kind}")
        };
    }

    private static MouseButton ParseButton(string s)
    {
        if (string.Equals(s, "Left", StringComparison.OrdinalIgnoreCase)) return MouseButton.Left;
        if (string.Equals(s, "Right", StringComparison.OrdinalIgnoreCase)) return MouseButton.Right;
        throw new InvalidDataException($"Invalid mouse button: {s}");
    }

}
