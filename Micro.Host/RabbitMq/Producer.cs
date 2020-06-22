using dFakto.Queue;
using RabbitMQ.Client;

namespace Micro.Host.RabbitMq
{
    public class Producer : IProducer, IPublisher
    {
        private readonly IConnection _connection;
        private readonly PayloadSerializerFactory _serializer;
        private readonly IModel _channel;

        public Producer(IConnection connection, PayloadSerializerFactory serializer)
        {
            _connection = connection;
            _serializer = serializer;
            _channel = connection.CreateModel();
            Serializer = serializer.GetSerializer(PayloadSerializerFactory.DefaultContentType);
        }
        
        public IPayloadSerializer Serializer { get; set; }

        public void Send<T>(string queueName, Message<T> message)
        {
            var n = PublicationAddress.Parse(queueName);
            Send(n.ExchangeType, n.ExchangeName, n.RoutingKey, PayloadSerializerFactory.DefaultContentType, message);
        }
        
        private void Send<T>(string exchangeType, string exchangeName, string routingKey, string contentType, Message<T> message)
        {
            lock (_channel)
            {
                _channel.ExchangeDeclare(exchange: exchangeName, type:exchangeType);

                var ser = _serializer.GetSerializer(contentType);

                var body = ser.Serialize(message.Payload);
                var props = _channel.CreateBasicProperties();
                props.ContentType = ser.ContentType;
                props.CorrelationId = message.CorrelationId;
                props.ReplyTo = message.ReplyTo;
                props.Persistent = message.Persistent;

                _channel.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: props, body: body);
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection.Dispose();
            
        }
    }
}
