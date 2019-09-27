using System.Collections.Generic;

namespace Sara.NETStandard.Logging.Configuration
{
    public interface ILogConfiguration
    {
        bool IgnoreDebug { get; }
        List<ILogWriterConfiguration> WriterConfigurationList { get; }
    }
}
