/*______________________________*
 *_________© Monoid INC_________*
 *_____RecoveryRequest.cs_______*
 *______________________________*/

using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class RecoveryRequest
    {
        public ObjectId Id { get; set; }
        public ObjectId UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
