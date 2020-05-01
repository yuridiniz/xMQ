using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.PubSubProtocol
{
    enum PubSubQueueLostType : byte
    {
        None = 0,
        Persitent = 1,
        LastMessage = 1,
    }
}
