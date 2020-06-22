﻿using System;

namespace dFakto.Queue
{
    public interface IProducer : IDisposable
    {
        IPayloadSerializer Serializer { get; set; }
        void Send<T>(string queueName, Message<T> message);
    }
}
