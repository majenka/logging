using Majenka.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class Service
    {
        private IHostApplicationLifetime hostLifetime;
        private ILogger<Service> logger;

        public Service(IHostApplicationLifetime hostLifetime, ILogger<Service> logger)
        {
            this.hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Run()
        {
            logger.LogInformation("Running service");

            // Put your code here
            #region sample code

            for (int i = 0; i < 1000; i++)
            {
                logger.LogInformation($"This is line # {i}");
            }

            logger.LogInformation("Before scope");

            using (logger.BeginScope("Some name"))
            using (logger.BeginScope(42))
            using (logger.BeginScope("Formatted {WithValue}", 12345))
            using (logger.BeginScope(new Dictionary<string, object> { ["ViaDictionary"] = 100 }))
            {
                logger.LogInformation("Hello from the Index!");
                logger.LogDebug("Hello is done");
            }

            logger.LogInformation("After scope");

            #endregion

            logger.LogInformation("Finished running service");

            hostLifetime.StopApplication();
        }
    }
}
