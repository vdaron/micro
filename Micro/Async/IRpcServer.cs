﻿using System;

namespace dFakto.Queue
{
    public interface IRpcServer : IDisposable
    {
        IPayloadSerializer Serializer { get; set; }
        void Start<T, U>(string queueName, Func<Message<T>, Message<U>> commandReceived) where T : new();
        
        void Stop();
    }
}
