using System.Configuration;

namespace Sara.NETStandard.Logging.Configuration
{
    public class LogWriterSectionCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new LogWriterSection();
        }

        /// <summary>
        /// Unique key is by name, or if that is not present by type.
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            var section = ((LogWriterSection)element);
            return string.IsNullOrEmpty(section.Name) ? section.LogWriterType : section.Name;
        }

        public LogWriterSection this[int index]
        {
            get
            {
                return (LogWriterSection)BaseGet(index);
            }
        }
    }
}
