/*______________________________*
 *_________© Monoid INC_________*
 *_____RequestTokenModel.cs_____*
 *______________________________*/

using Newtonsoft.Json;

namespace BackendApi
{
    public class RequestTokenModel
    {
        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
