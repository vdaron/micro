using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using dFakto.Queue;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Micro.HelloWorld.BackgroundServices
{
    public class SomeBackgroundService : IBackgroundProcess
    {
        private readonly ILogger<SomeBackgroundService> _logger;
        private readonly IPublisher _publisher;
        private readonly IConsumer _consumer;
        private readonly IServiceDiscovery _serviceDiscovery;

        public SomeBackgroundService(ILogger<SomeBackgroundService> logger, IPublisher publisher, IConsumer consumer, IServiceDiscovery serviceDiscovery)
        {
            _logger = logger;
            _publisher = publisher;
            _consumer = consumer;
            _serviceDiscovery = serviceDiscovery;
            _logger.LogInformation("Creating Background Service");
        }
        
        public async Task Run(CancellationToken token)
        {
            
            _logger.LogInformation("Starting Service ...");
            _consumer.Start<HelloRequest>("topic://test/*","micro:samplebackground:samplequeue", MessageReceived);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var helloInfo = await _serviceDiscovery.Discover("helloworld");

                    if (!string.IsNullOrEmpty(helloInfo.host) && helloInfo.port > 0)
                    {
                        var channelTarget = $"{helloInfo.host}:{helloInfo.port}";
                        var channel = new Channel(channelTarget, ChannelCredentials.Insecure);

                        var client = new Hello.HelloClient(channel);
                        var rr = await client.SayHelloAsync(new HelloRequest
                        {
                            Name = "Vincent"
                        });

                        _publisher.Send("topic://test/test",
                            new Message<HelloRequest>(new HelloRequest {Name = rr.Message}));
                    }

                    _logger.LogInformation("Will wait for 5 seconds");
                    await Task.Delay(TimeSpan.FromMilliseconds(5000), token);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Background Process cancelled");
            }
            finally
            {
                _logger.LogInformation("Stopping consumer");
                _consumer.Stop();
            }
            _logger.LogInformation("Background Process stopped");
        }

        private void MessageReceived(Message<HelloRequest> request)
        {
            _logger.LogInformation("Request Received ! ("+ request.Payload.Name+")");
        }
    }
}