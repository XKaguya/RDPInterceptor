using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RDPInterceptor.API
{
    public class Logger
    {
        private static RichTextBox logRichTextBox;
        private static string logFilePath = "Info.log";
        private static readonly object lockObject = new object();

        public static string LogLevel { get; set; } = "Info";

        static Logger()
        {
            logRichTextBox = new RichTextBox();
            
            File.WriteAllText(logFilePath, string.Empty);
        }

        public static void SetLogLevel(string level)
        {
            if (level == "Debug" || level == "DEBUG")
            {
                LogLevel = level;
            }
            else if (level == "Info" || level == "INFO")
            {
                LogLevel = level;
            }
        }
        
        public static void SetLogTarget(RichTextBox richTextBox)
        {
            logRichTextBox = richTextBox;
        }
        
        public static void SetLogBackgroundColor(SolidColorBrush color)
        {
            logRichTextBox.Background = color;
        }

        public static void Log(string message)
        {
            if (logRichTextBox != null)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO]: {message}";
                LogAddLine(logMessage, Brushes.CornflowerBlue);
                
                WriteLogToFile(logMessage);
            }
        }

        public static void Error(string message)
        {
            if (logRichTextBox != null)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR]: {message}";
                LogAddLine(logMessage, Brushes.Red);
                
                WriteLogToFile(logMessage);
            }
        }
        
        public static void Debug(string message)
        {
            if (logRichTextBox != null)
            {
                if (LogLevel == "Debug" || LogLevel == "DEBUG")
                {
                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [DEBUG]: {message}";
                    LogAddLine(logMessage, Brushes.Chocolate);
                    
                    WriteLogToFile(logMessage);
                }
            }
        }

        private static void LogAddLine(string message, SolidColorBrush color)
        {
            logRichTextBox.Dispatcher.Invoke(() =>
            {
                Paragraph paragraph = new Paragraph(new Run(message));
                paragraph.Foreground = color;
                logRichTextBox.Document.Blocks.Add(paragraph);
            });
        }

        private static void WriteLogToFile(string message)
        {
            lock (lockObject)
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(message);
                }
            }
        }

        public static string GetLogs()
        {
            if (logRichTextBox.Document.Blocks != null)
            {
                string logs = logRichTextBox.Dispatcher.Invoke(() =>
                {
                    var text = new StringBuilder();
                    foreach (var block in logRichTextBox.Document.Blocks)
                    {
                        if (block is Paragraph paragraph)
                        {
                            foreach (var inline in paragraph.Inlines)
                            {
                                if (inline is Run run)
                                {
                                    text.AppendLine(run.Text);
                                }
                            }
                        }
                    }
            
                    return text.ToString();
                });

                return logs;
            }

            return null;
        }
    }
}
