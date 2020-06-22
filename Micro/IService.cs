using System;
using System.Collections;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Micro
{
    public interface IService
    {
        void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
        }

        IEnumerable<ServerServiceDefinition> GetGrpcServicesDefinitions(IServiceProvider serviceProvider)
        {
            return new ServerServiceDefinition[0];
        }
    }
}
