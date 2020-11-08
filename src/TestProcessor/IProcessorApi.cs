using System.Threading.Tasks;
using MyLab.ApiClient;
using MyLab.StatusProvider;

namespace TestProcessor
{
    [Api]
    public interface IProcessorApi
    {
        [Get("status")]
        Task<ApplicationStatus> GetStatus();
    }
}
