/*______________________________*
 *_________© Monoid INC_________*
 *__________Settings.cs_________*
 *______________________________*/

using MongoDB.Bson;

namespace backend_core
{
    /// <summary> Frontend settings </summary>
    public class Settings
    {
        public ObjectId Id { get; set; }
        public ObjectId UserId { get; set; }
        public bool EnabledNotifications { get; set; }
        public string[] NotificationRecipients { get; set; }
    }
}
