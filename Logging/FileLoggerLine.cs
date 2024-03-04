using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majenka.Logging
{
    internal class FileLoggerLine
    {
        public FileLoggerOptions Options { get; }
        public string Message { get; }

        public FileLoggerLine(FileLoggerOptions options, string message)
        {
            Options = options;
            Message = message;
        }
    }
}
