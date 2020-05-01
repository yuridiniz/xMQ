using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ
{
    public class MessageData
    {
        public string Queue { get; set; }
        public bool IsLost { get; set; }
        public DateTime? SendDate { get; set; }
    }
}
