using System.Collections.Generic;

namespace Sara.Logging.Configuration
{
    public interface ILogConfiguration
    {
        bool IgnoreDebug { get; }
        List<ILogWriterConfiguration> WriterConfigurationList { get; }
    }
}
