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
            ErrorLog log = new ErrorLog();
            log.Source = ex.Source;
            log.Exception = ex.Message;
            log.StackTrace = ex.StackTrace;
            log.TimeStamp = DateTime.Now;

            return database.StoreErrorLog(log);
        }
    }
}
