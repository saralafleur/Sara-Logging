using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Sara.NETStandard.Common.Extension;
using Sara.NETStandard.Logging.Configuration;

namespace Sara.NETStandard.Logging
{
    public static partial class Log
    {
        private static readonly object LockObject = new object();
        private static readonly List<ILogWriter> DirectWriters = new List<ILogWriter>();
        /// <summary>
        /// When True, Debug LogEntryType will go to Writers.
        /// </summary>
        private static bool _ignoreDebug;

        #region Setup & Exit
        static Log()
        {
            ILogConfiguration configuration = LogConfigurationSection.Get();

            if (configuration == null)
                return;

            SetConfiguration(configuration);
        }
        private static void SetConfiguration(ILogConfiguration configuration)
        {
            lock (LockObject)
            {
                _ignoreDebug = configuration.IgnoreDebug;

                if (configuration.WriterConfigurationList.Count > 0)
                {
                    foreach (var writerConfig in configuration.WriterConfigurationList)
                    {
                        Assembly assembly = null;
                        try
                        {
                            assembly = string.IsNullOrEmpty(writerConfig.LoadDll) ? Assembly.GetExecutingAssembly() : Assembly.LoadFrom(writerConfig.LoadDll);

                            var types = assembly.GetClassTypes(typeof(ILogWriter));

                            var typeToInstantiate = types.SingleOrDefault(t => t.Name == writerConfig.LogWriterType);
                            if (typeToInstantiate == null)
                                throw (new Exception(
                                    $"No writers of the name '{writerConfig.LogWriterType}' were found in the assembly '{assembly.FullName}'"));

                            var lw = (ILogWriter)assembly.CreateInstance(typeToInstantiate.FullName);
                            if (lw != null)
                            {
                                lw.Initialize(writerConfig);

                                AddWriter(lw);
                            }
                        }
                        catch (ReflectionTypeLoadException rtle)
                        {

                            WriteSystem($"An error occurred attempting to load LogWriter '{writerConfig.LogWriterType}' from Assembly \"{(assembly == null ? "Not Found" : assembly.FullName)}\"");
                            foreach (var loaderException in rtle.LoaderExceptions)
                            {
                                WriteSystem($"LoaderException 'loaderException.ToString()':");
                            }
                            throw;
                        }
                        catch (Exception)
                        {
                            WriteSystem($"An error occurred attempting to load LogWriter '{writerConfig.LogWriterType}' from Assembly \"{(assembly == null ? "Not Found" : assembly.FullName)}\"");
                            throw;
                        }

                    }
                }

                RegisterAppDomainForUnhandledException(AppDomain.CurrentDomain);
                WriteTrace($"Logging Initialized [Configuration.IsLoggerDebug: {_ignoreDebug}]", typeof(Log).FullName, MethodBase.GetCurrentMethod().Name);
                PurgeAllWriters();
            }
        }
        /// <summary>
        /// Flush Queue and shutdowns all Logging.
        /// Waits for the given timeout before forcing the shutdown.
        /// </summary>
        public static bool Exit(TimeSpan? timeout = null)
        {
            if (timeout == null)
                timeout = new TimeSpan(0, 0, 10);

            lock (LockObject)
            {
                SendToWriters(new LogEntry(Thread.CurrentThread.ManagedThreadId, DateTime.Now, typeof(BackgroundThreadQueue).FullName, MethodBase.GetCurrentMethod().Name, LogEntryType.Trace, null, "BackgroundThreadQueue Exit Requested"));

                var result = true;

                result = BackgroundThreadQueue.Exit(timeout.Value);

                AppDomain.CurrentDomain.UnhandledException -= appDomain_UnhandledException;

                DisposeAllWriters();

                return result;
            }
        }
        #endregion

        #region Write Methods
        public static void Write(string message, string className = "", string operationName = "", LogEntryType logType = LogEntryType.Information, Exception exception = null)
        {
            if (!_ignoreDebug && logType == LogEntryType.Debug) return;

            SendToWriters(new LogEntry(Thread.CurrentThread.ManagedThreadId, DateTime.Now, className, operationName, logType, exception, message));
        }
        public static void WriteTrace(string message, string className = "", string operationName = "")
        {
            Write(message, className, operationName, LogEntryType.Trace);
        }
        public static void WriteError(string message, string className = "", string operationName = "", Exception exception = null)
        {
            Write(message, className, operationName, LogEntryType.Error, exception);
        }
        public static void WriteError(string className = "", string operationName = "", Exception exception = null)
        {
            Write("", className, operationName, LogEntryType.Error, exception);
        }

        public static void WriteSystem(LogEntry entry)
        {
            // TODO : How to handle Ignore - Sara
            lock (LockObject)
            {
                if (DirectWriters.Count <= 0) return;

                foreach (var directWriter in DirectWriters)
                {
                    if (directWriter is ILogSystemWriter)
                        directWriter.Write(entry);
                }
            }

            Debug.WriteLine("[EventLog] WritingToWindowsEventLog -- " + entry);
            Console.WriteLine(@"[EventLog] WritingToWindowsEventLog -- " + entry);
        }
        public static void WriteSystem(string message, string className = "", string operationName = "", LogEntryType logType = LogEntryType.SystemError, Exception exception = null)
        {
            WriteSystem(new LogEntry(Thread.CurrentThread.ManagedThreadId, DateTime.Now, className, operationName,
                logType, exception, message));
        }
        private static void SendToWriters(LogEntry entry)
        {
            lock (LockObject)
            {
                var written = false;
                if (BackgroundThreadQueue.WriterCount > 0)
                {
                    BackgroundThreadQueue.AddMessage(entry);
                    written = true;
                }

                if (DirectWriters.Count > 0)
                {
                    foreach (var directWriter in DirectWriters)
                    {
                        directWriter.Write(entry);
                    }
                    written = true;
                }

                if (!written)
                {
                    entry.Message = $"No Writers : {entry.Message}";
                    WriteSystem(entry);
                }
            }
        }
        #endregion

