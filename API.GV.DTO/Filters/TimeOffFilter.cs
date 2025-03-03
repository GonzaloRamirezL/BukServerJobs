using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO.Filters
{
    public class TimeOffFilter
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string UserIds { get; set; }

    }
}
