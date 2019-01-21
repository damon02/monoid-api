/*______________________________*
 *_________© Monoid INC_________*
 *___________Logger.cs__________*
 *______________________________*/

using MongoDB.Bson;
using System;

namespace backend_core
{
    public class Logger
    {
        private Database database = Database.Instance;

        public DataResult<ErrorLog> CreateErrorLog(Exception ex)
        {
            if (ex == null) return null;

            ErrorLog log = new ErrorLog
            {
                Source = ex.Source,
                Exception = ex.Message,
                StackTrace = ex.StackTrace,
                TimeStamp = DateTime.Now
            };

            return database.StoreErrorLog(log);
        }

        public void CreateEndPointLog(EndPointContext context, string body, EndPointType type)
        {
            if (context == null) return;

            EndPointLog log = new EndPointLog
            {
                ClientIp = context.ClientIP,
                Body = body,
                TimeStamp = DateTime.Now,
                EndPointType = type
            };

            database.StoreEndPointLog(log);
        }

        public void CreateDataLog(ObjectId uId, string message, Risk risk, bool visible = true)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            DataLog dataLog = new DataLog
            {
                UserId = uId,
                Message = message,
                Risk = risk,
                TimeStamp = DateTime.Now,
                Visible = visible
            };

            database.StoreDataLog(dataLog);
        }
    }
}
