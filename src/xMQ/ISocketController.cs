using System;
using System.Collections.Generic;
using System.Text;
using xMQ.SocketsType;

namespace xMQ
{
    internal interface ISocketController
    {
        void OnMessage(ISocket remote);

        void OnConnected(ISocket remote);

        void OnError(ISocket remote);
    }
}
