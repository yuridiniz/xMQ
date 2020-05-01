using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.SocketsType
{
    internal interface ISocket: IDisposable
    {
        bool Send(byte[] msg);
        int Read(byte[] buffer);
        void Close();
        void Bind();
        void Connect(int timeout);
    }
}
