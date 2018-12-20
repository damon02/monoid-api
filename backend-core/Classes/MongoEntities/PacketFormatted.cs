/*______________________________*
 *_________© Monoid INC_________*
 *______PacketFormatted.cs______*
 *______________________________*/

using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class PacketFormatted
    {
        public Risk Risk { get; set; }
        public string Reason { get; set; }
        public string SourceIp { get; set; }
        public string DestinationIp { get; set; }
        public Protocol Protocol { get; set; }
        public MainProtocol MainProtocol { get; set; }
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        public int PacketSize { get; set; }
        public bool HasSynFlag { get; set; }
        public bool HasAckFlag { get; set; }
    }
}
