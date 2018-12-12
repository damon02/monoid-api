/*______________________________*
 *_________© Monoid INC_________*
 *___________Packet.cs__________*
 *______________________________*/

using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    /// <summary>
    /// The data captured by the embedded system
    /// </summary>
    public class Packet
    {
        public ObjectId Id { get; set; }
        public ObjectId UserId { get; set; }
        public DateTime CreationDate { get; set; }
        public BsonDocument PacketData { get; set; }
    }
}
