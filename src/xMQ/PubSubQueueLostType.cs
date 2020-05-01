using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ
{
    public enum PubSubQueueLostType : byte
    {
        None = 0,
        Persitent = 1,
        LastMessage = 2,
    }
}
