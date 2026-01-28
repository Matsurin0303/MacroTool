using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using MacroTool.Domain.Macros;

namespace MacroTool.Application.Abstractions;

public sealed record RecordedAction(DateTime Timestamp, MacroAction Action);
