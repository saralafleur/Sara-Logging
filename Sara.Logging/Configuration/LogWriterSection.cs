using System.Collections.Generic;
using System.Configuration;

namespace Sara.Logging.Configuration
{
    public class LogWriterSection : ConfigurationElement, ILogWriterConfiguration
    {
        private const string CLogWriterType = "logWriterType";
        private const string CName = "name";
        private const string CUseQueue = "UseBackgroundThreadQueue";
        private const string CLoadDll = "loadDll";

        private Dictionary<string, string> _attributes;

        [ConfigurationProperty(CLogWriterType)]
        public string LogWriterType => this[CLogWriterType] as string;

        [ConfigurationProperty(CName)]
        public string Name => this[CName] as string;

        [ConfigurationProperty(CUseQueue)]
        public bool UseBackgroundTheadQueue => this[CUseQueue] == null || (bool)this[CUseQueue];

        public Dictionary<string, string> Attributes
        {
            get
            {
                if (_attributes != null) return _attributes;

                _attributes = new Dictionary<string, string>();
                foreach (LogWriterProperty p in LogWriterProperties)
                {
                    _attributes.Add(p.Name, p.Value);
                }

                return _attributes;
            }
        }

        [ConfigurationProperty(CLoadDll)]
        public string LoadDll => this[CLoadDll] as string;

        [ConfigurationProperty("properties")]
        [ConfigurationCollection(typeof(LogWriterProperty))]
        public LogWriterPropertyCollection LogWriterProperties => (LogWriterPropertyCollection)this["properties"];
    }
}
