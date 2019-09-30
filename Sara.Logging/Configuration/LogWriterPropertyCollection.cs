using System.Configuration;

namespace Sara.Logging.Configuration
{
    public class LogWriterPropertyCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new LogWriterProperty();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((LogWriterProperty)element).Name;
        }
    }
}
