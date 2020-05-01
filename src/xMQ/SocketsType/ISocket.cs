using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.SocketsType
{
    internal interface ISocket
    {
        bool Send(byte[] msg);
        void Close();
        void Bind();
        void Connect(int timeout);
    }
}
