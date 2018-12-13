/*______________________________*
 *_________© Monoid INC_________*
 *_____ActivateUserModel.cs_____*
 *______________________________*/

using Newtonsoft.Json;

namespace BackendApi
{
    public class ActivateUserModel
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
