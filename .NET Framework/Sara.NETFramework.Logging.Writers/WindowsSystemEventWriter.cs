using System.Diagnostics;
using Sara.NETStandard.Logging;
using Sara.NETStandard.Logging.Configuration;

namespace Sara.NETFramework.Logging.Writers
{
    public class WindowsSystemEventWriter : ILogSystemWriter
    {
        private const string ApplicationSource = "Application";

        public bool UseBackgroundThreadQueue { private set; get; }
        public void Initialize(ILogWriterConfiguration configuration)
        {
            // Always default to a direct Writer
            UseBackgroundThreadQueue = false;
        }

        public void Write(LogEntry entry)
        {
            //Note: If an exception occurs we want it to bubble up to the Consumer - Sara

            EventLogEntryType eType;

            switch (entry.LogEntryType)
            {
                case LogEntryType.Warning:
                case LogEntryType.SystemWarning:
                    eType = EventLogEntryType.Warning;
                    break;
                case LogEntryType.Trace:
                case LogEntryType.SystemInfo:
                    eType = EventLogEntryType.Information;
                    break;
                default:
                    eType = EventLogEntryType.Error;
                    break;
            }
            EventLog.WriteEntry(ApplicationSource, entry.ToString(), eType);
        }

        public void Purge() { }

        public void Dispose()
        {
            Write(new LogEntry("Dispose Log Writer: " + GetType().Name));
        }
    }
}
