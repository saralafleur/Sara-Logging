using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sara.NETStandard.Logging.Writers.File
{
    /// <summary>
    /// Maintains the rules for how long to keep files and max storage size.
    /// </summary>
    internal class PurgeSelfMaintain
    {
        #region Const
        /// <summary>
        /// When 0, Logs are kept forever.
        /// </summary>
        internal const int KeepLogsForever = 0;
        /// <summary>
        /// When 0, Logs are kept without limit.
        /// </summary>
        internal const long NoStorageSizeMax = 0;
        #endregion

        #region Properties
        public string CurrentDirectory;
        public string FileName;
        private int _maxDaysToKeepLogs;
        public int MaxDaysToKeepLogs
        {
            get { return _maxDaysToKeepLogs; }
            set
            {
                _maxDaysToKeepLogs = value;
                Log.Write(
                    _maxDaysToKeepLogs == KeepLogsForever
                        ? "Logging configured to keep Logs forever."
                        : $"Logging configured to remove files after {_maxDaysToKeepLogs} days", GetType().FullName,
                    MethodBase.GetCurrentMethod().Name, LogEntryType.SystemInfo);
            }
        }
        private long _maxStorageSizeInBytes;
        public long MaxStorageSizeInBytes
        {
            get { return _maxStorageSizeInBytes; }
            set
            {
                _maxStorageSizeInBytes = value;
                Log.Write(
                    _maxStorageSizeInBytes == NoStorageSizeMax
                        ? "Logging configured to keep Logs regardless of size."
                        : $"Logging configured to remove files once aggregate size reaches {_maxStorageSizeInBytes} bytes",
                    GetType().FullName, MethodBase.GetCurrentMethod().Name, LogEntryType.SystemInfo);

            }
        }
        #endregion

        internal void Purge()
        {
            DeleteFilesBasedOnSize();
            DeleteFilesBasedOnDays();
        }
        private List<FileInfo> GetLogFileInformation()
        {
            var files = new List<string>(Directory.GetFiles(CurrentDirectory, Path.GetFileNameWithoutExtension(FileName) + "*"));
            return files.Select(file => new FileInfo(file)).OrderBy(fileInfo => fileInfo.CreationTimeUtc).ToList();
        }
        private void DeleteFilesBasedOnDays()
        {
            if (string.IsNullOrEmpty(CurrentDirectory)) return;
            if (MaxDaysToKeepLogs <= KeepLogsForever) return;

            var fileInfos = GetLogFileInformation();
            try
            {
                var now = DateTime.UtcNow;
                var filesToDelete =
                    fileInfos.Where(fileInfo => now.Subtract(fileInfo.CreationTimeUtc).Days > MaxDaysToKeepLogs);

                foreach (var fileToDelete in filesToDelete)
                {
                    System.IO.File.Delete(fileToDelete.FullName);
                    Log.Write("Log file {fileToDelete.FullName} was removed because it was older than {_maxDaysToKeepLogs} days", typeof(FileStreamLogWriter).FullName, MethodBase.GetCurrentMethod().Name);
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("Exception in DeleteFilesBasedOnDays", typeof(FileStreamLogWriter).FullName, MethodBase.GetCurrentMethod().Name, ex);
            }
        }
        private void DeleteFilesBasedOnSize()
        {
            if (string.IsNullOrEmpty(CurrentDirectory)) return;
            if (MaxStorageSizeInBytes <= NoStorageSizeMax) return;

            var fileInfos = GetLogFileInformation();
            try
            {
                var totalSizeInBytes = fileInfos.Sum(fileInfo => fileInfo.Length);

                if (totalSizeInBytes <= MaxStorageSizeInBytes)
                    return;

                foreach (var fileToDelete in fileInfos)
                {
                    System.IO.File.Delete(fileToDelete.FullName);
                    Log.Write("Log file {fileToDelete.FullName} was removed because the total size of log files are greater than {_maxStorageSizeInBytes} bytes",
                        typeof(FileStreamLogWriter).FullName, MethodBase.GetCurrentMethod().Name);
                    totalSizeInBytes -= fileToDelete.Length;
                    if (totalSizeInBytes <= MaxStorageSizeInBytes)
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("Exception in DeleteFilesBasedOnSize", typeof(FileStreamLogWriter).FullName, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

    }
}
