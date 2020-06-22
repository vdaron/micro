using System;
using dFakto.Queue;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Micro.Host.RabbitMq
{
    public class WorkQueueConsumer : IConsumer
    {
        private readonly IConnection _connection;
        private readonly PayloadSerializerFactory _payloadSerializer;
        private readonly IModel _channel;
        private EventingBasicConsumer? _consumer;
        private string? _consumerToken;

        public WorkQueueConsumer(IConnection connection, PayloadSerializerFactory serviceProvider)
        {
            _connection = connection;
            _payloadSerializer = serviceProvider;
            _channel = connection.CreateModel();
            Serializer = _payloadSerializer.GetSerializer(PayloadSerializerFactory.DefaultContentType);
        }
        
        public IPayloadSerializer Serializer { get; set; }

        public void Start<T>(string address,string queueName, Action<Message<T>> messageReceived) where T : new()
        {
            var n = PublicationAddress.Parse(address);

            Start(n.ExchangeType, n.ExchangeName, n.RoutingKey, queueName, messageReceived);
        }
        private void Start<T>(string exchangeType, string exchangeName, string routingKey, string queueName, Action<Message<T>> messageReceived) where T : new()
        {
            lock (_channel)
            {
                _channel.ExchangeDeclare(exchange: exchangeName, type: exchangeType);
                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                _channel.QueueBind(queueName, exchangeName, routingKey);

                _consumer = new EventingBasicConsumer(_channel);
                _consumer.Received += (s, e) =>
                {
                    try
                    {
                        var ser = _payloadSerializer.GetSerializer(e.BasicProperties.ContentType);

                        var msg = new Message<T>(ser.Deserialize<T>(e.Body)){
                            CorrelationId = e.BasicProperties.CorrelationId,
                            ReplyTo = e.BasicProperties.ReplyTo
                        };

                        messageReceived(msg);
                        
                        _channel.BasicAck(e.DeliveryTag, false);
                    }
                    catch (Exception)
                    {
                        _channel.BasicNack(e.DeliveryTag, false, true);
                    }
                };
                _consumerToken = _channel.BasicConsume(queueName, false, _consumer);
            }
        }

        public void Stop()
        {
            if (_consumerToken != null)
            {
                _channel.BasicCancel(_consumerToken);
                _consumerToken = null;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection.Dispose();
        }
    }
}
