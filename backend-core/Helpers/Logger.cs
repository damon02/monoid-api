/*______________________________*
 *_________© Monoid INC_________*
 *___________Logger.cs__________*
 *______________________________*/

using System;
using System.Collections.Generic;
using System.Text;

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

        public void CreateDataLog(string message, Risk risk)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            DataLog dataLog = new DataLog
            {
                Message = message,
                Risk = risk
            };

            database.StoreDataLog(dataLog);
        }
    }
}
