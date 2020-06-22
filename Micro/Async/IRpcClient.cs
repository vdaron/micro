﻿using System;
using System.Threading.Tasks;

namespace dFakto.Queue
{
    public interface IRpcClient : IDisposable
    {
        IPayloadSerializer Serializer { get; set; }
        Task<Message<U>> SendCommand<T, U>(string queueName, Message<T> command) where U : new();
    }
}
