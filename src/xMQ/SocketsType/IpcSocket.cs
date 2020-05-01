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

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public bool Send(byte[] msg)
        {
            throw new NotImplementedException();
        }
    }
}
