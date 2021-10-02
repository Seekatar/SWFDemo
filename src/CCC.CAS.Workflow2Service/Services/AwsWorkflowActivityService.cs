using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using CCC.CAS.API.Common.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    public class AwsWorkflowActivityService : BackgroundService
    {
        private ILogger<AwsWorkflowActivityService> _logger;
        private readonly AwsWorkflowConfiguration _config;

        public AwsWorkflowActivityService(IOptions<AwsWorkflowConfiguration> config, ILogger<AwsWorkflowActivityService> logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var swfClient = new AmazonSimpleWorkflowClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region)))
                {
                    _logger.LogDebug($"{nameof(AwsWorkflowActivityService)} polling");
                    var activityTask = await Poll(swfClient).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(activityTask?.TaskToken))
                        continue;

                    var workDemoActivityState = JsonSerializer
                        .Deserialize<WorkDemoActivityState>(activityTask.Input);

                    if (workDemoActivityState != null)
                    {
                        workDemoActivityState = ProcessTask(
                            activityTask.ActivityType.Name, workDemoActivityState);

                        CompleteTask(swfClient, activityTask.TaskToken, workDemoActivityState);
                    }
                }

                Thread.Sleep(100);
            }
        }

        private static WorkDemoActivityState ProcessTask(string activityTypeName, WorkDemoActivityState workDemoActivityState)
        {
            WorkDemo.WasteTime(
                int.Parse(activityTypeName.Last().ToString(), CultureInfo.InvariantCulture),
                workDemoActivityState);

            return workDemoActivityState;
        }

        private void CompleteTask(
            AmazonSimpleWorkflowClient amazonSimpleWorkflowClient,
            string taskToken, WorkDemoActivityState workDemoActivityState)
        {
            var respondActivityTaskCompletedRequest =
                new RespondActivityTaskCompletedRequest()
                {
                    Result = JsonSerializer.Serialize(workDemoActivityState),
                    TaskToken = taskToken
                };

            try
            {
                amazonSimpleWorkflowClient.RespondActivityTaskCompletedAsync(respondActivityTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{workDemoActivityState} task complete failed: {ex}");
            }
        }

        private async Task<ActivityTask> Poll(
                AmazonSimpleWorkflowClient amazonSimpleWorkflowClient)
        {
            var pollForActivityTaskRequest = new PollForActivityTaskRequest
            {
                Domain = _config.Domain,
                TaskList = new TaskList { Name = _config.DefaultTaskList }
            };

            var pollForActivityTaskResponse = await amazonSimpleWorkflowClient
                .PollForActivityTaskAsync(pollForActivityTaskRequest).ConfigureAwait(false);

            return pollForActivityTaskResponse.ActivityTask;
        }
    }
}

