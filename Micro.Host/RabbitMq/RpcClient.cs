using System;
using System.Threading.Tasks;
using dFakto.Queue;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Micro.Host.RabbitMq
{
    public class RpcClient : IRpcClient
    {
        private readonly IConnection _connection;
        private readonly PayloadSerializerFactory _payloadSerializer;
        private readonly IModel _channel;
        private string? _consumerTag;

        public RpcClient(IConnection connection, PayloadSerializerFactory serviceProvider)
        {
            _connection = connection;
            _payloadSerializer = serviceProvider;
            _channel = connection.CreateModel();
            Serializer = _payloadSerializer.GetSerializer(PayloadSerializerFactory.DefaultContentType);
        }

        public IPayloadSerializer Serializer { get; set; }

        public Task<Message<U>> SendCommand<T,U>(string commandQueueName,Message<T> command) where U : new()
        {
            var n = PublicationAddress.Parse(commandQueueName);

            lock (_channel)
            {
                _channel.ExchangeDeclare(exchange: n.ExchangeName, n.ExchangeType,false,true,null);

                var replyQueueName = _channel.QueueDeclare().QueueName;
                _channel.QueueDeclare(n.ExchangeName, durable: false, exclusive: false, autoDelete: true, arguments: null);

                TaskCompletionSource<Message<U>> result = new TaskCompletionSource<Message<U>>();

                var corrId = Guid.NewGuid().ToString();
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (s, e) =>
                {
                    if (e.BasicProperties.CorrelationId == corrId)
                    {
                        try
                        {
                            _channel.BasicCancel(_consumerTag);
                            
                            var ser = _payloadSerializer.GetSerializer(e.BasicProperties.ContentType);

                            var answer = new Message<U>(ser.Deserialize<U>(e.Body));
                            result.SetResult(answer);
                        }
                        catch (Exception exception)
                        {
                            result.SetException(exception);
                        }
                    }
                };
                _consumerTag = _channel.BasicConsume(queue: replyQueueName, autoAck: true, consumer: consumer);

                
                var props = _channel.CreateBasicProperties();
                props.ContentType = Serializer.ContentType;
                props.ReplyTo = replyQueueName;
                props.CorrelationId = corrId;
                props.Persistent = command.Persistent;
                
                
                _channel.BasicPublish(exchange: "", n.ExchangeName , basicProperties: props, body: Serializer.Serialize(command.Payload));

                return result.Task;
            }
        }


        public void Dispose()
        {
            _channel?.Dispose();
            _connection.Dispose();
        }
    }
}
