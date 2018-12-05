/*______________________________*
 *_________© Monoid INC_________*
 *_____RegisterUserModel.cs_____*
 *______________________________*/

using Newtonsoft.Json;

namespace BackendApi
{
    public class RegisterUserModel
    {
        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
