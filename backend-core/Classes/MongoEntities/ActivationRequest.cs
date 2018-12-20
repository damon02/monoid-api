/*______________________________*
 *_________© Monoid INC_________*
 *_____ActivationRequest.cs_____*
 *______________________________*/

using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class ActivationRequest
    {
        public ObjectId Id { get; set; }
        public ObjectId UserId { get; set; }
        public string Token { get; set; }
    }
}
