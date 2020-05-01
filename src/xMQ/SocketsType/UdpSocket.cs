using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xMQ.Protocol;
using xMQ.Util;

namespace xMQ.SocketsType
{
    internal class UdpSocket : GenericSocket, ISocket
    {
        public const string SCHEME = "udp";
        private Uri serverUri;

        public UdpSocket(Uri serverUri)
        {
            this.serverUri = serverUri;
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
