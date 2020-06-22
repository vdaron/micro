﻿using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace dFakto.Queue.RabbitMQ
{
    public class RpcServer : IRpcServer
    {
        private readonly IConnection _connection;
        private readonly PayloadSerializerFactory _payloadSerializer;
        private readonly IModel _channel;
        private string? _consumerToken;

        public RpcServer(IConnection connection, PayloadSerializerFactory payloadSerializer)
        {
            _connection = connection;
            _payloadSerializer = payloadSerializer;
            _channel = connection.CreateModel();
            Serializer = _payloadSerializer.GetSerializer(PayloadSerializerFactory.DefaultContentType);
        }
        
        public IPayloadSerializer Serializer { get; set; }
        
        public void Start<T,U>(string queueName, Func<Message<T>, Message<U>> commandReceived) where T : new()
        {
            var n = PublicationAddress.Parse(queueName);

            _channel.QueueDeclare(n.ExchangeName, durable: false, exclusive: false, autoDelete: true, arguments: null);
            _channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (s, e) =>
            {
                try
                {
                    try
                    {
                        var ser = _payloadSerializer.GetSerializer(e.BasicProperties.ContentType);

                        var command = new Message<T>(ser.Deserialize<T>(e.Body))
                        {
                            Persistent = e.BasicProperties.Persistent,
                            CorrelationId = e.BasicProperties.CorrelationId,
                            ReplyTo = e.BasicProperties.ReplyTo
                        };

                        var response = commandReceived(command);

                        var replyProps = _channel.CreateBasicProperties();
                        replyProps.ContentType = Serializer.ContentType;
                        replyProps.CorrelationId = e.BasicProperties.CorrelationId;
                        replyProps.Persistent = response.Persistent;

                        _channel.BasicPublish(exchange: "", routingKey: e.BasicProperties.ReplyTo,
                            basicProperties: replyProps, body: Serializer.Serialize(response.Payload));
                        _channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                    }
                    catch (Exception)
                    {
                        _channel.BasicNack(e.DeliveryTag, false,false);
                    }
                }
                catch (Exception)
                {
                    _channel.BasicNack(e.DeliveryTag, false, true);
                }
            };
            _consumerToken = _channel.BasicConsume(queue: n.ExchangeName, autoAck: false, consumer: consumer);
        }

        public void Stop()
        {
            _channel.BasicCancel(_consumerToken);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection.Dispose();
        }
    }
}
