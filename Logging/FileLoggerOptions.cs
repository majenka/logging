using Microsoft.Extensions.Logging;

#nullable disable

namespace Majenka.Logging
{
    public class FileLoggerOptions
    {
        public long MaxFileSize { get; set; }
        public int MaxRetainedFiles { get; set; }
        public string Path { get; set; }
        public LogLevel MinLogLevel { get; set; }
        public bool LogDate { get; set; }
        public string DateFormat { get; set; }
    }
}
