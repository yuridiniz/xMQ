using System;
using System.Collections.Generic;
using System.Text;
using xMQ.SocketsType;

namespace xMQ
{
    internal interface ISocketController
    {
        void OnMessage(ISocket remote, byte[] data);

        void OnDisconnect(ISocket remote);

        void OnConnected(ISocket remote);

        void OnError(ISocket remote);
    }
}
