using System;
using System.Collections.Generic;
using Grpc.Core;
using Micro.HelloWorld.BackgroundServices;
using Micro.HelloWorld.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Micro.HelloWorld
{
    public class HelloWorldService: IService
    {
        public void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddTransient<HelloServiceImpl>();
            services.AddSingleton<IBackgroundProcess,SomeBackgroundService>();
        }

        public IEnumerable<ServerServiceDefinition> GetGrpcServicesDefinitions(IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService<HelloServiceImpl>();
            return new[] {Hello.BindService(service)};
        }
    }
}