using System;
using System.Net.Http;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;

namespace SampleGrpcClient
{
    class Program
    {
        const string DefaultHost = "localhost";
        const int Port = 50051;

        
        static void Main(string[] args)
        {
            var httpHandler = new HttpClientHandler();
// Return `true` to allow certificates that are untrusted/invalid
            httpHandler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var channel = GrpcChannel.ForAddress("https://localhost:50051",
                new GrpcChannelOptions { HttpHandler = httpHandler });
            // var client = new Greet.GreeterClient(channel);
            //
            //
            // var channelTarget = $"{DefaultHost}:{Port}";
            //
            // var channel = new Channel(channelTarget, ChannelCredentials.Insecure);
            
            Health.HealthClient cc = new Health.HealthClient(channel);
            var c = cc.Check(new HealthCheckRequest());
            
            Console.WriteLine(c.Status);
            
            // Create a channel
            // Hello.HelloClient client = new Hello.HelloClient(channel);
            //
            // for (int i = 0; i < 10; i++)
            // {
            //     var reply = client.SayHello(new HelloRequest{Name = "Vincent"});
            //
            //     Console.WriteLine(reply.Message);
            // }
        }
    }
}