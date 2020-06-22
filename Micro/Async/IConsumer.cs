﻿using System;

namespace dFakto.Queue
{
    public interface IConsumer : IDisposable
    {
        IPayloadSerializer Serializer { get; set; }
        void Start<T>(string address, string queueName, Action<Message<T>> messageReceived) where T : new();
        void Stop();
    }
}
