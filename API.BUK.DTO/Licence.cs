using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class Licence : Absence
    {
        public string application_date { get; set; }
        public double days_count { get; set; }
        public string justification { get; set; }
        public double contribution_days { get; set; }
        public string format { get; set; }
        public int licence_type_id { get; set; }
       
    }
}
