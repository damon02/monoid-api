/*______________________________*
 *_________© Monoid INC_________*
 *____RecoverPasswordModel.cs___*
 *______________________________*/

using Newtonsoft.Json;

namespace BackendApi
{
    public class RecoverPasswordModel
    {
        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
