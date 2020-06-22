using System;
using System.Runtime.InteropServices.WindowsRuntime;
using dFakto.Queue;
using Google.Protobuf;

namespace Micro.Host_old.RabbitMq
{
    public class GrpcPayloadSerializer : IPayloadSerializer
    {
        public string ContentType => "application/protobuf";
        public T Deserialize<T>(ReadOnlyMemory<byte> content) where T : new()
        {
            IMessage message = (IMessage) new T();
            message.MergeFrom(content.ToArray());
            return (T)message;
        }

        public ReadOnlyMemory<byte> Serialize<T>(T content)
        {
            if(content is IMessage msg)
            {
                return msg.ToByteArray();
            }

            return null;
        }
    }
}