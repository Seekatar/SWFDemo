using CCC.CAS.API.Common.Models;
using CCC.CAS.Workflow2Messages.Models;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Interfaces
{
    public interface IEchoRepository
    {
        Task<EchoResponse?> GetEchoAsync(CallerIdentity identity, string name);
    }

}
