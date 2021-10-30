using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            logger.LogInformation("Running log service");
            
            // Run your code here

            logger.LogInformation("Finished running log service");
            
            hostLifetime.StopApplication();
        }
    }
}
