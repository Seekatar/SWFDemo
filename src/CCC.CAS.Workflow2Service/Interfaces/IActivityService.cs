using CCC.CAS.API.Common.Models;
using CCC.CAS.Workflow2Messages.Models;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Interfaces
{
    public interface IActivityService
    {
        Task StartWorkflow(int scenario);
    }

}
