using System;
using Sara.NETStandard.Logging.Configuration;

namespace Sara.NETStandard.Logging
{
    public interface ILogWriter : IDisposable
    {
        bool UseBackgroundThreadQueue { get; }
        void Initialize(ILogWriterConfiguration configuration);
        void Write(LogEntry logEntry);
        void Purge();
    }
}
