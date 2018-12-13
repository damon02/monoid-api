/*______________________________*
 *_________© Monoid INC_________*
 *__________BaseCore.cs_________*
 *______________________________*/

using System;
using System.Collections.Generic;
using System.Text;

namespace backend_core
{
    public partial class BaseCore
    {
        public Database database = Database.Instance;
        public const string DASHBOARD_URL = "https://dashboard.monoidinc.nl/";
    }
}
