using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using dFakto.Queue;
using Google.Protobuf;
using Grpc.Core;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Micro.Host_old
{
    public class ServiceHost
    {
        private readonly ILogger<ServiceHost> _logger;
        private readonly ServiceHostConfig _config;
        private readonly IServiceProvider _baseServiceProvider;
        private Server? _grpcServer;
        private IServiceProvider? _serviceProvider;
        private Task[] _backgroundProcesses = new Task[0];
        private CancellationTokenSource? _backgroundProcessCancellationTokenSource;
        private readonly List<PluginLoader> _pluginLoaders = new List<PluginLoader>();

        public ServiceHost(IServiceProvider serviceProvider, ServiceHostConfig config)
        {
            _logger = serviceProvider.GetService<ILogger<ServiceHost>>();
            _baseServiceProvider = serviceProvider;
            _config = config;
        }
        
        public async Task Start()
        {
            if(_serviceProvider != null || _grpcServer != null)
                throw new ApplicationException("Already Started");

            _pluginLoaders.AddRange(GetPluginLoaders());
            
            _serviceProvider = GetServiceProvider();
            _logger.LogInformation("Starting GRPC server in port " + _config.GrpcServerPort);
            _grpcServer = BuildGrpcServer(_serviceProvider, _config.GrpcServerHost!, _config.GrpcServerPort);
            _grpcServer.Start();

            
            _logger.LogInformation("Starting Background Processes");
            await StartBackgroundProcesses();
        }

        public async Task Stop()
        {
            if(_serviceProvider == null ||
               _grpcServer == null || 
               _backgroundProcessCancellationTokenSource == null)
                throw new ApplicationException("Already Stopped");
            
            _logger.LogInformation("Stopping Background Processes");
            _backgroundProcessCancellationTokenSource.Cancel();
            
            _logger.LogInformation("Stopping GrpcServer");
            await StopGrpcServer();
            
            _logger.LogInformation("Wait for remaining Background Processes");
            Task.WaitAll(_backgroundProcesses);

            _backgroundProcesses = new Task[0];
            _serviceProvider = null;

            
            _logger.LogInformation("Unloading Plugins");
            foreach (var plugin in _pluginLoaders)
            {
                plugin.Dispose();
            }
            _pluginLoaders.Clear();
            
            _logger.LogInformation("ServiceHost stopped");
        }

        private async Task StopGrpcServer()
        {
            var shutdown = _grpcServer!.ShutdownAsync();

            shutdown.Wait(TimeSpan.FromSeconds(15));

            if (!shutdown.IsCompleted)
            {
                await _grpcServer.KillAsync();
            }
            
            _grpcServer = null;
        }

        public async Task Restart()
        {
            await Stop();
            await Start();
        }

        private async Task StartBackgroundProcesses()
        {
            _backgroundProcessCancellationTokenSource = new CancellationTokenSource();

            var t = new List<Task>();
            foreach (IBackgroundProcess process in _serviceProvider.GetServices<IBackgroundProcess>())
            {
                t.Add(Task.Run(() => process.Run(_backgroundProcessCancellationTokenSource.Token)));
            }

            _backgroundProcesses = t.ToArray();
        }
        
        private IServiceProvider GetServiceProvider()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var pluginsDir = AppContext.BaseDirectory;
            
            foreach (var dll in _pluginLoaders)
            {
                var assembly = dll.LoadDefaultAssembly();
                foreach (var type in assembly.GetTypes()
                    .Where(x => !x.IsAbstract && typeof(IService).IsAssignableFrom(x)))
                {
                    var service = (IService?) Activator.CreateInstance(type);
                    if (service != null)
                    {
                        serviceCollection.AddSingleton(service);
                        //TODO: Conf
                        service.ConfigureServices(new ConfigurationRoot(new List<IConfigurationProvider>()), serviceCollection);     
                    }
                }
            }

            serviceCollection.AddTransient(x => _baseServiceProvider.GetService<ILoggerFactory>());
            serviceCollection.AddTransient(x => _baseServiceProvider.GetService<ILoggerProvider>());
            serviceCollection.AddTransient(x => _baseServiceProvider.GetService<IPublisher>());
            serviceCollection.AddTransient(x => _baseServiceProvider.GetService<ISubscriber>());
            serviceCollection.AddTransient(x => _baseServiceProvider.GetService<IConsumer>());
            serviceCollection.AddTransient(x => _baseServiceProvider.GetService<IServiceDiscovery>());

            return serviceCollection.BuildServiceProvider();
        }
        
        private static Server BuildGrpcServer(IServiceProvider serviceProvider, string host, int port)
        {
            var server = new Server
            {
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };

            // Register Grpc Services
            foreach (IService service in serviceProvider.GetServices<IService>())
            {
                foreach (var grpc in service.GetGrpcServicesDefinitions(serviceProvider))
                {
                    server.Services.Add(grpc);
                }
            }

            return server;
        }
        
        private List<PluginLoader> GetPluginLoaders()
        {
            var loaders = new List<PluginLoader>();

            // create plugin loaders
            var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");

            _logger.LogInformation($"Loading Plugins from directory '{pluginsDir}'");
            if (Directory.Exists(pluginsDir))
            {
                foreach (var dir in Directory.GetDirectories(pluginsDir))
                {
                    var dirName = Path.GetFileName(dir);
                    var pluginDll = Path.Combine(dir, dirName + ".dll");
                    if (File.Exists(pluginDll))
                    {
                        _logger.LogInformation($"Loading Plugin '{pluginDll}'");
                        
                        var loader = PluginLoader.CreateFromAssemblyFile(
                            pluginDll,
                            true,
                            new[]
                            {
                                //Logging
                                typeof(ILogger),
                                typeof(ILogger<>),
                                typeof(ILoggerFactory),

                                //Dependency Injection
                                typeof(IServiceCollection),
                                typeof(IServiceProvider),
                                
                                //Grpc
                                typeof(IMessage),

                                // Async Rabbitmq
                                typeof(IPublisher),
                                typeof(ISubscriber),
                                typeof(IConsumer),
                                typeof(IServiceDiscovery)
                            });
                        loaders.Add(loader);
                    }
                }
            }

            return loaders;
        }
    }
}