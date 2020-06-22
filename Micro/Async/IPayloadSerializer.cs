using System;

namespace dFakto.Queue
{
    public interface IPayloadSerializer
    {
        string ContentType { get; }
        
        T Deserialize<T>(ReadOnlyMemory<byte> content) where T : new();
        
        ReadOnlyMemory<byte> Serialize<T>(T content);
    }
}