using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ
{
    public enum PubSubQueueLostType : byte
    {
        None = 0,
        LastMessage = 1,
        Persistent = 2,
    }
}
