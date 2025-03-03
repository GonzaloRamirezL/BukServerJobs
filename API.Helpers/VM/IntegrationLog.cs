using Microsoft.Azure.Cosmos.Table;
using System;

namespace API.Helpers.VM
{
    public class IntegrationLog : TableEntity
    {
        public string action { get; set; }
        public string user_identifier { get; set; }
        public string message { get; set; }

        public override string ToString()
        {
            string log = "";
            log += "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " +
                "(" + action + "): " + message;

            return log;
        }
    }
}
