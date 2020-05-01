using System;
using System.Collections.Generic;
using System.Text;
using xMQ.Protocol;

namespace xMQ.Extension
{
   public abstract class ExtendableProtocolCommand : ProtocolCommand
   {
        public abstract byte CODE { get; internal set; }

    }
}
