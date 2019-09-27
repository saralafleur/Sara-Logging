using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Sara.NETStandard.Common.Extension;
using Sara.NETStandard.Logging.Configuration;

namespace Sara.NETStandard.Logging.Writers.File
{
    public class FileStreamLogWriter : ILogWriter, ILogArchiver
    {
        #region Const
        private const string CMaxFileSizeInBytes = "MaxFileSizeInBytes";
        private const string CLogEntryDateFormat = "MM/dd/yyyy hh:mm:ss.fff tt zzz";
        private const string CMaxStorageSizeInBytes = "MaxStorageSizeInBytes";
        private const string CMaxDaysToKeepLogs = "MaxDaysToKeepLogs";
        private const string CLogFileName = "LogFileName";
        private const string CZipSearchPattern = "ZipSearchPattern";
        private const int CLogSizeMax = 10485760; // 10MB
        #endregion

        #region Properties
        public bool UseBackgroundThreadQueue { private set; get; }

        private Stream _fileStream;
        private StreamWriter _writer;
        private string _currentDirectory;
        private string _fileName;
        private string _fullLogPath;
        private bool _isInitialized;
        private int _maxFileSizeInBytes;

        private DateTime _lastLogEntryWrittenDate;
        private readonly PurgeSelfMaintain _purge = new PurgeSelfMaintain();
        private readonly ArchiveService _archive = new ArchiveService();
        #endregion

        #region Setup
        public void Initialize(ILogWriterConfiguration configuration)
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            if (configuration == null)
            {
                UseBackgroundThreadQueue = true;
                _fileName = processName + ".log";
                _archive.ArchiveZipSearchPattern = "*.Log";
                _maxFileSizeInBytes = CLogSizeMax;
            }
            else
            {
                UseBackgroundThreadQueue = configuration.UseBackgroundTheadQueue;
                _fileName = configuration.Attributes.GetValue(CLogFileName, $"{processName}.log");
                _archive.ArchiveZipSearchPattern = configuration.Attributes.GetValue(CZipSearchPattern, "*.Log");
                _maxFileSizeInBytes = configuration.Attributes.GetValue(CMaxFileSizeInBytes, CLogSizeMax);
                _purge.MaxStorageSizeInBytes = configuration.Attributes.GetValue(CMaxStorageSizeInBytes, PurgeSelfMaintain.NoStorageSizeMax);
                _purge.MaxDaysToKeepLogs = configuration.Attributes.GetValue(CMaxDaysToKeepLogs, PurgeSelfMaintain.KeepLogsForever);
            }

            if (string.IsNullOrEmpty(_currentDirectory))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                _currentDirectory =
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Log");
                _purge.CurrentDirectory = _currentDirectory;
                _purge.FileName = _fileName;
                _archive.CurrentDirectory = _currentDirectory;
                _archive.FileName = _fileName;
            }

            _lastLogEntryWrittenDate = DateTime.Now;

            SetupWriter();
            _isInitialized = true;

            LogVersionInformation();
        }
        private void SetupWriter()
        {
            CreateDirectory();

            _fullLogPath = GetNewLogFileFullPath();
            try
            {
                _fileStream = System.IO.File.Open(_fullLogPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(_fileStream) { AutoFlush = true };
            }
            catch (Exception exc)
            {
                var msg = $"FileStreamWriter cannot open log file: {_fullLogPath}";
                Log.WriteError(msg, GetType().FullName, MethodBase.GetCurrentMethod().Name, exc);
                Log.WriteSystem(msg);
                throw;
            }
        }
        private void CreateDirectory()
        {
            if (Directory.Exists(_currentDirectory))
                return;

            try
            {
                Directory.CreateDirectory(_currentDirectory);
            }
            catch (Exception exc)
            {
                Log.WriteError($"Cannot create Log Directory: {_currentDirectory}", GetType().FullName, MethodBase.GetCurrentMethod().Name, exc);
                Log.WriteSystem($"FileStreamWriter cannot create Log Directory: {_currentDirectory}");
                throw;
            }
        }
        private string GetNewLogFileFullPath()
        {
            var fileName = DateTime.Now.ToString(FileConst.CLogFilenameFormat);
            var path = Path.Combine(_currentDirectory,
                // ReSharper disable once AssignNullToNotNullAttribute
                Path.ChangeExtension(_fileName,  $"{fileName}.log"));

            var count = 2;
            while (System.IO.File.Exists(path))
            {
                path = Path.Combine(_currentDirectory,
                    // ReSharper disable once AssignNullToNotNullAttribute
                    Path.ChangeExtension(_fileName, $"{fileName}_{count}.log"));
                count++;
            }

            return path;
        }
        #endregion

        #region Write
        public void Write(LogEntry entry)
        {
            if (!_isInitialized)
                throw new Exception("FileStreamLogWriter was not initialized.");

            var tempLastEntry = _lastLogEntryWrittenDate;
            if (_fileStream.CanWrite)
            {
                _lastLogEntryWrittenDate = DateTime.Now;
                _writer.WriteLine(entry.ToString(CLogEntryDateFormat));
            }

            if (!HandleLogRollover(tempLastEntry)) return;

            if (!_fileStream.CanWrite) return;

            _lastLogEntryWrittenDate = DateTime.Now;
            _writer.WriteLine(entry.ToString(CLogEntryDateFormat));
        }
        private bool HandleLogRollover(DateTime lastEntryDate)
        {
            if (FileSizeExceeded())
            {
                StartNewLogFile(NewLogReason.FileSizeExceeded);
                Purge();
                return true;
            }

            if (DateTime.Now.Date <= lastEntryDate.Date) return false;

            StartNewLogFile(NewLogReason.DayRollover);
            Purge();
            return true;
        }
        private void StartNewLogFile(NewLogReason reason)
        {
            _fileStream.Flush();
            _fileStream.Close();
            SetupWriter();

            LogVersionInformation();
            var entry = new LogEntry(Thread.CurrentThread.ManagedThreadId, DateTime.Now, GetType().Name,
                MethodBase.GetCurrentMethod().Name, LogEntryType.Trace, null, $"New Log File reason: {reason}");
            Write(entry);
        }
        private bool FileSizeExceeded()
        {
            return _fileStream.Length > _maxFileSizeInBytes;
        }
        private void LogVersionInformation()
        {
            var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(entryAssembly.Location);
            var entry = new LogEntry(Thread.CurrentThread.ManagedThreadId, DateTime.Now, GetType().Name,
                MethodBase.GetCurrentMethod().Name, LogEntryType.Trace, null,
                $"Log file created for: {versionInfo.FileName} version: {versionInfo.FileVersion}");
            Write(entry);
        }
        #endregion

        #region Archice
        public bool IsArchiveSuccess => _archive.IsArchiveSuccess;
        public Exception ArchiveFailException => _archive.ArchiveFailException;
        public void Archive(ArchiveArgs args)
        {
            if (!_isInitialized)
                throw new Exception("FileStreamLogWriter was not initialized.");

            _archive.Archive(args);
        }
        #endregion

        public void Purge()
        {
            if (_purge == null) return;

            // Purge the logs on a separate thread
            var workerThread = new Thread(_purge.Purge);
            workerThread.Start();
        }
        public void Dispose()
        {
            Log.WriteTrace($"Dispose Log Writer: {GetType().Name}", GetType().FullName, MethodBase.GetCurrentMethod().Name);

            _writer.Close();
            _writer.Dispose();
            _fileStream.Close();
            _fileStream.Dispose();
        }
    }

    internal enum NewLogReason
    {
        DayRollover,
        FileSizeExceeded,
    }
}
