/*______________________________*
 *_________© Monoid INC_________*
 *________EndPointLog.cs________*
 *______________________________*/

using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class EndPointLog
    {
        public int UserId { get; set; }
        public string ClientIp { get; set; }
        public EndPointType EndPointType { get; set; }
        public string Body { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    /// <summary>
    /// The EndPoint 
    /// </summary>
    public enum EndPointType
    {
        A,
        B,
        C
    }
}
