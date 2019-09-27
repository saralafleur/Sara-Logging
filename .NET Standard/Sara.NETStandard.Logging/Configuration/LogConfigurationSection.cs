using System.Collections.Generic;
using System.Configuration;

namespace Sara.NETStandard.Logging.Configuration
{
    public class LogConfigurationSection : ConfigurationSection, ILogConfiguration
    {
        private const string CIgnoreDebug = "IgnoreDebug";
        private const string ConfigPath = "Logging";
        private const string CWriters = "writers";

        private List<ILogWriterConfiguration> _writerConfigurationList;

        public static LogConfigurationSection Get()
        {
            return (LogConfigurationSection)ConfigurationManager.GetSection(ConfigPath);
        }

        [ConfigurationProperty(CIgnoreDebug)]
        public bool IgnoreDebug => this[CIgnoreDebug] != null && (bool)this[CIgnoreDebug];

        [ConfigurationProperty(CWriters)]
        [ConfigurationCollection(typeof(LogWriterSection), AddItemName = "writer")]
        internal LogWriterSectionCollection Writers => this[CWriters] as LogWriterSectionCollection;

        public List<ILogWriterConfiguration> WriterConfigurationList
        {
            get
            {
                if (_writerConfigurationList == null)
                {
                    // First time through initialize the list
                    _writerConfigurationList = new List<ILogWriterConfiguration>();
                    foreach (LogWriterSection s in Writers)
                    {
                        _writerConfigurationList.Add(s);
                    }
                }

                return _writerConfigurationList;
            }
        }
    }
}
