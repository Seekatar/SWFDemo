using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    class HistoryIterator : IAsyncEnumerable<HistoryEvent>
    {
        DecisionTask lastResponse;
        private readonly string _domain;
        private readonly string _taskList;
        IAmazonSimpleWorkflow swfClient;

        /// <summary>
        /// Create a new HistoryIterator to enumerate the history events in the DecisionTask passed in.
        /// </summary>
        /// <param name="client">SWF client to use for getting next page of history events.</param>
        /// <param name="response">The decision task returned from the PollForDecisionTask call.</param>
        /// <param name="domain"></param>
        /// <param name="taskList"></param>
        public HistoryIterator(IAmazonSimpleWorkflow client, DecisionTask response, string domain, string taskList )
        {
            swfClient = client;
            lastResponse = response;
            _domain = domain;
            _taskList = taskList;
        }

        /// <summary>
        /// Creates an enumerator for the history events. Automatically retrieves pages of
        /// history from SWF.
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerator<HistoryEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            //Yield the history events in the current page of events
            foreach (HistoryEvent e in lastResponse.Events)
            {
                yield return e;
            }
            //If the NextPageToken is not null, get the next page of history events 
            while (!String.IsNullOrEmpty(lastResponse.NextPageToken))
            {
                List<HistoryEvent> events = await GetNextPage().ConfigureAwait(false);
                //Start yielding results from the next page of events
                foreach (HistoryEvent e in events)
                {
                    yield return e;
                }
            }
        }

        /// <summary>
        /// Helper method to call PollForDecisionTask with the NextPageToken
        /// to retrieve the next page of history events.
        /// </summary>
        /// <returns>List of history events received in the next page.</returns>
        async Task<List<HistoryEvent>> GetNextPage()
        {
            PollForDecisionTaskRequest request = new PollForDecisionTaskRequest()
            {
                Domain = _domain,
                NextPageToken = lastResponse.NextPageToken,
                TaskList = new TaskList()
                {
                    Name = _taskList
                }
            };

            //AmazonSimpleWorkflow client does exponential back off and retries by default.
            //We want additional retries for robustness in case of transient failures like throttling.
            int retryCount = 10;
            int currentTry = 1;
            bool pollFailed;
            do
            {
                pollFailed = false;
                try
                {
                    lastResponse = (await swfClient.PollForDecisionTaskAsync(request).ConfigureAwait(false)).DecisionTask;
                }
                catch (Exception ex)
                {
                    //Swallow exception and keep polling
                    Console.Error.WriteLine("Poll request failed with exception :" + ex);
                    pollFailed = true;
                }
            }
            while (pollFailed && ++currentTry <= retryCount);
            return lastResponse.Events;
        }
    }
}
