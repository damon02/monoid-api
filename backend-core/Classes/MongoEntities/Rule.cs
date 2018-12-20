/*______________________________*
 *_________© Monoid INC_________*
 *____________Rule.cs___________*
 *______________________________*/

using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class Rule
    {
        public ObjectId Id { get; set; }
        public Protocol Protocol { get; set; }

    }
}