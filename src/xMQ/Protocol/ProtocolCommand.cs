using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xMQ.Protocol
{
    public abstract class ProtocolCommand
    {
        public int CODE { get; internal set; }

        public abstract bool HandleMessage(PairSocket me, PairSocket remote, Envelope envelope);

        public override bool Equals(object obj)
        {
            if(obj is byte)
            {
                return CODE == (byte)obj;
            } else
            {
                return object.ReferenceEquals(this, obj);
            }
        }

        public override int GetHashCode()
        {
            return CODE;
        }

        public static bool operator ==(ProtocolCommand item1, ProtocolCommand item2)
        {
            if (object.ReferenceEquals(item1, item2)) { return true; }
            if ((object)item1 == null || (object)item2 == null) { return false; }
            return item1.CODE == item2.CODE;
        }

        public static bool operator !=(ProtocolCommand item1, ProtocolCommand item2)
        {
            return !(item1 == item2);
        }

        public static bool operator ==(int item1, ProtocolCommand item2)
        {
            return item1 == item2.CODE;
        }

        public static bool operator !=(int item1, ProtocolCommand item2)
        {
            return !(item1 == item2);
        }

        public static bool operator ==(ProtocolCommand item2, int item1)
        {
            return item1 == item2;
        }

        public static bool operator !=(ProtocolCommand item2, int item1)
        {
            return item1 != item2;
        }
    }
}
