using CCC.CAS.Workflow2Messages.Models;
using System.Threading.Tasks;
using CCC.CAS.API.Common.Models;
using Microsoft.Extensions.Logging;
using MassTransit;
using CCC.CAS.Workflow2Service.Interfaces;
using System;
using System.Collections.Generic;
using CCC.CAS.API.Common.Logging;

namespace CCC.CAS.Workflow2Service.Repositories
{

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
    /// <summary>
    /// repository for service that hits its domain database
    /// </summary>
    class Workflow2Repository : IWorkflow2Repository
    {
        private readonly ILogger<Workflow2Repository> _logger;
        // TODO: inject you database/storage here
        public Workflow2Repository(ILogger<Workflow2Repository> logger)
        {
            _logger = logger;
        }

        public async Task DeleteWorkflow2Async(CallerIdentity identity, string workflow2Id)
        {
            if (string.IsNullOrEmpty(workflow2Id)) { throw new ArgumentNullException(nameof(workflow2Id)); }
            if (identity == null) { throw new ArgumentNullException(nameof(identity)); }

            // TODO add your code to delete here
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public async Task<List<Workflow2>> GetWorkflow2ByNameAsync(CallerIdentity identity, string name)
        {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(nameof(name)); }
            if (identity == null) { throw new ArgumentNullException(nameof(identity)); }

            // TODO add your code to get here
            _logger.LogInformation("TODO, create message and consumer to call this");
            return await Task.FromResult(new List<Workflow2>()).ConfigureAwait(false);
        }

        public async Task<Workflow2?> GetWorkflow2Async(CallerIdentity identity, string workflow2Id)
        {
            if (string.IsNullOrEmpty(workflow2Id)) { throw new ArgumentNullException(nameof(workflow2Id)); }
            if (identity == null) { throw new ArgumentNullException(nameof(identity)); }

            // TODO add your code to get here
            return await Task.FromResult<Workflow2?>(new Workflow2() { Name = "Fred", Id = workflow2Id }).ConfigureAwait(false);
        }

        public async Task<Workflow2?> SaveWorkflow2Async(CallerIdentity identity, Workflow2 item, Guid? correlationId)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }
            if (identity == null) { throw new ArgumentNullException(nameof(identity)); }
            if (string.IsNullOrWhiteSpace(identity.Username)) { throw new ArgumentException("identity.Username must be set"); }

            bool isAdd = string.IsNullOrEmpty(item.Id);
            if (isAdd)
            {
                item.Id = NewId.NextGuid().ToString();
            }

            // TODO add your code to save here
            return await Task.FromResult<Workflow2?>(item).ConfigureAwait(false);
        }
    }
#pragma warning restore CA1812

}
