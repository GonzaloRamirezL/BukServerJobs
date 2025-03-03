using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO.Filters
{
    public class AttendanceFilter
    {
        
        public string StartDate { get; set; }
        
        public string EndDate { get; set; }
        
        public string UserIds { get; set; }
    }
}
