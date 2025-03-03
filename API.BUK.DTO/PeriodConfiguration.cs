using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class PeriodConfiguration
    {
        public string process_month { get; set; }
        public string absences_sync_start { get; set; }
        public string absences_sync_end { get; set; }
        public string permission_sync_start { get; set; }
        public string permission_sync_end { get; set; }
        public string attendance_sync_start { get; set; }
        public string attendance_sync_end { get; set; }
        public string overtime_sync_start { get; set; }
        public string overtime_sync_end { get; set; }
        public string non_worked_hours_sync_start { get; set; }
        public string non_worked_hours_sync_end { get; set; }
        public int company_id { get; set; }
        
    }
}
