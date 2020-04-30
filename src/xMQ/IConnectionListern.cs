using System;
using System.Collections.Generic;
using System.Text;
using xMQ.SocketsType;

namespace xMQ
{
    internal interface IController
    {
        void OnMessage(PairSocket remote, Envelope envelop);

        void OnDisconnect(PairSocket remote);

        void OnConnected(PairSocket remote);
    }
}