        #region Archive
        public static void Archive(ArchiveArgs args)
        {
            lock (LockObject)
            {
                ArchiveOnWriters(DirectWriters, args);
                ArchiveOnWriters(BackgroundThreadQueue.Writers, args);
            }
        }
        private static void ArchiveOnWriters(IEnumerable<ILogWriter> writers, ArchiveArgs args)
        {
            foreach (var writer in writers)
            {
                var archiver = writer as ILogArchiver;
                if (archiver == null)
                    continue;

                archiver.Archive(args);
                if (false != archiver.IsArchiveSuccess) continue;

                WriteSystem(archiver.ArchiveFailException.ToString());
                WriteError("", typeof(Log).FullName, MethodBase.GetCurrentMethod().Name, archiver.ArchiveFailException);
            }
        }
        #endregion

        #region Private Methods
        private static ILogWriter GetWriter(Type logWriterType)
        {
            foreach (var w in DirectWriters.Where(w => w.GetType() == logWriterType))
                return w;

            return BackgroundThreadQueue.Writers.FirstOrDefault(w => w.GetType() == logWriterType);
        }
        private static void AddWriter(ILogWriter writer)
        {
            if (writer.UseBackgroundThreadQueue)
                AddQueueWriter(writer);
            else
                AddDirectWriter(writer);

            // adding a message as soon as the logger is added so that the logger has a timestamp in its log for when it was added
            WriteTrace($"LogWriter '{writer.GetType().Name}'({(writer.UseBackgroundThreadQueue ? "queued" : "not queued")}) added", typeof(Log).FullName, MethodBase.GetCurrentMethod().Name);
        }
        private static void RemoveWriter(ILogWriter writer)
        {


            if (writer.UseBackgroundThreadQueue)
                BackgroundThreadQueue.Remove(writer);
            else
                DirectWriters.Remove(writer);

            // adding a message as soon as the logger is removed so that the logger has a timestamp in its log for when it was removed
            WriteTrace($"LogWriter '{writer.GetType().Name}'({(writer.UseBackgroundThreadQueue ? "queued" : "not queued")}) removed", typeof(Log).FullName, MethodBase.GetCurrentMethod().Name);
        }
        private static void DisposeAllWriters()
        {
            foreach (var writer in DirectWriters)
                writer.Dispose();

            DirectWriters.Clear();

            BackgroundThreadQueue.Exit(TimeSpan.FromSeconds(1));
        }
        private static void AddQueueWriter(ILogWriter writer)
        {
            BackgroundThreadQueue.Add(writer);
        }
        private static void PurgeAllWriters()
        {
            foreach (var logWriter in BackgroundThreadQueue.Writers)
                logWriter.Purge();

            DirectWriters.ForEach(lw => lw.Purge());
        }
        private static void AddDirectWriter(ILogWriter writer)
        {
            DirectWriters.Add(writer);
        }
        #endregion

        #region ErrrorHandling
        private static void RegisterAppDomainForUnhandledException(AppDomain appDomain)
        {
            appDomain.UnhandledException -= appDomain_UnhandledException;
            appDomain.UnhandledException += appDomain_UnhandledException;

            WriteTrace("The AppDomain's UnhandledException Handler is Registered", typeof(Log).FullName, MethodBase.GetCurrentMethod().Name);
        }
        private static void appDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exc = e.ExceptionObject as Exception;
            if (exc == null)
            {
                string exceptionObjectType = "NULL",
                        exceptionObjectToString = "NULL";
                if (e.ExceptionObject != null) //ExceptionObject was set but not an exception type
                {
                    exceptionObjectType = e.ExceptionObject.GetType().FullName;
                    exceptionObjectToString = e.ExceptionObject.ToString();
                }

                var message =
                    $"Unhandled Exception in AppDomain. [IsTerminating: {e.IsTerminating}, ExceptionObjectType: {exceptionObjectType}, ExceptionObject: {exceptionObjectToString}]";

                WriteError(message, typeof(Log).FullName, MethodBase.GetCurrentMethod().Name);

                WriteSystem(message);
            }
            else
            {
                var message = $"Unhandled Exception in AppDomain. [IsTerminating: {e.IsTerminating}]";
                WriteError(message, typeof(Log).FullName, MethodBase.GetCurrentMethod().Name, exc);

                WriteSystem(new LogEntry(Thread.CurrentThread.ManagedThreadId, DateTime.Now, typeof(Log).FullName, MethodBase.GetCurrentMethod().Name, LogEntryType.Error, exc, message));
            }

            BackgroundThreadQueue.Exit(TimeSpan.FromSeconds(5));
        }
        #endregion

    }
}
