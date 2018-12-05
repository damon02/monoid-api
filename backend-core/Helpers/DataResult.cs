/*______________________________*
 *_________© Monoid INC_________*
 *_________DataResult.cs________*
 *______________________________*/

using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class DataResult<T>
    {
        public bool Success { get; set; }
        public List<T> Data { get; set; }
        public string ErrorMessage { get; set; }
    }
}
