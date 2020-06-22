using System;
using dFakto.Queue;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Micro.Host.RabbitMq
{
    public class RabbitMqConfig
    {
        public RabbitMqConfig()
        {
            HostName = "localhost";
            AutomaticRecoveryEnabled = true;
        }

        public string HostName { get; set; }
        public bool AutomaticRecoveryEnabled { get; set; }
        public string? Password { get; set; }
        public string? UserName { get; set; }
        public string VirtualHost { get; set; } = "/";
        public int Port { get; set; }
    }

    public static class RabbitMqExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            collection.AddTransient<IPayloadSerializer, GrpcPayloadSerializer>();

            collection.AddTransient<RabbitMqConfig>(x  =>
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
            
            collection.AddSingleton(x =>
            {
                var c = x.GetService<RabbitMqConfig>();

                var result = new ConnectionFactory
                {
                    Port = c.Port,
                    HostName = c.HostName,
                    VirtualHost = c.VirtualHost,
                    AutomaticRecoveryEnabled = c.AutomaticRecoveryEnabled
                };
                
                if (!string.IsNullOrEmpty(c.UserName))
                {
                    result.UserName = c.UserName;
                    result.Password = c.Password;
                }

                return result;
            } );

            collection.AddTransient<IProducer, Producer>();
            collection.AddTransient<IPublisher, Producer>();
            collection.AddTransient<IRpcClient, RpcClient>();
            collection.AddTransient<IRpcServer, RpcServer>();
            collection.AddTransient<ISubscriber, Subscriber>();
            collection.AddTransient<IConsumer, WorkQueueConsumer>();
            collection.AddTransient(x => x.GetService<ConnectionFactory>().CreateConnection());
            collection.AddTransient<PayloadSerializerFactory>();
           
            return collection;
        }
    }
}
