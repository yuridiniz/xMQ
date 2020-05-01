using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xMQ
{
    public class Message : NetworkPackage
    {
        internal Envelope Envelope { get; set; }

        public Message()
        {
        }

        internal Message(params object[] appends)
            : base(appends)
        {
        }

        internal Message(byte[] socketData)
            :base(socketData)
        {
        }

    }
}
