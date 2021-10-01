﻿using CCC.CAS.API.Common.Storage;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleWorkflow;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Amazon;
using System.Text.Json;
using Amazon.SimpleWorkflow.Model;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CCC.CAS.Workflow2Service.Services
{

    public static class WorkDemo
    {
        public static void WasteTime(
            int workItemId, WorkDemoActivityState workDemoActivityState)
        {
            if (workDemoActivityState == null) throw new ArgumentNullException(nameof(workDemoActivityState));

            workDemoActivityState.WorkCompleted.Add(workItemId);
            Thread.Sleep(1000);
        }
    }

    public class WorkDemoActivityState
    {
        public DateTime EventTimestamp { get; set; }
        public EventType EventType { get; set; } = EventType.ActivityTaskCanceled;
        public int ScenarioNumber { get; set; }
#pragma warning disable CA2227 // Collection properties should be read only
        public List<int> WorkRequested { get; init; } = new List<int>();
        public List<int> WorkCompleted { get; init; } = new List<int>();
#pragma warning restore CA2227 // Collection properties should be read only

        public override string ToString()
        {
            return $"ScenarioNumber {ScenarioNumber}: " +
                   $"Work Requested: {string.Join("|", WorkRequested.ToArray())} " +
                   $"Work Completed: {string.Join("|", WorkCompleted.ToArray())}";
        }
    }

    public class AwsWorkflowDeciderService : BackgroundService
    {
        private readonly StorageConfiguration _config;
        //private readonly IAmazonSimpleWorkflow _client;
        private readonly ILogger<AwsWorkflowDeciderService> _logger;

        public AwsWorkflowDeciderService(IOptions<StorageConfiguration> config, ILogger<AwsWorkflowDeciderService> logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            _config = config.Value;
            //_client = new AmazonSimpleWorkflowClient(_config.AccessKey, _config.SecretKey,RegionEndpoint.GetBySystemName(_config.Region));
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var swfClient = new AmazonSimpleWorkflowClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region)))
                {
                    var decisionTask = await Poll(swfClient).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(decisionTask?.TaskToken))
                    {
                        var decisions = await CreateDecisionList(decisionTask).ConfigureAwait(false);

                        await CompleteDecisionTasks(swfClient, decisionTask.TaskToken, decisions).ConfigureAwait(false);
                    }
                }
                Thread.Sleep(100);
            }
        }

        private async Task CompleteDecisionTasks(AmazonSimpleWorkflowClient amazonSimpleWorkflowClient, string taskToken, List<Decision> decisions)
        {
            var respondDecisionTaskCompletedRequest =
                new RespondDecisionTaskCompletedRequest()
                {
                    Decisions = decisions,
                    TaskToken = taskToken
                };

            try
            {
                await amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(respondDecisionTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{taskToken} decision complete failed: {ex}");
            }
        }

        private async Task<WorkDemoActivityState?> GetLastWorkDemoActivityState(
            DecisionTask decisionTask)
        {
            var workDemoActivityStates =
                new List<WorkDemoActivityState>();
            var timerWorkDemoActivityStates =
                new Dictionary<string, WorkDemoActivityState>();

            using (var swfClient = new AmazonSimpleWorkflowClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region)))
            {
                var historyIterator = new HistoryIterator(
                    swfClient, decisionTask); // , "test-jmw", "defaultTaskList");

                await foreach (var historyEvent in historyIterator)
                {
                    if (historyEvent.EventType == EventType.WorkflowExecutionStarted)
                    {
                        var workDemoActivityState = JsonSerializer
                            .Deserialize<WorkDemoActivityState>(
                                historyEvent.WorkflowExecutionStartedEventAttributes.Input);

                        if (workDemoActivityState != null)
                        {
                            workDemoActivityStates.Add(new WorkDemoActivityState
                            {
                                EventTimestamp = historyEvent.EventTimestamp,
                                EventType = EventType.WorkflowExecutionStarted,
                                ScenarioNumber = workDemoActivityState.ScenarioNumber
                            });
                        }
                    }

                    if (historyEvent.EventType == EventType.ActivityTaskCompleted)
                    {
                        var workDemoActivityState = JsonSerializer
                            .Deserialize<WorkDemoActivityState>(
                                historyEvent.ActivityTaskCompletedEventAttributes.Result);

                        if (workDemoActivityState != null)
                        {
                            workDemoActivityStates.Add(new WorkDemoActivityState
                            {
                                EventTimestamp = historyEvent.EventTimestamp,
                                EventType = EventType.ActivityTaskCompleted,
                                ScenarioNumber = workDemoActivityState.ScenarioNumber,
                                WorkRequested = workDemoActivityState.WorkRequested,
                                WorkCompleted = workDemoActivityState.WorkCompleted
                            });
                        }
                    }
                    if (historyEvent.EventType == EventType.TimerStarted)
                    {
                        var workDemoActivityState = JsonSerializer
                            .Deserialize<WorkDemoActivityState>(
                                historyEvent.TimerStartedEventAttributes.Control);

                        if (workDemoActivityState != null)
                        {
                            timerWorkDemoActivityStates.Add(
                            historyEvent.TimerStartedEventAttributes.TimerId,
                            new WorkDemoActivityState
                            {
                                EventTimestamp = historyEvent.EventTimestamp,
                                EventType = EventType.TimerStarted,
                                ScenarioNumber = workDemoActivityState.ScenarioNumber,
                                WorkRequested = workDemoActivityState.WorkRequested,
                                WorkCompleted = workDemoActivityState.WorkCompleted
                            });
                        }
                    }

                    if (historyEvent.EventType == EventType.TimerFired)
                    {
                        if (timerWorkDemoActivityStates.TryGetValue(
                            historyEvent.TimerFiredEventAttributes.TimerId, out var workDemoActivityState) && workDemoActivityState is not null)
                        {
                            workDemoActivityState!.EventType = EventType.TimerFired;
                            workDemoActivityStates.Add(workDemoActivityState);
                        }
                    }
                }
            }
            return workDemoActivityStates.OrderByDescending(x => x.EventTimestamp).FirstOrDefault();
        }

        private async Task<List<Decision>> CreateDecisionList(DecisionTask decisionTask)
        {
            var decisions = new List<Decision>();

            var workDemoActivityState = await GetLastWorkDemoActivityState(decisionTask).ConfigureAwait(false);

            if (workDemoActivityState == null) return decisions;

            switch (workDemoActivityState.ScenarioNumber)
            {
                case 1:

                    if (workDemoActivityState.WorkCompleted.Contains(4))
                    {
                        decisions.Add(CreateCompleteWorkflowDecision(workDemoActivityState));
                        return decisions;
                    }
                    if (!workDemoActivityState.WorkRequested.Contains(1))
                    {
                        workDemoActivityState.WorkRequested.Add(1);
                        decisions.Add(CreateDecisionActivity(workDemoActivityState, 1));
                        return decisions;
                    }
                    if (!workDemoActivityState.WorkRequested.Contains(2))
                    {
                        workDemoActivityState.WorkRequested.Add(2);
                        decisions.Add(CreateDecisionActivity(
                            workDemoActivityState, 2));

                        workDemoActivityState.WorkRequested.Add(3);
                        decisions.Add(CreateDecisionActivity(
                            workDemoActivityState, 3));

                        return decisions;
                    }
                    if (workDemoActivityState.WorkRequested.Contains(2))
                    {
                        workDemoActivityState.WorkRequested.Add(4);
                        decisions.Add(CreateDecisionActivity(workDemoActivityState, 4));
                        return decisions;
                    }
                    break;

                case 2:

                    var activityOneCount = workDemoActivityState
                        .WorkRequested.Count(x => x.Equals(1));

                    switch (activityOneCount)
                    {
                        case 0:

                            workDemoActivityState.WorkRequested.Add(1);
                            decisions.Add(CreateDecisionActivity(
                                workDemoActivityState, 1));
                            return decisions;
                        case 1:

                            if (workDemoActivityState.EventType == EventType.TimerFired)
                            {
                                workDemoActivityState.WorkRequested.Add(1);
                                decisions.Add(CreateDecisionActivity(
                                    workDemoActivityState, 1));
                            }
                            else
                            {
                                decisions.Add(CreateTimerDecision(
                                    workDemoActivityState));
                            }
                            return decisions;
                    }
                    break;
            }
            return decisions;
        }

        private static Decision CreateDecisionActivity(
                   WorkDemoActivityState workDemoActivityState, int activitySuffix)
        {
            var decision = new Decision
            {
                DecisionType = DecisionType.ScheduleActivityTask,
                ScheduleActivityTaskDecisionAttributes =
                    new ScheduleActivityTaskDecisionAttributes
                    {
                        ActivityType = new ActivityType
                        {
                            Name = $"DemoActivity{activitySuffix}",
                            Version = "1.0"
                        },
                        ActivityId = $"{DateTime.Now.Ticks}_{activitySuffix}",
                        Input = JsonSerializer.Serialize(workDemoActivityState)
                    }
            };

            return decision;
        }

        private static Decision CreateTimerDecision(
            WorkDemoActivityState workDemoActivityState)
        {
            var decision = new Decision
            {
                DecisionType = DecisionType.StartTimer,
                StartTimerDecisionAttributes = new StartTimerDecisionAttributes
                {
                    StartToFireTimeout = "15",
                    TimerId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture),
                    Control = JsonSerializer.Serialize(workDemoActivityState)
                }
            };

            return decision;
        }

        private static Decision CreateCompleteWorkflowDecision(
            WorkDemoActivityState workDemoActivityState)
        {
            var decision = new Decision
            {
                DecisionType = DecisionType.CompleteWorkflowExecution,
                CompleteWorkflowExecutionDecisionAttributes =
                    new CompleteWorkflowExecutionDecisionAttributes
                    {
                        Result = workDemoActivityState.ToString()
                    }
            };

            return decision;
        }


        private static async Task<DecisionTask> Poll(AmazonSimpleWorkflowClient amazonSimpleWorkflowClient)
        {
            var pollForDecisionTaskRequest = new PollForDecisionTaskRequest
            {
                Domain = "test-jmw",
                TaskList = new TaskList { Name = "defaultTaskList" }
            };

            var pollForDecisionTaskResponse = await amazonSimpleWorkflowClient
                .PollForDecisionTaskAsync(pollForDecisionTaskRequest).ConfigureAwait(false);

            return pollForDecisionTaskResponse.DecisionTask;
        }
    }
}