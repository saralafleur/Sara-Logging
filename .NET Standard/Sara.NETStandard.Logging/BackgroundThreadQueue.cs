using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sara.NETStandard.Logging
{
    public static partial class Log
    {
        /// <summary>
        /// BackgroundThreadQueue is a Queue that runs on a Thread.
        /// This enables the main process to continue without delay.
        /// The Queue will handle processing the Log Entry and send
        /// it to the Writers, all on it's own thread.
        /// </summary>
        private static class BackgroundThreadQueue
        {
            private class ProcessableQueue : Queue<LogEntry>
            {
                public bool ProcessingComplete { get; set; }

                public ProcessableQueue(int startCapacity) : base(startCapacity) { }
            }

            #region Properties
            private const int QueueStartSize = 100;
            private static readonly List<ILogWriter> LogWriters;
            private static ProcessableQueue _messageQueue;
            private static readonly object QueueSyncObject = new object();
            private static readonly Thread ConsumerThread;
            private static readonly AutoResetEvent NewMessageEvent = new AutoResetEvent(false);
            private static readonly AutoResetEvent ShutdownEvent = new AutoResetEvent(false);
            private static readonly AutoResetEvent ShutdownAckEvent = new AutoResetEvent(false);
            internal static int WriterCount
            {
                get
                {
                    lock (LogWriters)
                    {
                        return LogWriters?.Count ?? 0;
                    }
                }
            }
            internal static ILogWriter[] Writers
            {
                get
                {
                    if (LogWriters == null)
                        return null;

                    ILogWriter[] localCopy;
                    lock (LogWriters)
                    {
                        localCopy = new ILogWriter[LogWriters.Count];
                        LogWriters.CopyTo(localCopy);
                    }
                    return localCopy;
                }
            }
            #endregion

            #region Setup & General Methods
            static BackgroundThreadQueue()
            {
                lock (QueueSyncObject)
                {
                    _messageQueue = new ProcessableQueue(QueueStartSize);
                    LogWriters = new List<ILogWriter>();

                    try
                    {
                        ConsumerThread = new Thread(ThreadMainMethod)
                        {
                            Name = "BackgroundThreadQueue Worker",
                            IsBackground = false
                        };
                        ConsumerThread.Start();
                    }
                    catch (Exception e)
                    {
                        WriteSystem($"Unable to initialize BackgroundThreadQueue. Exception: {e}");
                    }
                }
            }
            public static void Add(ILogWriter writer)
            {
                if (writer == null)
                    return;

                lock (LogWriters)
                {
                    LogWriters.Add(writer);
                }
            }
            public static void Remove(ILogWriter writer)
            {
                if (writer == null)
                    return;

                lock (LogWriters)
                {
                    LogWriters.Remove(writer);
                }
            }
            public static void AddMessage(LogEntry entry)
            {
                lock (QueueSyncObject)
                {
                    _messageQueue.Enqueue(entry);
                    NewMessageEvent.Set();
                }
            }
            #endregion

            #region Exit Methods
            public static bool Exit(TimeSpan timeout)
            {
                if (WriterCount > 0)
                    WaitForMessagesToFlushWithoutExitCheck(timeout);

                NewMessageEvent.Set();

                ShutdownEvent.Set();
                var ackReceived = ShutdownAckEvent.WaitOne(timeout);
                if (!ackReceived)
                    ConsumerThread.Abort();


                lock (LogWriters)
                {
                    foreach (var writer in LogWriters)
                        writer.Dispose();
                    LogWriters.Clear();
                }

                return ackReceived;
            }
            private static bool WaitForMessagesToFlushWithoutExitCheck(TimeSpan timeout)
            {
                var sw = Stopwatch.StartNew(); // used for tracking the timeout
                var messagesToProcess = _messageQueue;

                while (!messagesToProcess.ProcessingComplete && sw.Elapsed < timeout)
                {
                    NewMessageEvent.Set();
                    Thread.Sleep(10);
                }

                sw.Stop();

                return messagesToProcess.ProcessingComplete;
            }
            #endregion

            #region Thread Methods
            private static void ThreadMainMethod()
            {
                                                  // 0                1
                var waitHandles = new WaitHandle[] { NewMessageEvent, ShutdownEvent };
                var done = false;
                while (done == false)
                {
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (WaitHandle.WaitAny(waitHandles))
                    {
                        case 0: // NewMessageEvent
                            {
                                ProcessableQueue messagesToProcess;
                                var newQueue = new ProcessableQueue(QueueStartSize);

                                lock (QueueSyncObject)
                                {
                                    messagesToProcess = _messageQueue;
                                    _messageQueue = newQueue;
                                }

                                SendMessagesToWriters(messagesToProcess);

                                messagesToProcess.ProcessingComplete = true;
                            }
                            break;
                        case 1: // ShutdownEvent
                            done = true;
                            break;
                    }
                }
                ShutdownAckEvent.Set();
            }
            private static void SendMessagesToWriters(IEnumerable<LogEntry> messagesToProcess)
            {
                foreach (var entry in messagesToProcess)
                {
                    if (WriterCount == 0)
                    {
                        entry.Message = $"There is no log writer subscribed to the log : {entry.Message}";
                        WriteSystem(entry);
                    }

                    lock (LogWriters)
                    {
                        foreach (var writer in LogWriters)
                        {
                            try
                            {
                                writer.Write(entry);
                            }
                            catch (Exception e)
                            {
                                entry.Message = $"Error writing message [Logger: {writer.GetType().FullName}, Exception: {e}] : {entry.Message}";
                                WriteSystem(entry);
                            }
                        }
                    }
                }
            }
            #endregion
        }
    }
}
