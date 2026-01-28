using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace MacroTool.Application.Abstractions;

public interface IRecorder
{
    event EventHandler<RecordedAction>? ActionRecorded;

    bool Start();
    void Stop();

    bool IsRecording { get; }
}
