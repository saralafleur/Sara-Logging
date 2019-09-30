using System;
using Sara.Logging.Configuration;

namespace Sara.Logging
{
    public interface ILogWriter : IDisposable
    {
        bool UseBackgroundThreadQueue { get; }
        void Initialize(ILogWriterConfiguration configuration);
        void Write(LogEntry logEntry);
        void Purge();
    }
}
