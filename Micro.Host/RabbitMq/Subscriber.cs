﻿using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace dFakto.Queue.RabbitMQ
{
    public class Subscriber : ISubscriber
    {
        private readonly IConnection _connection;
        private readonly PayloadSerializerFactory _payloadSerializer;
        private readonly IModel _channel;
        private string? _queueName;
        private EventingBasicConsumer? _consumer;
        private string? _consumerToken;

        public Subscriber(IConnection connection, PayloadSerializerFactory serviceProvider)
        {
            _connection = connection;
            _payloadSerializer = serviceProvider;
            _channel = connection.CreateModel();
        }

        public void Start<T>(string queueName, Action<Message<T>> messageReceived) where T : new()
        {
            var n = PublicationAddress.Parse(queueName);

            Start(n.ExchangeType,n.ExchangeName,n.RoutingKey, messageReceived); 
        }

        private void Start<T>(string exchangeType, string exchangeName, string routingKey, Action<Message<T>> messageReceived) where T : new()
        {
            _channel.ExchangeDeclare(exchange: exchangeName, type: exchangeType);
            _queueName = _channel.QueueDeclare().QueueName;
            
            _channel.QueueBind(_queueName, exchangeName, routingKey);

            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += (s, e) =>
            {
                try
                {
                    var serializer = _payloadSerializer.GetSerializer(e.BasicProperties.ContentType);

                    T body = serializer.Deserialize<T>(e.Body);

                    var message = new Message<T>(body)
                    {
                        CorrelationId = e.BasicProperties.CorrelationId,
                        ReplyTo = e.BasicProperties.ReplyTo,
                        Persistent = e.BasicProperties.Persistent
                    };

                    messageReceived(message);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            };
            _consumerToken = _channel.BasicConsume(_queueName, true, _consumer);
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
