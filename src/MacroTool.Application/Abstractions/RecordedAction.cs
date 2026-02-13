using System;
using MacroTool.Domain.Macros;

namespace MacroTool.Application.Abstractions;

public sealed record RecordedAction(TimeSpan Elapsed, MacroAction Action);
