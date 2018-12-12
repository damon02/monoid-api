/*______________________________*
 *_________© Monoid INC_________*
 *____________User.cs___________*
 *______________________________*/

using MongoDB.Bson;
using System;

namespace backend_core
{
    /// <summary>
    /// A user that is able to connect to the API and authorize himself to obtain an API token.
    /// </summary>
    public class User
    {
        public ObjectId Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTime CreationDate { get; set; }
        public string EmailAddress { get; set; }
        public string Token { get; set; }
        public bool Activated { get; set; }
    }
}
