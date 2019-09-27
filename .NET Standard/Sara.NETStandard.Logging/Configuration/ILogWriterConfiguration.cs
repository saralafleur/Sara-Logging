using System.Collections.Generic;

namespace Sara.NETStandard.Logging.Configuration
{
    public interface ILogWriterConfiguration
    {
        Dictionary<string, string> Attributes { get; }
        /// <summary>
        /// Optional.  Used to load a different dll that implements ILogWriter vs the current Assembly(dll).
        /// </summary>
        string LoadDll { get; }
        /// <summary>
        /// Type of the ILogWriter class to instantiate
        /// </summary>
        string LogWriterType { get; }
        bool UseBackgroundTheadQueue { get; }
    }
}
