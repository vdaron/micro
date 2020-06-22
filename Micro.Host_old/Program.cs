using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using dFakto.Queue;
using dFakto.Queue.RabbitMQ;
using Grpc.Core;
using Micro.Host_old.Consul;
using Micro.Host_old.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Micro.Host_old
{
    class Program
    {
        const string Host = "0.0.0.0";
        const int Port = 5000;

        public static async Task Main(string[] args)
        {
            var serviceProvider = Configure();

            var host = serviceProvider.GetService<ServiceHost>();

            await host.Start();
     
            Console.WriteLine("GreeterServer listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            await host.Stop();
            
            Console.WriteLine("Shutting down...");
            Console.WriteLine("All Stopped, restarting after 20 seconds");

            await Task.Delay(TimeSpan.FromSeconds(10));

            await host.Start();
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            await host.Stop();
        }

        private static ServiceProvider Configure()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(x => { x.AddConsole(y => { y.Format = ConsoleLoggerFormat.Systemd; }); });

            serviceCollection.AddSingleton<ServiceHost>();
            serviceCollection.AddSingleton<ServiceHostConfig>(x => new ServiceHostConfig
            {
                GrpcServerHost = "localhost",
                GrpcServerPort = 3300
            });
            serviceCollection.AddTransient<IServiceDiscovery, ConsulServiceDiscovery>();

            serviceCollection.AddTransient<RabbitMqConfig>(x  =>
            {
                var rabbitImfo = x.GetService<IServiceDiscovery>().Discover("rabbitmq").Result;
                if (rabbitImfo.host == null || rabbitImfo.port == null)
                {
                    return new RabbitMqConfig();
                }
                return new RabbitMqConfig
                {
                    HostName = rabbitImfo.host,
                    Port = rabbitImfo.port.Value,
                    VirtualHost = "/",
                    AutomaticRecoveryEnabled = true
                };
            });
            
            serviceCollection.AddRabbitMq(new RabbitMqConfig
            {
                HostName = "localhost",
                VirtualHost = "/",
                AutomaticRecoveryEnabled = true
            });

            serviceCollection.AddTransient<IPayloadSerializer, GrpcPayloadSerializer>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }

        private static Task AwaitCancellation(CancellationToken token)
        {
            var taskSource = new TaskCompletionSource<bool>();
            token.Register(() => taskSource.SetResult(true));
            return taskSource.Task;
        }

        private static async Task RunServiceAsync(Server server, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Start server
            server.Start();

            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}