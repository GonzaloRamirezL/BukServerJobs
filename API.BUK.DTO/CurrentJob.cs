using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class CurrentJob
    {
        public bool zone_assignment { get; set; }
        public string union { get; set; }
        public int company_id { get; set; }
        public int area_id { get; set; }
        public string contract_term { get; set; }
        public string contract_type { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string contract_finishing_date_1 { get; set; }
        public string contract_finishing_date_2 { get; set; }
        public string weekly_hours { get; set; }
        public double base_wage { get; set; }
        public string cost_center { get; set; }
        public string active_until { get; set; }
        public string periodicity { get; set; }
        public string working_schedule_type { get; set; }
        public Role role { get; set; }
    }
}
