using System.Security.Cryptography;

namespace dFakto.Queue
{
    public class Message<T>
    {
        public Message(T payload)
        {
            Payload = payload;
        }
        
        public T Payload { get; set; }
        
        public string? CorrelationId { get; set; }
        public string? ReplyTo { get; set; }
        public bool Persistent { get; set; } = true;
    }
}