using CCC.CAS.API.Common.Models;
using CCC.CAS.Workflow2Messages.Models;
using System;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Interfaces
{
    public interface IWorkflow2Repository
    {
        Task<Workflow2?> SaveWorkflow2Async(CallerIdentity identity, Workflow2 item, Guid? correlationId);
        Task<Workflow2?> GetWorkflow2Async(CallerIdentity identity, string workflow2Id);
        Task DeleteWorkflow2Async(CallerIdentity identity, string workflow2Id);
    }

}
