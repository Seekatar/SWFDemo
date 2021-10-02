using System;
using CCC.CAS.API.Common.Installers;
using CCC.CAS.API.Common.Logging;
using CCC.CAS.API.Common.Storage;
using CCC.CAS.Workflow2Service.Interfaces;
using CCC.CAS.Workflow2Service.Repositories;
using CCC.CAS.Workflow2Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCC.CAS.Workflow2Service.Installers
{
    public class RepositoryInstaller : IInstaller
    {
        private readonly ILogger<RepositoryInstaller> _debugLogger;

        public RepositoryInstaller()
        {
            _debugLogger = DebuggingLoggerFactory.Create<RepositoryInstaller>();
        }

        public void InstallServices(IConfiguration configuration, IServiceCollection services)
        {
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }

            try
            {
                services.AddHostedService<AwsWorkflowDeciderService>();
                services.AddHostedService<AwsWorkflowActivityService>();

                var section = configuration.GetSection(AwsWorkflowConfiguration.DefaultConfigName);
                services.AddOptions<AwsWorkflowConfiguration>()
                         .Bind(section)
                         .ValidateDataAnnotations();

                services.AddSingleton<IActivityService,ActivityService>();
                services.AddSingleton<AwsWorkflowConfig>();

                _debugLogger.LogDebug("Services added.");
            }
            catch (Exception ex)
            {
                _debugLogger.LogError(ex, "Exception occurred while adding DB services.");
            }
        }
    }
}
