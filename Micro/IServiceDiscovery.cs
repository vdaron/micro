using System.Threading.Tasks;

namespace Micro
{
    public interface IServiceDiscovery
    {
        Task<(string? host, int? port)> Discover(string serviceName);
    }
}