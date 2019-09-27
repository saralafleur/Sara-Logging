using System.Diagnostics;
using Sara.NETStandard.Logging.Configuration;

namespace Sara.NETStandard.Logging.Writers
{
    public class DebugOutputWriter : ILogWriter
    {
        public static bool PrependMessages { get; set; }

        private static string GetPrependText()
        {
            return PrependMessages ? "[DebugOutputWriter]" : string.Empty;
        }

        static DebugOutputWriter()
        {
            PrependMessages = true;
        }

        #region ILogWriter Members

        public bool UseBackgroundThreadQueue
        {
            private set;
            get;
        }

        //not used
        public void Purge() { }

        public void Initialize(ILogWriterConfiguration configuration)
        {
            UseBackgroundThreadQueue = configuration.UseBackgroundTheadQueue;
        }

        public void Write(LogEntry entry)
        {
            Debug.WriteLine($"<{GetPrependText()}> {entry}");
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Write(new LogEntry("Dispose Log Writer: " + GetType().Name));
        }

        #endregion
    }
}
