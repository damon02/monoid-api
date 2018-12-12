/*______________________________*
 *_________© Monoid INC_________*
 *___________Parser.cs__________*
 *______________________________*/

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace backend_core
{
    public class Parser
    {
        public JObject PropertyCheck(JObject jObj)
        {
            JObject newJObj = new JObject();

            // Can parse jObj here to remove data.
            foreach (JProperty jp in jObj.Properties().ToList())
            {
                string newName = null;

                if (jp.Name.Contains('.'))
                {
                    newName = jp.Name.Replace('.', '_');
                }

                if (jp.Value is JObject)
                {
                    jp.Value = PropertyCheck(jp.Value.ToObject<JObject>());
                }

                newJObj[string.IsNullOrWhiteSpace(newName) ? jp.Name : newName] = jp.Value;
            }

            return newJObj;
        }
    }

    public static class ParserExtension
    {
        public static List<Packet> ConvertToPackets(this JToken token, ObjectId uId)
        {
            List<Packet> packets = new List<Packet>();
            
            List<JObject> jObjects = new List<JObject>();

            if (token is JArray)
            {
                jObjects = token.ToObject<List<JObject>>();
            }
            else if (token is JObject)
            {
                jObjects.Add(token.ToObject<JObject>());
            }

            if (jObjects.Count < 1) return packets;

            Parser parser = new Parser();

            foreach (JObject jObj in jObjects)
            {
                JObject parsedJObj = parser.PropertyCheck(jObj);

                Packet packet = new Packet
                {
                    UserId = uId,
                    CreationDate = DateTime.Now,
                    PacketData = BsonDocument.Parse(parsedJObj.ToString())
                };

                packets.Add(packet);
            }

            return packets;
        }
    }
}
