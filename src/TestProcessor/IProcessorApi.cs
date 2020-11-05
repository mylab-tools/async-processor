using MyLab.ApiClient;
using MyLab.StatusProvider;

namespace TestProcessor
{
    [Api]
    public interface IProcessorApi
    {
        [Get("status")]
        ApplicationStatus GetStatus();
    }
}
