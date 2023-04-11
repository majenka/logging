using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
