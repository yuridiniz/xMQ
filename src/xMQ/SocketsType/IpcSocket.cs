using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xMQ.Protocol;

namespace xMQ.SocketsType
{
    internal class IpcSocket : GenericSocket, ISocket
    {
        public const string SCHEME = "ipc";
        private string host;

        public IpcSocket(string host)
        {
            this.host = host;
        }

        public void Bind()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public bool Connect()
        {
            throw new NotImplementedException();
        }

        public List<PairSocket> GetAllClients()
        {
            throw new NotImplementedException();
        }

        public PairSocket GetClient<T>(T identifier)
        {
            throw new NotImplementedException();
        }

        public Dictionary<byte[], PairSocket> GetIdentitySocketsMap()
        {
            throw new NotImplementedException();
        }

        public Dictionary<uint, ResponseAwaiter> GetStoredResponse()
        {
            throw new NotImplementedException();
        }

        public Message Request(Message msg, int millisecondsTimeout = -1)
        {
            throw new NotImplementedException();
        }

        public bool Send(Message msg)
        {
            throw new NotImplementedException();
        }
    }
}
