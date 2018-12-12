/*______________________________*
 *_________© Monoid INC_________*
 *_____SaveUserSettingsModel____*
 *______________________________*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackendApi
{
    public class SaveUserSettingsModel
    {
        [JsonProperty("enabledNotifications")]
        public bool EnabledNotifications { get; set; }

        [JsonProperty("notificationRecipients")]
        public string[] NotificationRecipients { get; set; }


    }
}
