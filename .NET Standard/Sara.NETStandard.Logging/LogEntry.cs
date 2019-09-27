using System;
using System.Runtime.Serialization;

namespace Sara.NETStandard.Logging
{
    public enum LogEntryType
    {
        Error,
        Warning,
        Trace,
        Debug,
        Information,
        SystemInfo,
        SystemWarning,
        SystemError
    }

    [DataContract]
    public class LogEntry
    {
        #region Properties

        [DataMember]
        public int ThreadId { get; set; }

        [DataMember]
        public DateTime TimeStamp { get; set; }

        [DataMember]
        public string ClassName { get; set; }

        [DataMember]
        public string OperationName { get; set; }

        [DataMember]
        public LogEntryType LogEntryType { get; set; }

        [DataMember]
        public Exception Exception { get; set; }

        [DataMember]
        public string Message { get; set; }

        #endregion

        public LogEntry(string message)
        {
            Message = message;
        }

        public LogEntry(int threadId, DateTime timestamp, string className, string operationName, LogEntryType filterPoint, Exception exception, string message)
        {
            ThreadId = threadId;
            TimeStamp = timestamp;
            ClassName = className;
            OperationName = operationName;
            LogEntryType = filterPoint;
            Exception = exception;
            Message = message;
        }

        public override string ToString()
        {
            return ToString("");
        }

        public string ToString(string dateTimeFormat)
        {
            var className = string.IsNullOrEmpty(ClassName) ? string.Empty : "Class: " + ClassName;
            var methodName = string.IsNullOrEmpty(OperationName) ? string.Empty : "Method: " + OperationName;
            var message = string.IsNullOrEmpty(Message) ? string.Empty : "Message: " + Message;
            var exception = Exception == null ? string.Empty : "Exception: " + Exception;

            return
                $"<{TimeStamp.ToString(dateTimeFormat)}> ({LogEntryType}) [{ThreadId}] {className} {methodName} {message} {exception}";
        }
    }
}
