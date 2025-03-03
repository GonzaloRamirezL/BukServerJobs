using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class LicenceToSend 
    {
        public int licence_type_id { get; set; }
        public double contribution_days { get; set; }
        public string format { get; set; }
        public string @type { get; set; }
        public string start_date { get; set; }
        public double days_count { get; set; }
        public string day_percent { get; set; }
        public string application_date { get; set; }
        public string justification { get; set; }
        public int employee_id { get; set; }
    }
}
