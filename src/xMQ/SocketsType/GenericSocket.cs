using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace xMQ.SocketsType
{
    public abstract class GenericSocket
    {
        public bool ServerRunning { get; set; }
        public bool ClientRunning { get; set; }

        public GenericSocket()
        {
           
        }
    }
}
