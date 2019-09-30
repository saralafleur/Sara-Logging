using System.Configuration;

namespace Sara.Logging.Configuration
{
    public class LogWriterProperty : ConfigurationElement
    {
        [ConfigurationProperty("name")]
        public string Name => (string)this["name"];

        [ConfigurationProperty("value")]
        public string Value => (string)this["value"];
    }
}
