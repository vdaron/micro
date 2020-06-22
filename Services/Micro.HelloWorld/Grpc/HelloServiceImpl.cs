using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Micro.HelloWorld.Grpc
{
    public class HelloServiceImpl : Hello.HelloBase
    {
        private readonly ILogger<HelloServiceImpl> _logger;

        public HelloServiceImpl(ILogger<HelloServiceImpl> logger)
        {
            _logger = logger;
        }
        
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Calling SayHello !");
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}