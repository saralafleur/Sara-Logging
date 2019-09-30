using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sara.Common.Extension;
using Sara.Logging.Configuration;

namespace Sara.Logging.Writers
{
    public class ConsoleWriter : ILogWriter
    {
        #region Construction

        static ConsoleWriter()
        {
            PrependMessages = false;
        }

        #endregion Construction

        public static bool PrependMessages { get; set; }

        #region ILogWriter Members

        public bool UseBackgroundThreadQueue { private set; get; }

        public void Initialize(ILogWriterConfiguration configuration)
        {
            UseBackgroundThreadQueue = configuration.UseBackgroundTheadQueue;
        }

        //not used
        public void Purge() { }

        public void Write(LogEntry entry)
        {
            if (PrependMessages)
            {
                Console.Write(@"[ConsoleWriter] ");
            }

            var originalBackColor = Console.BackgroundColor;
            var originalForeColor = Console.ForegroundColor;

            // header color
            var logEntryForeTypeColor = default(ConsoleColor);
            var logEntryBackTypeColor = default(ConsoleColor);
            switch (entry.LogEntryType)
            {
                case LogEntryType.Debug:
                    logEntryBackTypeColor = ConsoleColor.Cyan;
                    logEntryForeTypeColor = ConsoleColor.Black;
                    break;
                case LogEntryType.Error:
                    logEntryBackTypeColor = ConsoleColor.Red;
                    logEntryForeTypeColor = ConsoleColor.Black;
                    break;
                case LogEntryType.Information:
                    logEntryBackTypeColor = ConsoleColor.Green;
                    logEntryForeTypeColor = ConsoleColor.Black;
                    break;
                case LogEntryType.Trace:
                    logEntryBackTypeColor = ConsoleColor.DarkGray;
                    logEntryForeTypeColor = ConsoleColor.Black;
                    break;
                case LogEntryType.Warning:
                    logEntryBackTypeColor = ConsoleColor.DarkYellow;
                    logEntryForeTypeColor = ConsoleColor.Black;
                    break;
            }

            Console.BackgroundColor = logEntryBackTypeColor;

            // time stamp
            ConsoleWrite(ConsoleColor.White, "<");
            ConsoleWrite(logEntryForeTypeColor, entry.TimeStamp.ToString("HH:mm:ss.fff"));
            ConsoleWrite(ConsoleColor.White, "> ");

            // log type
            var logType = entry.LogEntryType.ToString();
            ConsoleWrite(ConsoleColor.White, "(");
            ConsoleWrite(logEntryForeTypeColor, logType);
            ConsoleWrite(ConsoleColor.White, ")");
            Console.Write(string.Empty.PadRight(11 - logType.Length));

            Console.BackgroundColor = originalBackColor;

            // thread
            ConsoleWrite(ConsoleColor.White, " [");
            ConsoleWrite(ConsoleColor.Yellow, entry.ThreadId.ToString("00"));
            ConsoleWrite(ConsoleColor.White, "]");

            // class name
            if (!string.IsNullOrEmpty(entry.ClassName))
            {
                ConsoleWrite(ConsoleColor.Cyan, "  " + entry.ClassName);
            }

            // operation name
            if (!string.IsNullOrEmpty(entry.OperationName))
            {
                ConsoleWrite(ConsoleColor.Green, "  " + entry.OperationName);
            }

            Console.ForegroundColor = originalForeColor;
            Console.WriteLine();

            // message
            if (!string.IsNullOrEmpty(entry.Message))
            {
                ConsoleWrite(ConsoleColor.White, IndentText(entry.Message));
            }

            // exception
            if (entry.Exception != null)
            {
                ConsoleWrite(ConsoleColor.Red, IndentText(entry.Exception.ToString()));
            }

            if (entry.Message != null || entry.Exception != null)
            {
                Console.WriteLine();
            }
        }

        private static void ConsoleWrite(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }

        private static string IndentText(string value)
        {
            var textWidth = Console.BufferWidth - 4;
            var lines = value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var stringBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                var newLines = WrapText(line, textWidth);
                foreach (var newLine in newLines)
                {
                    stringBuilder.AppendLine("   " + newLine);
                }
            }

            return stringBuilder.ToString();
        }

        private static IEnumerable<string> WrapText(string line, int textWidth)
        {
            var originalLine = line.Split(new[] { " " }, StringSplitOptions.None);
            var wrappedLines = new List<string>();
            var actualLine = new StringBuilder();

            foreach (var word in originalLine.Where(w => w != string.Empty))
            {
                if (actualLine.Length + word.Length > textWidth)
                {
                    if (word.Length > textWidth)
                    {
                        var wordSegments = (actualLine + word).SplitInChunks(textWidth).ToList();
                        for (var i = 0; i < wordSegments.Count - 1; i++)
                        {
                            wrappedLines.Add(wordSegments[i]);
                        }

                        actualLine = new StringBuilder(wordSegments[wordSegments.Count - 1] + " ");
                    }
                    else
                    {
                        wrappedLines.Add(actualLine.ToString().TrimEnd());
                        actualLine = new StringBuilder(word + " ");
                    }
                }
                else
                {
                    actualLine.Append(word + " ");
                }
            }

            wrappedLines.Add(actualLine.ToString().TrimEnd());

            return wrappedLines;
        }

        #endregion

        public void Dispose()
        {
            Write(new LogEntry("Dispose Log Writer: " + GetType().Name));
        }
    }
}
