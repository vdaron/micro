using System.Threading;
using System.Threading.Tasks;

namespace Micro
{
    public interface IBackgroundProcess
    {
        public Task Run(CancellationToken token);
    }
}