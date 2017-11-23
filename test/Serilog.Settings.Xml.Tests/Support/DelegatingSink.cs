using System;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Tests.Support
{
    internal sealed class DelegatingSink : ILogEventSink
    {
        readonly Action<LogEvent> _write;

        public DelegatingSink(Action<LogEvent> write)
        {
            if (write == null)
                throw new ArgumentNullException(nameof(write));
            _write = write;
        }

        public void Emit(LogEvent logEvent)
        {
            _write(logEvent);
        }
    }
}
