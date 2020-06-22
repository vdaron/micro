﻿using System;

namespace dFakto.Queue
{
    public interface ISubscriber : IDisposable
    {
        void Start<T>(string queueName, Action<Message<T>> messageReceived) where T : new();
        void Stop();
    }
}
