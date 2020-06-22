using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace dFakto.Queue.RabbitMQ
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
        public static IServiceCollection AddRabbitMq(this IServiceCollection collection, RabbitMqConfig config)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (config == null) throw new ArgumentNullException(nameof(config));

            // collection.AddSingleton<RabbitMqConfig>(config);

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
