/*______________________________*
 *_________© Monoid INC_________*
 *__________DataCore.cs_________*
 *______________________________*/

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public class DataCore : BaseCore
    {
        private static DataCore instance = null;
        private static readonly object padlock = new object();

        public static DataCore Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DataCore();
                    }
                    return instance;
                }
            }
        }

        public DataResult<Packet> StorePackets(JToken json)
        {
            DataResult<Packet> dr = null;

            List<Packet> packets = json.ConvertToPackets();

            dr = database.StorePackets(packets);

            return dr;
        }
    }
}
