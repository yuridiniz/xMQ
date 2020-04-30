using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.SocketsType
{
    internal interface ISocket
    {

        Dictionary<uint, ResponseAwaiter> GetStoredResponse();
        Dictionary<byte[], PairSocket> GetIdentitySocketsMap();

        bool Send(Message msg);
        Message Request(Message msg, int millisecondsTimeout = -1);
        void Close();
        void Bind();
        bool Connect();

        List<PairSocket> GetAllClients();

        PairSocket GetClient<T>(T identifier);
    }
}
