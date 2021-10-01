using CCC.CAS.API.AspNetCommon;
using CCC.CAS.API.AspNetCommon.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.CAS.Workflow2Service
{
    public class Startup : BaseStartup
    {
        public Startup(IConfiguration configuration) : base(configuration, apiVersion: "1", title: "Workflow2Service")
        {
        }

        protected override void AddMoreHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            // Add any of your additional health checks, or remove if none.
        }

    }
}
