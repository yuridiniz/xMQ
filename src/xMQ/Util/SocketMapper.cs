using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using xMQ.SocketsType;

namespace xMQ.Util
{
    internal static class SocketMapper
    {
        private static Dictionary<Socket, ISocket> MappedISocket { get; } = new Dictionary<Socket, ISocket>();

        internal static void Mapper(Socket source, ISocket target)
        {
            MappedISocket[source] = target;
        }

        internal static void RemoveISocketMapper(Socket source)
        {
            MappedISocket.Remove(source);
        }

        internal static ISocket GetISocket(Socket source)
        {
            return MappedISocket[source];
        }
    }
}
