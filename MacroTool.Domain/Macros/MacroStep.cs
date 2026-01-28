using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroTool.Domain.Macros;

public sealed record MacroStep(MacroDelay Delay, MacroAction Action);
