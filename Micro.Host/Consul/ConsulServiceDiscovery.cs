using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;

namespace Micro.Host.Consul
{
    public class ConsulServiceDiscovery : IServiceDiscovery
    {
        public ConsulServiceDiscovery()
        {
        }


        public async Task<(string? host, int? port)> Discover(string serviceName)
        {
            var lookup = new LookupClient(IPAddress.Loopback,8600);

            var r = await lookup.QueryAsync($"{serviceName}.service.consul",QueryType.SRV);
            
            return (r.Additionals.ARecords().FirstOrDefault()?.Address.ToString(),
                    (int?)(r.Answers.SrvRecords().FirstOrDefault()?.Port));
        }
    }
}