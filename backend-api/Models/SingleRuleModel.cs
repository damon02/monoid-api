/*______________________________*
 *_________© Monoid INC_________*
 *______SingleRuleModel.cs______*
 *______________________________*/
using backend_core;

namespace BackendApi
{
    public class SingleRuleModel
    {
        public MainProtocol MainProtocol { get; set; }
        public Protocol Protocol { get; set; }
        public string[] DestIp { get; set; }
        public string[] SourceIp { get; set; }
        public int[] DestPort { get; set; }
        public int[] SourcePort { get; set; }
        public bool Notify { get; set; }
        public bool Log { get; set; }
        public string Message { get; set; }
        public Risk Risk { get; set; }
        public string RuleId { get; set; }
    }
}
