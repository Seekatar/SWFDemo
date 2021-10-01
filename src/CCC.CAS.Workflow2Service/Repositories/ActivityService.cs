using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using CCC.CAS.API.Common.Models;
using CCC.CAS.API.Common.Storage;
using CCC.CAS.Workflow2Messages.Models;
using CCC.CAS.Workflow2Service.Interfaces;
using CCC.CAS.Workflow2Service.Services;
using CCC.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCC.CAS.Workflow2Service.Repositories
{
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
    /// <summary>
    /// repository for the gateway-type service that sends a command to another service
    /// </summary>
    internal class ActivityService : IActivityService
    {
        private readonly StorageConfiguration _config;
        private readonly ILogger<ActivityService> _logger;

        public ActivityService(IOptions<StorageConfiguration> config, ILogger<ActivityService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public async Task StartWorkflow(int scenario)
        {
            ExecuteScenario(new WorkDemoActivityState { ScenarioNumber = scenario });
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public void ExecuteScenario(
                    WorkDemoActivityState workDemoActivityState)
        {
            Task.Run(async () =>
            {
                var startWorkflowExecutionRequest = new StartWorkflowExecutionRequest
                {
                    Domain = "test-jmw",
                    WorkflowId = Guid.NewGuid().ToString(),
                    WorkflowType = new WorkflowType
                    {
                        Name = "StDemoWorkflow",
                        Version = "1.0"
                    },
                    Input =
                            JsonSerializer.Serialize(workDemoActivityState),
                    ExecutionStartToCloseTimeout = "30",
                    TagList = new List<string>
                            { $"Scenario # {workDemoActivityState.ScenarioNumber}" }
                };

                using var amazonSimpleWorkflowClient = new AmazonSimpleWorkflowClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region));

                try
                {
                    _logger.LogDebug($"Starting workflow for {workDemoActivityState.ScenarioNumber}");
                    await amazonSimpleWorkflowClient.StartWorkflowExecutionAsync(
                        startWorkflowExecutionRequest).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Starting workflow");
                }
            });
        }
    }

#pragma warning restore CA1812
}
