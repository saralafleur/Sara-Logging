using System;

namespace Sara.Logging
{
    public class ArchiveArgs
    {
        public static readonly int IgnoreFileSizeLimits = 0;

        public ArchiveArgs()
        {
            // today at 11:59PM
            End = DateTime.Now.Date.AddDays(1).AddMinutes(-1);
            Start = DateTime.Now.Date.AddDays(-3);
            MaxArchiveSizeBytes = IgnoreFileSizeLimits;
        }
         
        /// <summary>
        /// Inclusive start datetime.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// Inclusive end datetime.
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// Max amount of data to put in archive.  If the data specified by the date range exceeds this size
        /// then the archive will not contain all of the data for the date range.  
        /// </summary>
        public int MaxArchiveSizeBytes { get; set; }

        public override string ToString()
        {
            return
                $@"ArchiveArgs: Start={Start}, End={End}, MaxArchiveSizeBytes{MaxArchiveSizeBytes}";
        }

    }

    public interface ILogArchiver
    {
        bool IsArchiveSuccess { get; }

        Exception ArchiveFailException { get; }

        void Archive(ArchiveArgs args);
    }

}
