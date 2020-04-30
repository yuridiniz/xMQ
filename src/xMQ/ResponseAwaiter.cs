using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xMQ
{
    internal class ResponseAwaiter
    {
        public Envelope Data { get; set; }
        public ManualResetEventSlim Signal { get; set; }

        public ResponseAwaiter()
        {
            Signal = new ManualResetEventSlim(false);
        }
    }
}
