/*______________________________*
 *_________© Monoid INC_________*
 *__________ErrorLog.cs_________*
 *______________________________*/

using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class ErrorLog
    {
        public string Exception { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
